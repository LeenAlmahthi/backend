using System.ComponentModel.DataAnnotations;

namespace TrackUp.Api_.Application.DTOS;

public sealed class GoogleSignUpRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
    public string? Nonce { get; set; }

    [StringLength(50, MinimumLength = 2)]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    public string? LastName { get; set; }
}
