namespace TrackUp.Api_.Domain.Model
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string DeviceName { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        // Store the hash, never the raw refresh token.
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? ReplacedByTokenHash { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}