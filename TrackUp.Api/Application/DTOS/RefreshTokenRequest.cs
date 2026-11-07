using System.ComponentModel.DataAnnotations;

namespace TrackUp.Api_.Application.DTOS;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;
}
