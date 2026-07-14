using Microsoft.EntityFrameworkCore;
using UnoWebTemplate.Shared.Models;

namespace UnoWebTemplate.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<LogEntry> Logs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.ToTable("Logs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Level).HasMaxLength(50);
                entity.Property(e => e.Thread).HasMaxLength(255);
                entity.Property(e => e.Logger).HasMaxLength(255);
                entity.Property(e => e.Message).HasMaxLength(4000);
                entity.Property(e => e.Exception).HasMaxLength(2000);
            });
        }
    }
}
