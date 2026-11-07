using System.ComponentModel.DataAnnotations;

namespace TrackUp.Api_.Application.DTOS;

public sealed class RevokeSessionRequest
{
    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }
}
