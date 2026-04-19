using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data.Entities;

namespace NeighborhoodContacts.Server.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Property> Properties { get; set; } = null!;
        public DbSet<PropertyGroup> PropertyGroups { get; set; } = null!; // new

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Username)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(u => u.Username)
                      .IsUnique();
            });

            modelBuilder.Entity<Property>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Address)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasIndex(p => p.Address)
                      .IsUnique();

                entity.HasOne(p => p.PropertyGroup)
                      .WithMany(pg => pg.Properties)
                      .HasForeignKey(p => p.PropertyGroupId)
                      .IsRequired();
            });

            modelBuilder.Entity<PropertyGroup>(entity =>
            {
                entity.HasKey(pg => pg.Id);

                entity.Property(pg => pg.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(pg => pg.Name)
                      .IsUnique();
            });
        }
    }
}