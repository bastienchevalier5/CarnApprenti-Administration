using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Data.Common;
using System.Data;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace CarnApprenti
{
    public class LivretApprentissageContext : DbContext
    {
        public LivretApprentissageContext()
        {
        }

        public LivretApprentissageContext(DbContextOptions<LivretApprentissageContext> options)
       : base(options)
        {

        }

        // DbSets pour chaque table
        public DbSet<User> Users { get; set; }
        public DbSet<Groupe> Groupes { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AssignedRole> AssignedRoles { get; set; }
        public DbSet<CompteRendu> CompteRendus { get; set; }
        public DbSet<Livret> Livrets { get; set; }
        public DbSet<Modele> Modeles { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<GroupeWithReferent> GroupeWithReferents { get; set; }
        public DbSet<Formateur> Formateurs { get; set; }
        public DbSet<Matiere> Matieres { get; set; }
        public DbSet<Composition> Compositions { get; set; }
        public DbSet<Personnel> Personnels { get; set; }
        public DbSet<GroupeMatiere> GroupeMatieres { get; set; }
        public DbSet<PersonnelSite> PersonnelSites { get; set; }
        public DbSet<Entreprise> Entreprises { get; set; }

        // Fluent API Configurations can be added here (if needed)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration remains unchanged
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);

                entity.HasOne(u => u.Groupe)
                    .WithMany()
                    .HasForeignKey(u => u.GroupeId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.Apprenant)
                    .WithMany()
                    .HasForeignKey(u => u.ApprenantId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AssignedRole configuration
            modelBuilder.Entity<AssignedRole>(entity =>
            {
                entity.ToTable("assigned_roles");
                entity.HasKey(e => e.Id);

                entity.HasOne(ar => ar.Role)
                    .WithMany(r => r.AssignedRoles)
                    .HasForeignKey(ar => ar.RoleId);

                entity.HasOne(ar => ar.Entity)
                    .WithMany(u => u.AssignedRoles)
                    .HasForeignKey(ar => ar.EntityId)
                    .HasPrincipalKey(u => u.Id);
            });

            // Role configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.Id);
            });
        }

        [Table("compte_rendus")]
        public class CompteRendu
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("livret_id")]
            public ulong LivretId { get; set; }  // LivretId non nullable

            [Column("periode")]
            public string? Periode { get; set; }

            [Column("activites_pro")]
            public string? ActivitesPro { get; set; }

            [Column("observations_apprenti")]
            public string? ObservationsApprenti { get; set; }

            [Column("observations_tuteur")]
            public string? ObservationsTuteur { get; set; }

            [Column("observations_referent")]
            public string? ObservationsReferent { get; set; }

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }  // DateTime non nullable

            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; }  // DateTime non nullable
        }

        [Table("groupes")]
        public class Groupe
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string? Nom { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }

        }

        [Table("livrets")]
        public class Livret
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("observation_apprenti_global")]
            public string? ObservationApprentiGlobal { get; set; }

            [Column("observation_admin")]
            public string? ObservationAdmin { get; set; }

            [Column("lien")]
            public string? Lien { get; set; }

            [Column("user_id")]
            public ulong? UserId { get; set; }

            [Column("modele_id")]
            public ulong? ModeleId { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            public User? User { get; set; }
            public Modele? Modele { get; set; }
        }

        [Table("modeles")]
        public class Modele
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string? Nom { get; set; }

            [Column("lien")]
            public string? Lien { get; set; }

            [Column("groupe_id")]
            public ulong GroupeId { get; set; }

            public Groupe? Groupe { get; set; }

            [Column("site_id")]
            public ulong SiteId { get; set; }

            public Site? Site { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }
        }

        [Table("roles")]
        public class Role
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("name")]
            public string? Name { get; set; }

            [Column("title")]
            public string? Title { get; set; }

            [Column("scope")]
            public int? Scope { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }
            public ICollection<AssignedRole>? AssignedRoles { get; set; }

        }

        [Table("sites")]
        public class Site
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string? Nom { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }

        }

        [Table("assigned_roles")]
        public class AssignedRole
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("role_id")]
            public ulong RoleId { get; set; }

            [Column("entity_id")]
            public ulong EntityId { get; set; }

            [Column("entity_type")]
            public string? EntityType { get; set; }

            [Column("restricted_to_id")]
            public ulong? RestrictedToId { get; set; }

            [Column("restricted_to_type")]
            public string? RestrictedToType { get; set; }

            [Column("scope")]
            public int? Scope { get; set; }


            public virtual Role? Role { get; set; }

            // Navigation vers l'entité liée (qui sera probablement User)
            public virtual User? Entity { get; set; }
        }


        [Table("users")]
        public class User
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string? Nom { get; set; }

            [Column("prenom")]
            public string? Prenom { get; set; }

            [Column("email")]
            [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "L'email n'est pas valide.")]
            public string? Email { get; set; }

            [Column("groupe_id")]
            public ulong? GroupeId { get; set; }

            [Column("apprenant_id")]
            public ulong? ApprenantId { get; set; }

            [Column("email_verified_at")]
            public DateTime? EmailVerifiedAt { get; set; }

            [Column("password")]
            [Required]
            public string? Password { get; set; }

            [Column("remember_token")]
            public string? RememberToken { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            public virtual Groupe? Groupe { get; set; }
            public virtual User? Apprenant { get; set; }
            public virtual List<AssignedRole>? AssignedRoles { get; set; } // Association des rôles

        }

        public class GroupeWithReferent
        {
            public ulong Id { get; set; }
            public string Nom { get; set; } = string.Empty;
            public User? Referent { get; set; }
        }

        [Table("formateurs")]
        public class Formateur
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string? Nom { get; set; }

            [Column("prenom")]
            public string? Prenom { get; set; }

        }

        [Table("matieres")]
        public class Matiere
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string? Nom { get; set; }

            [Column("formateur_id")]
            public ulong FormateurId { get; set; }

            public Formateur? Formateur { get; set; }
        }

        [Table("compositions")]
        public class Composition
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string? Nom { get; set; }

            [Column("lien")]
            public string? Lien { get; set; }

            [Column("modele_id")]
            public ulong ModeleId { get; set; }

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; }
        }

        [Table("personnels")]
        public class Personnel
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }


            [Column("nom")]
            public string? Nom { get; set; }


            [Column("prenom")]
            public string? Prenom { get; set; }


            [Column("mail")]
            public string? Mail { get; set; }

            [Column("description")]
            public string? Description { get; set; }


            [Column("telephone")]
            public string? Telephone { get; set; }

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; }

            public List<PersonnelSite>? PersonnelSites { get; set; } = [];
        }


        [Table("personnel_site")]
        public class PersonnelSite
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("personnel_id")]
            public ulong PersonnelId { get; set; }

            [Column("site_id")]
            public ulong SiteId { get; set; }
        }



        [Table("groupe_matiere")]
        public class GroupeMatiere
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("groupe_id")]
            public ulong GroupeId { get; set; }

            [Column("matiere_id")]
            public ulong MatiereId { get; set; }

            public Matiere? Matiere { get; set; }

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; }
        }

        [Table("entreprises")]
        public class Entreprise
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string Nom { get; set; }

            [Column("adresse")]
            public string Adresse { get; set; }

            [Column("telephone")]
            public string Telephone { get; set; }

            [Column("user_id")]
            public ulong UserId { get; set; }

           public User User { get; set; }

        }
    }
}