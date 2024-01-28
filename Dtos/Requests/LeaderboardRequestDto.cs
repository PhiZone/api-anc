using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class LeaderboardRequestDto
{
    [Range(1, 1000, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int TopRange { get; set; } = 10;

    [Range(0, 50, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int NeighborhoodRange { get; set; } = 1;
}