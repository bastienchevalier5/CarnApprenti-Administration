using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using static CarnApprenti.LivretApprentissageContext;
using MySql.Data.MySqlClient;
using System.Data;
using System.Globalization;
using System.Diagnostics;
using CarnApprenti.Components.Pages;

namespace CarnApprenti
{
    public class DatabaseService(LivretApprentissageContext context, ILogger<DatabaseService> logger)
    {
        private readonly LivretApprentissageContext _context = context;
        private readonly ILogger<DatabaseService?> _logger = logger;

        /// <summary>
        /// Récupère un utilisateur à partir de son email.
        /// </summary>
        /// <param name="email">Email de l'utilisateur</param>
        /// <returns>Un dictionnaire contenant les informations de l'utilisateur</returns>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
                var user = await _context.Users
                                         .Include(u => u.Groupe)
                                         .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return null;
                }

                // Convertir les propriétés de l'utilisateur en un dictionnaire
                return user;
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
            User? user = await _context.Users.FindAsync(userId);
            Livret livret = await _context.Livrets
                .Where(l => l.UserId == user.Id)
                .FirstOrDefaultAsync();


                if (user != null)
                {
                    if (livret != null)
                    {
                    _context.Livrets.Remove(livret);
                    }

                    // Vérifier si d'autres utilisateurs ont cet utilisateur comme apprenant
                    var tuteurs = await _context.Users
                        .Where(u => u.ApprenantId == user.Id)
                        .ToListAsync();

                    if (tuteurs != null)
                    {
                        foreach (var tuteur in tuteurs)
                            {
                                // Supprimer la relation avec l'apprenant
                                tuteur.ApprenantId = null;
                                _context.Users.Update(tuteur); // Mettre à jour l'utilisateur (tuteur)
                            }
                    }
                    
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new Exception("Utilisateur non trouvé.");
                }
            
        }

        /// <summary>
        /// Supprime un groupe en fonction de son ID.
        /// </summary>
        /// <param name="idGroupe">ID du groupe à supprimer</param>
        public async Task DeleteGroupeAsync(ulong idGroupe)
        {
                var groupe = await _context.Groupes.FindAsync(idGroupe);
                var groupeMatieres = await _context.GroupeMatieres
                    .Where(gm => gm.GroupeId == groupe.Id)
                    .ToListAsync();
                var users = await _context.Users
                    .Where(u => u.GroupeId == groupe.Id)
                    .ToListAsync();

                if (groupe != null)
                {
                    foreach (var gm in groupeMatieres)
                    {
                    _context.GroupeMatieres.Remove(gm);
                    }

                    foreach (var user in users)
                    {
                        user.GroupeId = null;
                    }
                _context.Groupes.Remove(groupe);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new Exception("Groupe non trouvé.");
                }
            
        }

        public async Task<string?> GetUserRolesAsync(ulong userId)
        {
            var role = await _context.AssignedRoles
                .Include(ar => ar.Role)
                .Where(ar => ar.EntityId == userId)
                .Select(ar =>  ar.Role.Name)
                .FirstOrDefaultAsync(); // Retourne le premier rôle ou null si aucun rôle trouvé

            return role;
        }


        public async Task<List<Groupe>> GetGroupesAsync()
        {
            return await _context.Groupes.ToListAsync();
        }

        public async Task<ulong> AddGroupeAsync(Groupe groupe)
        {
            groupe.CreatedAt = DateTime.Now;
            groupe.UpdatedAt = DateTime.Now;
            _context.Groupes.Add(groupe);
            await _context.SaveChangesAsync();
            return groupe.Id;
        }

        public async Task<Groupe?> GetGroupeByIdAsync(ulong id)
        {
            return await _context.Groupes
                                 .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task UpdateGroupeAsync(Groupe? groupe)
        {
            _context.Groupes.Update(groupe);
            await _context.SaveChangesAsync();
        }

        public async Task<List<GroupeWithReferent>> GetGroupesWithReferentAsync()
        {
            const string ROLE_REFERENT = "referent";

            var groupesWithReferents = await _context.Groupes
                .Select(groupe => new GroupeWithReferent
                {
                    Id = groupe.Id,
                    Nom = groupe.Nom,
                    Referent = _context.AssignedRoles
                        .Include(ar => ar.Role)
                        .Include(ar => ar.Entity)
                        .Where(ar => ar.Role.Name == ROLE_REFERENT)
                        .Where(ar => ar.Entity.GroupeId == groupe.Id)
                        .Select(ar => ar.Entity)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return groupesWithReferents;
        }

        public async Task<List<User>> GetReferentsAsync()
        {
            try
            {
                // Step 1: Retrieve all assigned roles with the role name "referent"
                var referentIds = await _context.AssignedRoles
                    .Include(ar => ar.Role)  // Include the related Role entity
                    .Where(ar => ar.Role.Name == "referent")
                    .Select(ar => ar.EntityId)  // Select the IDs of users with the "referent" role
                    .ToListAsync();

                // Step 2: Retrieve users whose IDs match the referent IDs
                var users = await _context.Users
                    .Where(u => referentIds.Contains(u.Id))  // Filter users based on the referent IDs
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Erreur lors de la récupération des référents.", ex);
            }
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
                    .FirstOrDefaultAsync(u => u.Id == entityId) ?? throw new Exception("Utilisateur non trouvé.");

                // Récupérer le nouveau rôle
                var roleEntity = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == newRole) ?? throw new Exception($"Rôle '{newRole}' non trouvé.");

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
                }

                // Vérifier si l'utilisateur était tuteur avant le changement
                bool wasTuteur = currentRole == "tuteur";
                bool willBeTuteur = newRole == "tuteur";

                // Si l'utilisateur était tuteur mais ne le sera plus
                if (wasTuteur && !willBeTuteur)
                {
                    user.ApprenantId = null;
                }

                if (currentAssignedRole != null)
                {
                    // Mettre à jour le rôle existant
                    currentAssignedRole.RoleId = roleEntity.Id;
                    currentAssignedRole.EntityType = "App\\Models\\User";
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
                }

                // Sauvegarder tous les changements
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'assignation du rôle: {ex.Message}");
            }
        }

        public async Task<User?> GetUserByIdAsync(ulong userId)
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
                    .FirstOrDefaultAsync() ?? throw new Exception("Utilisateur introuvable.");

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

        public async Task<List<string?>> GetAllUserEmailsAsync()
        {
            return await _context.Users
                .Where(u => u.Email != null)
                .Select(u => u.Email)
                .ToListAsync();
        }

        public async Task<List<Formateur>> GetFormateursAsync()
        {
            try
            {
                // Supposons que _context est un DbContext ou similaire
                var formateurs = await _context.Formateurs.ToListAsync();
                return formateurs ?? []; // Retourne une liste vide si null
            }
            catch (Exception ex)
            {
                // Log de l'erreur et gestion des exceptions
                Console.Error.WriteLine($"Erreur lors du chargement des formateurs : {ex.Message}");
                return []; // Retourne une liste vide en cas d'erreur
            }
        }


        // Récupérer toutes les matières
        public async Task<List<Matiere>> GetMatieresAsync()
        {
            return await _context.Matieres.Include(m => m.Formateur).ToListAsync();
        }

        // Ajouter un formateur
        public async Task AddFormateurAsync(Formateur formateur)
        {
            _context.Formateurs.Add(formateur);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateFormateurAsync(Formateur formateur)
        {
            try
            {
                var existingFormateur = await _context.Formateurs.FindAsync(formateur.Id);
                if (existingFormateur != null)
                {
                    existingFormateur.Nom = formateur.Nom;
                    existingFormateur.Prenom = formateur.Prenom;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log ou gérer l'erreur
                throw new Exception($"Erreur lors de la mise à jour du formateur: {ex.Message}", ex);
            }
        }



        // Ajouter une matière
        public async Task AddMatiereAsync(Matiere matiere)
        {
            _context.Matieres.Add(matiere);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMatiereAsync(Matiere matiere)
        {
            // Vérifier si la matière existe dans la base de données
            var existingMatiere = await _context.Matieres
                                                  .Include(m => m.Formateur) // Inclure le formateur pour éviter les problèmes de navigation
                                                  .FirstOrDefaultAsync(m => m.Id == matiere.Id);
            if (existingMatiere != null)
            {
                // Mettre à jour les propriétés de la matière
                existingMatiere.Nom = matiere.Nom;
                existingMatiere.FormateurId = matiere.FormateurId;

                // Mettre à jour le formateur si nécessaire
                    var formateur = await _context.Formateurs
                                                     .FirstOrDefaultAsync(f => f.Id == matiere.FormateurId);
                    if (formateur != null)
                    {
                        existingMatiere.Formateur = formateur;
                    }
                

                // Sauvegarder les changements dans la base de données
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Matière non trouvée");
            }
        }


        // Supprimer un formateur
        public async Task DeleteFormateurAsync(ulong formateurId)
        {
            var formateur = await _context.Formateurs.FindAsync(formateurId);
            var matieres = await _context.Matieres
                .Where(m => m.FormateurId == formateur.Id)
                .ToListAsync();

            if (formateur != null)
            {
                foreach (var matiere in matieres)
                {
                    _context.Matieres.Remove(matiere);
                }
                _context.Formateurs.Remove(formateur);
                await _context.SaveChangesAsync();
            }
        }

        // Supprimer une matière
        public async Task DeleteMatiereAsync(ulong matiereId)
        {
            var matiere = await _context.Matieres.FindAsync(matiereId);
            if (matiere != null)
            {
                _context.Matieres.Remove(matiere);
                await _context.SaveChangesAsync();

            }
        }

        // Récupérer un formateur par ID
        public async Task<Formateur?> GetFormateurByIdAsync(ulong formateurId)
        {
            try
            {
                Console.WriteLine($"Récupération du formateur avec ID : {formateurId}");
                var formateur = await _context.Formateurs
                    .Where(f => f.Id == formateurId)
                    .FirstOrDefaultAsync() ?? throw new Exception($"Formateur avec l'ID {formateurId} non trouvé.");
                return formateur;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erreur lors de la récupération du formateur : {ex.Message}");
                throw;
            }
        }




        // Récupérer une matière par ID
        public async Task<Matiere?> GetMatiereByIdAsync(ulong matiereId)
        {
            try
            {
                var matiere = await _context.Matieres
                    .Where(f => f.Id == matiereId)  // Recherche un formateur avec cet ID
                    .FirstOrDefaultAsync();

                return matiere;  // Retourne le formateur trouvé ou null si aucun formateur n'est trouvé
            }
            catch (Exception ex)
            {
                // Log l'erreur ou gérer comme nécessaire
                throw new Exception($"Erreur lors de la récupération du formateur : {ex.Message}", ex);
            }
        }

        public async Task<Matiere?> GetMatiereByNameAndFormateurAsync(string nomMatiere, ulong formateurId)
        {
            try
            {
                return await _context.Matieres
                                     .FirstOrDefaultAsync(m => m.Nom == nomMatiere && m.FormateurId == formateurId);
            }
            catch (Exception ex)
            {
                // Log de l'erreur
                Console.WriteLine($"Erreur lors de la récupération de la matière : {ex.Message}");
                throw; // Relancer l'exception pour la gestion globale
            }
        }

        public async Task<List<Livret>> GetLivretsAsync()
        {
            return await _context.Livrets
                .Include(l => l.User)  // Récupère les informations liées à l'utilisateur (l'apprenant)
                .Include(l => l.Modele) // Récupère les informations liées au modèle
                .ToListAsync();
        }

        // Supprimer un livret par son ID
        public async Task DeleteLivretAsync(ulong livretId)
        {
            var livret = await _context.Livrets.FindAsync(livretId);
            var compteRendus = await _context.CompteRendus
                .Where(cr => cr.LivretId == livret.Id)
                .ToListAsync();

            if (livret != null)
            {
                foreach (var compteRendu in compteRendus)
                {
                    _context.CompteRendus.Remove(compteRendu);
                }
                _context.Livrets.Remove(livret);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddLivretAsync(Livret livret)
        {
            _context.Livrets.Add(livret);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetApprenantsAsync()
        {
            try
            {
                // Step 1: Retrieve all assigned roles with the role name "referent"
                List<ulong>? apprenantIds = await _context.AssignedRoles
                    .Include(ar => ar.Role)  // Include the related Role entity
                    .Where(ar => ar.Role.Name == "apprenant")
                    .Select(ar => ar.EntityId)  // Select the IDs of users with the "apprenant" role
                    .ToListAsync();

                // Step 2: Retrieve users whose IDs match the referent IDs
                var users = await _context.Users
                    .Where(u => apprenantIds.Contains(u.Id))  // Filter users based on the referent IDs
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Erreur lors de la récupération des apprenants.", ex);
            }
        }

        public async Task<List<User?>> GetApprenantsForTuteurAsync(ulong tuteurId)
        {
            try
            {
                // Récupérer tous les utilisateurs où le ApprenantId correspond à l'ID de l'apprenant
                var apprenant = await _context.Users
                    .Where(u => tuteurId == u.Id)
                    .Select(u => u.Apprenant)  // Filtrer par ApprenantId
                    .ToListAsync();  // Récupérer les utilisateurs

                return apprenant;
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la récupération des tuteurs associés.", ex);
            }
        }

        public async Task<Livret?> GetLivretByIdAsync(ulong livretId)
        {
            return await _context.Livrets
                .FirstOrDefaultAsync(l => l.Id == livretId);
        }

        public async Task UpdateLivretAsync(Livret livret)
        {
            _context.Livrets.Update(livret);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string?>> GetPeriodesAsync(ulong livretId)
        {
            try
            {
                List<string?> periodes = [];

                // Récupérer les comptes rendus pour le livret donné
                var comptesRendus = await _context.CompteRendus
                    .Where(cr => cr.LivretId == livretId)
                    .ToListAsync();

                // Récupérer les périodes uniques
                foreach (var cr in comptesRendus)
                {
                    var periode = cr.Periode;
                    if (!periodes.Contains(periode))
                    {
                        periodes.Add(periode);
                    }
                }

                return periodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des périodes : {ex.Message}");
                return []; // Retourner une liste vide en cas d'erreur
            }
        }


        public async Task<CompteRendu?> GetCompteRenduAsync(ulong livretId, string periode)
        {
            try
            {
                // Recherche du compte rendu correspondant à la période donnée
                var compteRendu = await _context.CompteRendus
                    .FirstOrDefaultAsync(cr => cr.LivretId == livretId && cr.Periode == periode);

                return compteRendu;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération du compte rendu : {ex.Message}");
                return null; // Retourner null en cas d'erreur
            }
        }


        public async Task AddCompteRenduAsync(CompteRendu compteRendu)
        {
            try
            {
                // Ajouter le compte rendu à la base de données
                _context.CompteRendus.Add(compteRendu);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'ajout du compte rendu : {ex.Message}");
                throw; // Relancer l'exception pour être gérée ailleurs
            }
        }


        public async Task UpdateCompteRenduAsync(CompteRendu compteRendu)
        {
            try
            {
                // Rechercher le compte rendu existant basé sur la période et le livretId
                var existingCompteRendu = await _context.CompteRendus
                    .FirstOrDefaultAsync(cr => cr.LivretId == compteRendu.LivretId && cr.Periode == compteRendu.Periode);

                if (existingCompteRendu != null)
                {
                    // Mettre à jour les propriétés du compte rendu
                    existingCompteRendu.ActivitesPro = compteRendu.ActivitesPro;
                    existingCompteRendu.ObservationsApprenti = compteRendu.ObservationsApprenti;
                    existingCompteRendu.ObservationsTuteur = compteRendu.ObservationsTuteur;
                    existingCompteRendu.ObservationsReferent = compteRendu.ObservationsReferent;

                    // Sauvegarder les modifications
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new Exception("Compte rendu introuvable pour cette période.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour du compte rendu : {ex.Message}");
                throw; // Relancer l'exception pour être gérée ailleurs
            }
        }

        public async Task UpdateLivretObservationsAsync(Livret livret)
        {
            var existingLivret = await _context.Livrets
                .FirstOrDefaultAsync(l => l.Id == livret.Id);

            if (existingLivret != null)
            {
                existingLivret.ObservationApprentiGlobal = livret.ObservationApprentiGlobal;
                existingLivret.ObservationAdmin = livret.ObservationAdmin;
                existingLivret.UpdatedAt = DateTime.UtcNow;  // Mettre à jour le champ UpdatedAt

                // Sauvegarder les changements dans la base de données
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddMatiereToGroupeAsync(ulong groupeId, ulong? matiereId)
        {
            // Vérifiez si une entrée pour ce groupe, matière et formateur existe déjà
            var existingRelation = await _context.GroupeMatieres
                .FirstOrDefaultAsync(gmf => gmf.GroupeId == groupeId && gmf.MatiereId == matiereId);

            // Si cette relation existe déjà, on n'ajoute rien
            if (existingRelation != null)
            {
                return; // Relation déjà présente, rien à faire
            }

            // Créez une nouvelle relation groupe-matière-formateur
            var newRelation = new GroupeMatiere
            {
                GroupeId = groupeId,
                MatiereId = (ulong)matiereId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };

            // Ajoutez la relation à la base de données
            _context.GroupeMatieres.Add(newRelation);

            // Sauvegardez les changements dans la base de données
            await _context.SaveChangesAsync();
        }

        public async Task<List<Matiere?>> GetMatieresForGroupeAsync(ulong groupeId)
        {
            try
            {
                // Récupère les matières associées à un groupe donné via la table pivot
                var matieres = await _context.GroupeMatieres
                    .Where(gm => gm.GroupeId == groupeId)
                    .Select(gm => gm.Matiere)
                    .ToListAsync();

                return matieres;
            }
            catch (Exception ex)
            {
                // Gérer l'exception selon votre besoin, par exemple enregistrer l'erreur dans un log
                throw new Exception($"Erreur lors de la récupération des matières pour le groupe {groupeId}: {ex.Message}");
            }
        }

        public async Task RemoveAllMatieresFromGroupeAsync(ulong groupeId)
        {
            try
            {
                // Récupérer toutes les relations entre le groupe et ses matières
                var groupeMatieres = await _context.GroupeMatieres
                    .Where(gm => gm.GroupeId == groupeId)
                    .ToListAsync();

                // Supprimer toutes les relations
                _context.GroupeMatieres.RemoveRange(groupeMatieres);

                // Sauvegarder les changements dans la base de données
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Gérer l'exception selon votre besoin
                throw new Exception($"Erreur lors de la suppression des matières pour le groupe {groupeId}: {ex.Message}");
            }
        }

        public async Task<List<Matiere>> GetMatieresWithFormateursAsync()
        {
            // Supposons que vous ayez une relation entre Matiere et Formateur dans la base de données.
            return await _context.Matieres
                                 .Include(m => m.Formateur) // Inclure les formateurs associés
                                 .ToListAsync();
        }

        public async Task<List<Personnel>> GetPersonnelsAsync()
        {
            return await _context.Personnels.ToListAsync();

        }

        public async Task DeletePersonnelAsync(ulong personnelId)
        {
            var personnel = await _context.Personnels.FindAsync(personnelId);
            var personnelSite = await _context.PersonnelSites
                .Where(ps => ps.PersonnelId == personnelId)
                .FirstOrDefaultAsync();
            if (personnel != null)
            {
                _context.PersonnelSites.Remove(personnelSite);
                _context.Personnels.Remove(personnel);
                await _context.SaveChangesAsync();
            }

        }

        public async Task AddPersonnelAsync(Personnel personnel)
        {
            try
            {
                _context.Personnels.Add(personnel);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'ajout du personnel : {ex.Message}", ex);
            }
        }

        public ILogger Get_logger()
        {
            return _logger;
        }

        public async Task AddPersonnelSiteAsync(ulong personnel_id, ulong site_id)
        {
                var personnelsite = new PersonnelSite
                {
                    PersonnelId = personnel_id,
                    SiteId = site_id
                };

                _context.PersonnelSites.Add(personnelsite);
                await _context.SaveChangesAsync();
        }

        public async Task ClearPersonnelSitesAsync(ulong personnelId)
        {
            // Fetch all PersonnelSite entries for the given personnel
            var personnelSites = _context.PersonnelSites.Where(ps => ps.PersonnelId == personnelId);

            // Remove all fetched entries
            _context.PersonnelSites.RemoveRange(personnelSites);

            // Save changes to the database
            await _context.SaveChangesAsync();
        }


        public async Task<Personnel?> GetPersonnelByIdAsync(ulong personnelId)
        {
            try
            {
                // Fetch the personnel record from the database
                var personnel = await _context.Personnels
                    .Where(p => p.Id == personnelId)
                    .FirstOrDefaultAsync();

                return personnel ?? throw new Exception("Personnel not found.");
            }
            catch (Exception ex)
            {
                // Handle exceptions, such as database errors
                throw new Exception($"An error occurred while fetching the personnel: {ex.Message}");
            }
        }

        public async Task UpdatePersonnelAsync(Personnel personnel)
        {
            try
            {
                // Fetch the personnel record from the database by ID
                var existingPersonnel = await _context.Personnels
                    .Where(p => p.Id == personnel.Id)
                    .FirstOrDefaultAsync() ?? throw new Exception("Personnel not found.");

                // Update the properties of the existing personnel
                existingPersonnel.Nom = personnel.Nom;
                existingPersonnel.Prenom = personnel.Prenom;
                existingPersonnel.Telephone = personnel.Telephone;
                existingPersonnel.Mail = personnel.Mail;
                existingPersonnel.Description = personnel.Description;

                // Save the changes to the database
                _context.Personnels.Update(existingPersonnel);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Handle exceptions, such as database errors
                throw new Exception($"An error occurred while updating the personnel: {ex.Message}");
            }
        }

        public async Task<List<LivretApprentissageContext.Site>> GetSitesAsync()
        {
            try
            {
                return await _context.Sites.ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération des sites : {ex.Message}", ex);
            }
        }

        // Supprimer un site
        public async Task DeleteSiteAsync(ulong siteId)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId) ?? throw new Exception("Site introuvable.");
                var personnelSites = await _context.PersonnelSites
                    .Where(ps => ps.SiteId == site.Id)
                    .ToListAsync();
                var modeles = await _context.Modeles
                    .Where(m => m.SiteId == site.Id)
                    .ToListAsync();
                foreach (var personnelSite in personnelSites)
                {
                    _context.PersonnelSites.Remove(personnelSite);
                }
                foreach (var modele in modeles)
                {
                    _context.Modeles.Remove(modele);
                }
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la suppression du site : {ex.Message}", ex);
            }
        }

        public async Task AddSiteAsync(LivretApprentissageContext.Site newSite)
        {
            try
            {
                // Ajouter le nouveau site à la base de données
                await _context.Sites.AddAsync(newSite);

                // Sauvegarder les changements
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'ajout du site : {ex.Message}", ex);
            }
        }

        public async Task<LivretApprentissageContext.Site> GetSiteByIdAsync(ulong siteId)
        {
            try
            {
                // Rechercher le site par son ID
                var site = await _context.Sites.FindAsync(siteId) ?? throw new Exception($"Aucun site trouvé avec l'ID {siteId}");
                return site;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération du site : {ex.Message}", ex);
            }
        }

        public async Task UpdateSiteAsync(LivretApprentissageContext.Site updatedSite)
        {
            try
            {
                // Vérifier si le site existe dans la base de données
                var existingSite = await _context.Sites.FindAsync(updatedSite.Id) ?? throw new Exception($"Aucun site trouvé avec l'ID {updatedSite.Id}");

                // Mettre à jour les propriétés nécessaires
                existingSite.Nom = updatedSite.Nom;
                existingSite.UpdatedAt = DateTime.UtcNow;

                // Sauvegarder les modifications
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la mise à jour du site : {ex.Message}", ex);
            }
        }

        public async Task<List<Modele>> GetModelesAsync()
        {
            try
            {
                return await _context.Modeles
                    .Include(m => m.Groupe) // Charger la relation avec Groupe si nécessaire
                    .Include(m => m.Site)   // Charger la relation avec Site si nécessaire
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des modèles : {ex.Message}");
                throw;
            }
        }

        public async Task DeleteModeleAsync(ulong? modeleId)
        {
            try
            {
                var modele = await _context.Modeles.FindAsync(modeleId) ?? throw new Exception($"Modèle avec l'ID {modeleId} non trouvé.");
                var livrets = await _context.Livrets
                    .Where(l => l.ModeleId == modele.Id)
                    .ToListAsync();
                var compositions = await _context.Compositions
                    .Where(c => c.ModeleId == modeleId)
                    .ToListAsync();
                foreach (Composition composition in compositions)
                {
                    if (composition.Lien != null)
                    {
                        // Construire le chemin absolu du fichier PDF
                        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        var projectRoot = Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "wwwroot");
                        var wwwrootPath = Path.GetFullPath(projectRoot);
                        var pdfPath = Path.Combine(wwwrootPath, composition.Lien);

                        // Vérifier si le fichier existe et le supprimer
                        if (File.Exists(pdfPath))
                        {
                            File.Delete(pdfPath);
                        }
                    }

                    // Supprimer la composition de la base de données
                    _context.Compositions.Remove(composition);


                    foreach (var livret in livrets)
                    {
                        _context.Livrets.Remove(livret);
                    }
                    
                    
                }
                _context.Modeles.Remove(modele);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression du modèle : {ex.Message}");
                throw;
            }
        }

        public async Task<Modele?> GetModeleByIdAsync(ulong id)
        {
            try
            {
                return await _context.Modeles
                    .Include(m => m.Site)  // Inclure les données liées au site
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération du modèle avec l'ID {id}: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateModeleAsync(Modele modele)
        {
            try
            {
                var existingModele = await _context.Modeles.FindAsync(modele.Id);

                if (existingModele == null)
                {
                    Console.WriteLine($"Le modèle avec l'ID {modele.Id} n'a pas été trouvé.");
                    return;
                }

                // Mettre à jour les propriétés du modèle
                existingModele.Nom = modele.Nom;
                existingModele.Groupe = modele.Groupe;
                existingModele.SiteId = modele.SiteId;

                // Sauvegarder les modifications dans la base de données
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour du modèle avec l'ID {modele.Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<int> AddModeleAsync(Modele modele)
        {
            modele.CreatedAt = DateTime.UtcNow;
            modele.UpdatedAt = DateTime.UtcNow;
            _context.Modeles.Add(modele);
            await _context.SaveChangesAsync();
            return (int)modele.Id;
        }

        public async Task AddCompositionAsync(Composition composition)
        {
            _context.Compositions.Add(composition);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Composition>> GetCompositionsByModeleIdAsync(ulong modeleId)
        {
            try
            {
                // Fetch compositions linked to the given Modele ID
                return await _context.Compositions
                    .Where(c => c.ModeleId == modeleId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching compositions for Modele ID {modeleId}: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateCompositionAsync(Composition updatedComposition)
        {
            try
            {
                var existingComposition = await _context.Compositions.FindAsync(updatedComposition.Id) ?? throw new InvalidOperationException($"Composition with ID {updatedComposition.Id} not found");

                // Update fields
                existingComposition.Nom = updatedComposition.Nom;
                existingComposition.Lien = updatedComposition.Lien;
                existingComposition.UpdatedAt = updatedComposition.UpdatedAt;

                // Save changes to the database
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating composition with ID {updatedComposition.Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<LivretApprentissageContext.Entreprise>> GetEntreprisesAsync()
        {
            try
            {
                return await _context.Entreprises
                    .Include(e => e.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération des entreprises : {ex.Message}", ex);
            }
        }

        public async Task DeleteEntrepriseAsync(ulong entrepriseId)
        {
            try
            {
                var entreprise = await _context.Entreprises.FindAsync(entrepriseId) ?? throw new Exception("Entreprise introuvable.");
                _context.Entreprises.Remove(entreprise);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la suppression du site : {ex.Message}", ex);
            }
        }

        public async Task AddEntrepriseAsync(LivretApprentissageContext.Entreprise newEntreprise)
        {
            try
            {
                // Ajouter le nouveau site à la base de données
                await _context.Entreprises.AddAsync(newEntreprise);

                // Sauvegarder les changements
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'ajout de l'entreprise : {ex.Message}", ex);
            }
        }

        public async Task<List<User>> GetTuteursAsync()
        {
            try
            {
                // Step 1: Retrieve all assigned roles with the role name "referent"
                var tuteurIds = await _context.AssignedRoles
                    .Include(ar => ar.Role)  // Include the related Role entity
                    .Where(ar => ar.Role.Name == "tuteur")
                    .Select(ar => ar.EntityId)  // Select the IDs of users with the "referent" role
                    .ToListAsync();

                // Step 2: Retrieve users whose IDs match the referent IDs
                var users = await _context.Users
                    .Where(u => tuteurIds.Contains(u.Id))  // Filter users based on the referent IDs
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Erreur lors de la récupération des tuteurs.", ex);
            }
        }

        public async Task<Entreprise?> GetEntrepriseByIdAsync(ulong entrepriseId)
        {
            try
            {
                var entreprise = await _context.Entreprises
                    .Include(e => e.User)
                    .Where(f => f.Id == entrepriseId)  // Recherche un formateur avec cet ID
                    .FirstOrDefaultAsync();

                return entreprise;  // Retourne le formateur trouvé ou null si aucun formateur n'est trouvé
            }
            catch (Exception ex)
            {
                // Log l'erreur ou gérer comme nécessaire
                throw new Exception($"Erreur lors de la récupération de l'entreprise : {ex.Message}", ex);
            }
        }

        public async Task UpdateEntrepriseAsync(Entreprise entreprise)
        {
            // Vérifier si la matière existe dans la base de données
            var existingEntreprise = await _context.Entreprises
                                                  .Include(m => m.User) // Inclure le formateur pour éviter les problèmes de navigation
                                                  .FirstOrDefaultAsync(m => m.Id == entreprise.Id);
            if (existingEntreprise != null)
            {
                // Mettre à jour les propriétés de la matière
                existingEntreprise.Nom = entreprise.Nom;
                existingEntreprise.Adresse = entreprise.Adresse;
                existingEntreprise.Telephone = entreprise.Telephone;
                existingEntreprise.UserId = entreprise.UserId;


                // Sauvegarder les changements dans la base de données
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Entreprise non trouvée");
            }
        }
    }
}