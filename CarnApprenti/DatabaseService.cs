using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using static CarnApprenti.LivretApprentissageContext;
using MySql.Data.MySqlClient;
using System.Data;

namespace CarnApprenti
{
    public class DatabaseService
    {
        private readonly LivretApprentissageContext _context;
        private readonly ILogger<DatabaseService> _logger;
        private string _connectionString;

        public DatabaseService(LivretApprentissageContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Récupère un utilisateur à partir de son email.
        /// </summary>
        /// <param name="email">Email de l'utilisateur</param>
        /// <returns>Un dictionnaire contenant les informations de l'utilisateur</returns>
        public async Task<Dictionary<string, object>?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _context.Users
                                         .Include(u => u.Groupe)
                                         .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return null;
                }

                // Convertir les propriétés de l'utilisateur en un dictionnaire
                return new Dictionary<string, object>
                {
                    { "Id", user.Id },
                    { "Nom", user.Nom },
                    { "Prenom", user.Prenom },
                    { "Email", user.Email },
                    { "Password", user.Password }, // Le hash du mot de passe
                    { "GroupeId", user.GroupeId },
                    { "ApprenantId", user.ApprenantId },
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur par email");
                throw;
            }
        }


        /// <summary>
        /// Récupère la liste de tous les utilisateurs.
        /// </summary>
        /// <returns>Liste de tous les utilisateurs.</returns>
        public async Task<List<User>> GetUsersAsync()
        {
            return await _context.Users
                                 .Include(u => u.Groupe)
                                 .ToListAsync();
        }

