using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class PetQuestionDto
{
    public int Position { get; set; }

    public PetQuestionType Type { get; set; }

    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;

    public List<string>? Choices { get; set; }
}