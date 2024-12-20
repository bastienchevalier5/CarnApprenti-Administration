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

        public LivretApprentissageContext(DbContextOptions<LivretApprentissageContext> options)
       : base(options)
        {

        }

        // DbSets pour chaque table
        public DbSet<User> Users { get; set; }
        public DbSet<Groupe> Groupes { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AssignedRole> AssignedRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Ability> Abilities { get; set; }
        public DbSet<Cache> Caches { get; set; }
        public DbSet<CacheLock> CacheLocks { get; set; }
        public DbSet<CompteRendu> CompteRendus { get; set; }
        public DbSet<FailedJob> FailedJobs { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobBatch> JobBatches { get; set; }
        public DbSet<Livret> Livrets { get; set; }
        public DbSet<Migration> Migrations { get; set; }
        public DbSet<Modele> Modeles { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<GroupeWithReferent> GroupeWithReferents { get; set; }
        public DbSet<Formateur> Formateurs { get; set; }
        public DbSet<Matiere> Matieres { get; set; }

        // Fluent API Configurations can be added here (if needed)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration de User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);

                // Relation avec Groupe
                entity.HasOne(u => u.Groupe)
                    .WithMany()
                    .HasForeignKey(u => u.GroupeId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Relation avec Apprenant (self-referencing)
                entity.HasOne(u => u.Apprenant)
                    .WithMany()
                    .HasForeignKey(u => u.ApprenantId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuration de AssignedRole
            modelBuilder.Entity<AssignedRole>(entity =>
            {
                entity.ToTable("assigned_roles");
                entity.HasKey(e => e.Id);

                // Relation avec Role
                entity.HasOne(ar => ar.Role)
                    .WithMany(r => r.AssignedRoles)
                    .HasForeignKey(ar => ar.RoleId);

                // Relation avec User (Entity)
                entity.HasOne(ar => ar.Entity)
                    .WithMany(u => u.AssignedRoles)
                    .HasForeignKey(ar => ar.EntityId)
                .HasPrincipalKey(u => u.Id);

       

            });

            // Configuration de Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.Id);
            });
        }

    [Table("abilities")]
        public class Ability
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("name")]
            public string Name { get; set; }

            [Column("title")]
            public string Title { get; set; }

            [Column("entity_id")]
            public ulong? EntityId { get; set; }

            [Column("entity_type")]
            public string EntityType { get; set; }

            [Column("only_owned")]
            public bool OnlyOwned { get; set; }

            [Column("options")]
            public string Options { get; set; }

            [Column("scope")]
            public int? Scope { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }
        }

        [Table("cache")]
        public class Cache
        {
            [Key]
            [Column("key")]
            public string Key { get; set; }

            [Column("value")]
            public string Value { get; set; }

            [Column("expiration")]
            public int Expiration { get; set; }
        }

        [Table("cache_locks")]
        public class CacheLock
        {
            [Key]
            [Column("key")]
            public string Key { get; set; }

            [Column("owner")]
            public string Owner { get; set; }

            [Column("expiration")]
            public int Expiration { get; set; }
        }

        [Table("compte_rendus")]
        public class CompteRendu
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("livret_id")]
            public ulong? LivretId { get; set; }

            [Column("periode")]
            public string Periode { get; set; }

            [Column("activites_pro")]
            public string ActivitesPro { get; set; }

            [Column("observations_apprenti")]
            public string ObservationsApprenti { get; set; }

            [Column("observations_tuteur")]
            public string ObservationsTuteur { get; set; }

            [Column("observations_referent")]
            public string ObservationsReferent { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }
        }

        [Table("failed_jobs")]
        public class FailedJob
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("uuid")]
            public string Uuid { get; set; }

            [Column("connection")]
            public string Connection { get; set; }

            [Column("queue")]
            public string Queue { get; set; }

            [Column("payload")]
            public string Payload { get; set; }

            [Column("exception")]
            public string Exception { get; set; }

            [Column("failed_at")]
            public DateTime FailedAt { get; set; }
        }

        [Table("groupes")]
        public class Groupe
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string Nom { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }

        }

        [Table("job_batches")]
        public class JobBatch
        {
            [Key]
            [Column("id")]
            public string Id { get; set; }

            [Column("name")]
            public string Name { get; set; }

            [Column("total_jobs")]
            public int TotalJobs { get; set; }

            [Column("pending_jobs")]
            public int PendingJobs { get; set; }

            [Column("failed_jobs")]
            public int FailedJobs { get; set; }

            [Column("failed_jobs_ids")]
            public string FailedJobIds { get; set; }

            [Column("options")]
            public string Options { get; set; }

            [Column("cancelled_at")]
            public int? CancelledAt { get; set; }

            [Column("created_at")]
            public int CreatedAt { get; set; }

            [Column("finished_at")]
            public int? FinishedAt { get; set; }
        }

        [Table("jobs")]
        public class Job
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("queue")]
            public string Queue { get; set; }

            [Column("payload")]
            public string Payload { get; set; }

            [Column("attempts")]
            public byte Attempts { get; set; }

            [Column("reserved_at")]
            public uint? ReservedAt { get; set; }

            [Column("available_at")]
            public uint AvailableAt { get; set; }

            [Column("created_at")]
            public uint CreatedAt { get; set; }
        }

        [Table("livrets")]
        public class Livret
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("observation_apprenti_global")]
            public string ObservationApprentiGlobal { get; set; }

            [Column("observation_admin")]
            public string ObservationAdmin { get; set; }

            [Column("lien")]
            public string Lien { get; set; }

            [Column("user_id")]
            public ulong UserId { get; set; }

            [Column("modele_id")]
            public ulong ModeleId { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }
        }

        [Table("migrations")]
        public class Migration
        {
            [Key]
            [Column("id")]
            public uint Id { get; set; }

            [Column("migration")]
            public string MigrationName { get; set; }

            [Column("batch")]
            public int Batch { get; set; }
        }

        [Table("modeles")]
        public class Modele
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string Nom { get; set; }

            [Column("lien")]
            public string Lien { get; set; }

            [Column("groupe_id")]
            public ulong GroupeId { get; set; }

            [Column("site_id")]
            public ulong SiteId { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }
        }

        [Table("password_reset_tokens")]
        public class PasswordResetToken
        {
            [Key]
            [Column("email")]
            public string Email { get; set; }

            [Column("token")]
            public string Token { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }
        }

        [Table("roles")]
        public class Role
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("name")]
            public string Name { get; set; }

            [Column("title")]
            public string Title { get; set; }

            [Column("scope")]
            public int? Scope { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }
            public ICollection<Permission> RolePermissions { get; set; }
            public ICollection<AssignedRole> AssignedRoles { get; set; }

        }

        [Table("sessions")]
        public class Session
        {
            [Key]
            [Column("id")]
            public string Id { get; set; }

            [Column("user_id")]
            public ulong? UserId { get; set; }

            [Column("ip_address")]
            public string IpAddress { get; set; }

            [Column("user_agent")]
            public string UserAgent { get; set; }

            [Column("payload")]
            public string Payload { get; set; }

            [Column("last_activity")]
            public int LastActivity { get; set; }
        }

        [Table("sites")]
        public class Site
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string Nom { get; set; }

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
            public string EntityType { get; set; }

            [Column("restricted_to_id")]
            public ulong? RestrictedToId { get; set; }

            [Column("restricted_to_type")]
            public string RestrictedToType { get; set; }

            [Column("scope")]
            public int? Scope { get; set; }


            public virtual Role Role { get; set; }

            // Navigation vers l'entité liée (qui sera probablement User)
            public virtual User Entity { get; set; }
        }

        [Table("permissions")]
        public class Permission
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("ability_id")]
            public ulong AbilityId { get; set; }

            [Column("entity_id")]
            public ulong? EntityId { get; set; }

            [Column("entity_type")]
            public string EntityType { get; set; }

            [Column("forbidden")]
            public bool Forbidden { get; set; }

            [Column("scope")]
            public int? Scope { get; set; }

        }


        [Table("users")]
        public class User
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string ?Nom { get; set; }

            [Column("prenom")]
            public string ?Prenom { get; set; }

            [Column("email")]
            public string ?Email { get; set; }

            [Column("groupe_id")]
            public ulong? GroupeId { get; set; }

            [Column("apprenant_id")]
            public ulong? ApprenantId { get; set; }

            [Column("email_verified_at")]
            public DateTime? EmailVerifiedAt { get; set; }

            [Column("password")]
            [Required]
            public string ?Password { get; set; }

            [Column("remember_token")]
            public string ?RememberToken { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            public virtual Groupe ?Groupe { get; set; }
            public virtual User ?Apprenant { get; set; }
            public virtual List<AssignedRole> ?AssignedRoles { get; set; } // Association des rôles

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
            public string Nom { get; set; }

            [Column("prenom")]
            public string Prenom { get; set; }

        }

        [Table("matieres")]
        public class Matiere
        {
            [Key]
            [Column("id")]
            public ulong Id { get; set; }

            [Column("nom")]
            public string Nom { get; set; }

            [Column("formateur_id")]
            public ulong FormateurId { get; set; }

            public Formateur ?Formateur { get; set; }
        }
    }
}