        /// <summary>
        /// Supprime un utilisateur en fonction de son ID.
        /// </summary>
        /// <param name="userId">ID de l'utilisateur à supprimer</param>
        public async Task DeleteUserAsync(ulong userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new Exception("Utilisateur non trouvé.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'utilisateur avec l'ID {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Supprime un groupe en fonction de son ID.
        /// </summary>
        /// <param name="idGroupe">ID du groupe à supprimer</param>
        public async Task DeleteGroupeAsync(ulong idGroupe)
        {
            try
            {
                var groupe = await _context.Groupes.FindAsync(idGroupe);
                if (groupe != null)
                {
                    _context.Groupes.Remove(groupe);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new Exception("Groupe non trouvé.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du groupe avec l'ID {IdGroupe}", idGroupe);
                throw;
            }
        }

        public async Task CreateRoleAsync(string roleName, string title)
        {
            var role = new Role
            {
                Name = roleName,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserHasRoleAsync(ulong userId, string roleName)
        {
            var hasRole = await _context.AssignedRoles
                .Include(ar => ar.Role) // Charge les rôles liés
                .AnyAsync(ar => ar.EntityId == userId && ar.Role.Name == roleName);

            return hasRole;
        }

        public async Task RemoveRoleFromUserAsync(ulong userId, ulong roleId)
        {
            var assignedRole = await _context.AssignedRoles
                .FirstOrDefaultAsync(ar => ar.EntityId == userId && ar.RoleId == roleId);

            if (assignedRole != null)
            {
                _context.AssignedRoles.Remove(assignedRole);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GetUserRolesAsync(ulong userId)
        {
            var role = await _context.AssignedRoles
                .Include(ar => ar.Role)
                .Where(ar => ar.EntityId == userId)
                .Select(ar => ar.Role.Name)
                .FirstOrDefaultAsync(); // Retourne le premier rôle ou null si aucun rôle trouvé

            return role ?? "defaultRole"; // Retourne un rôle par défaut si aucun rôle trouvé
        }


        public async Task<List<Groupe>> GetGroupesAsync()
        {
            return await _context.Groupes.ToListAsync();
        }

        public async Task<List<User>> GetUsersByRoleAsync(string roleName)
        {
            var userIdsWithRole = await _context.AssignedRoles
                .Include(ar => ar.Role)
                .Where(ar => ar.Role.Name == roleName)
                .Select(ar => ar.EntityId) // Sélectionner les IDs des utilisateurs avec ce rôle
                .ToListAsync();

            // Récupérer les utilisateurs qui ont l'ID correspondant à ces rôles
            var users = await _context.Users
                .Where(u => userIdsWithRole.Contains(u.Id)) // Filtrer les utilisateurs par leurs IDs
                .ToListAsync();

            return users;
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task AssignRoleToUserAsync(ulong entityId, string newRole)
        {
            try
            {
                // Récupérer l'utilisateur avec Include pour charger les relations
                var user = await _context.Users
                    .Include(u => u.AssignedRoles)
                        .ThenInclude(ar => ar.Role)
                    .FirstOrDefaultAsync(u => u.Id == entityId);

                if (user == null)
                {
                    throw new Exception("Utilisateur non trouvé.");
                }

                // Récupérer le nouveau rôle
                var roleEntity = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == newRole);

                if (roleEntity == null)
                {
                    throw new Exception($"Rôle '{newRole}' non trouvé.");
                }

                // Récupérer le rôle actuel de manière sécurisée
                var currentAssignedRole = user.AssignedRoles?
                    .FirstOrDefault();

                var currentRole = currentAssignedRole?.Role?.Name ?? "";

                // Vérifier si l'utilisateur était référent ou apprenant avant le changement
                bool wasReferentOrApprenant = currentRole == "referent" || currentRole == "apprenant";
                bool willBeReferentOrApprenant = newRole == "referent" || newRole == "apprenant";

                // Si l'utilisateur était référent ou apprenant mais ne le sera plus
                if (wasReferentOrApprenant && !willBeReferentOrApprenant)
                {
                    user.GroupeId = null;
                    _logger.LogInformation($"Suppression du groupe pour l'utilisateur {user.Id} car n'est plus référent ou apprenant");
                }

                // Vérifier si l'utilisateur était tuteur avant le changement
                bool wasTuteur = currentRole == "tuteur";
                bool willBeTuteur = newRole == "tuteur";

                // Si l'utilisateur était tuteur mais ne le sera plus
                if (wasTuteur && !willBeTuteur)
                {
                    user.ApprenantId = null;
                    _logger.LogInformation($"Suppression de l'apprenant associé pour l'utilisateur {user.Id} car n'est plus tuteur");
                }

                if (currentAssignedRole != null)
                {
                    // Mettre à jour le rôle existant
                    currentAssignedRole.RoleId = roleEntity.Id;
                    currentAssignedRole.EntityType = "App\\Models\\User";
                    _logger.LogInformation($"Mise à jour du rôle pour l'utilisateur {user.Id} vers {newRole}");
                }
                else
                {
                    // Créer un nouveau rôle assigné
                    var newAssignedRole = new AssignedRole
                    {
                        EntityId = entityId,
                        EntityType = "App\\Models\\User",
                        RoleId = roleEntity.Id,
                        Scope = null,
                        RestrictedToId = null,
                        RestrictedToType = null
                    };
                    _context.AssignedRoles.Add(newAssignedRole);
                    _logger.LogInformation($"Création d'un nouveau rôle pour l'utilisateur {user.Id}: {newRole}");
                }

                // Sauvegarder tous les changements
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Changements sauvegardés avec succès pour l'utilisateur {user.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'assignation du rôle: {ex.Message}");
                throw new Exception($"Erreur lors de l'assignation du rôle: {ex.Message}");
            }
        }

        public async Task<User> GetUserByIdAsync(ulong userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .FirstOrDefaultAsync();  // Recherche un utilisateur avec cet ID
                return user;
            }
            catch (Exception ex)
            {
                // Log l'erreur ou gérer comme nécessaire
                throw new Exception($"Erreur lors de la récupération de l'utilisateur : {ex.Message}", ex);
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await _context.Users
                    .Where(u => u.Id == user.Id)
                    .FirstOrDefaultAsync();

                if (existingUser == null)
                {
                    throw new Exception("Utilisateur introuvable.");
                }

                // Mettre à jour les propriétés de l'utilisateur
                existingUser.Nom = user.Nom;
                existingUser.Prenom = user.Prenom;
                existingUser.Email = user.Email;
                existingUser.GroupeId = user.GroupeId;
                existingUser.ApprenantId = user.ApprenantId;
                existingUser.Password = user.Password;
                existingUser.UpdatedAt = DateTime.Now;

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log l'erreur ou gérer comme nécessaire
                throw new Exception($"Erreur lors de la mise à jour de l'utilisateur : {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetAllUserEmailsAsync()
        {
            return await _context.Users
                .Where(u => u.Email != null)
                .Select(u => u.Email)
                .ToListAsync();
        }

    }
}
