using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Models;

namespace CapstoneProjectPUSH_API.Data
{
    public class PushDbContext : DbContext
    {
        public PushDbContext(DbContextOptions<PushDbContext> options) : base(options)
        {
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Gym> Gyms { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<WeightLog> WeightLogs { get; set; }
        public DbSet<Workout> Workouts { get; set; }
        public DbSet<WorkoutExercise> WorkoutExercises { get; set; }
        public DbSet<WorkoutFavourite> WorkoutFavourites { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique values
            modelBuilder.Entity<Category>().HasIndex(c => c.Title).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.DisplayName).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<WorkoutFavourite>().HasIndex(f => new { f.UserId, f.WorkoutId }).IsUnique();

            // Indexes for the ID columns the app searches by
            modelBuilder.Entity<Exercise>().HasIndex(e => e.CategoryId);
            modelBuilder.Entity<Note>().HasIndex(n => n.UserId);
            modelBuilder.Entity<Note>().HasIndex(n => n.WorkoutId);
            modelBuilder.Entity<PasswordReset>().HasIndex(p => p.UserId);
            modelBuilder.Entity<Report>().HasIndex(r => r.WorkoutId);
            modelBuilder.Entity<Report>().HasIndex(r => r.UserId);
            modelBuilder.Entity<Report>().HasIndex(r => r.IsResolved);
            modelBuilder.Entity<Trainer>().HasIndex(t => t.CategoryId);
            modelBuilder.Entity<Trainer>().HasIndex(t => t.GymId);
            modelBuilder.Entity<User>().HasIndex(u => u.GymId);
            modelBuilder.Entity<WeightLog>().HasIndex(w => w.UserId);
            modelBuilder.Entity<Workout>().HasIndex(w => w.UserId);
            modelBuilder.Entity<Workout>().HasIndex(w => w.CategoryId);
            modelBuilder.Entity<WorkoutExercise>().HasIndex(we => we.WorkoutId);
            modelBuilder.Entity<WorkoutExercise>().HasIndex(we => we.ExerciseId);
            modelBuilder.Entity<WorkoutLog>().HasIndex(l => l.WorkoutId);
            modelBuilder.Entity<WorkoutLog>().HasIndex(l => l.UserId);
        }
    }
}
