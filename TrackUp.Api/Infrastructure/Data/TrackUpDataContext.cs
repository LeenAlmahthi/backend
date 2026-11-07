using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrackUp.Api_.Domain.Model;

namespace TrackUp.Api_.Infrastructure.Data
{
    public class TrackUpDataContext : IdentityDbContext<ApplicationUser>
    {
        public TrackUpDataContext(
            DbContextOptions<TrackUpDataContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(token => token.Id);

                entity.HasIndex(token => token.TokenHash)
                    .IsUnique();

                entity.Property(token => token.TokenHash)
                    .IsRequired();

                entity.HasOne(token => token.User)
                    .WithMany()
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}