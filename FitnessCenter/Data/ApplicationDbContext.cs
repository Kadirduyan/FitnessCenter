using FitnessCenter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FitnessCenterModel> FitnessCenters { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Service>()
                .HasOne(s => s.FitnessCenter)
                .WithMany(fc => fc.Services)
                .HasForeignKey(s => s.FitnessCenterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Trainer>()
                .HasOne(t => t.FitnessCenter)
                .WithMany(fc => fc.Trainers)
                .HasForeignKey(t => t.FitnessCenterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TrainerAvailability>()
                .HasOne(ta => ta.Trainer)
                .WithMany(t => t.Availabilities)
                .HasForeignKey(ta => ta.TrainerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Appointment>()
                .HasOne(a => a.Member)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Trainer)
                .WithMany(t => t.Appointments)
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-many relationship between Trainer and Service
            builder.Entity<Trainer>()
                .HasMany(t => t.Services)
                .WithMany(s => s.Trainers)
                .UsingEntity<Dictionary<string, object>>(
                    "TrainerService",
                    j => j.HasOne<Service>().WithMany().HasForeignKey("ServiceId").OnDelete(DeleteBehavior.NoAction),
                    j => j.HasOne<Trainer>().WithMany().HasForeignKey("TrainerId").OnDelete(DeleteBehavior.NoAction),
                    j => j.HasKey("TrainerId", "ServiceId"));

        }
    }
}

