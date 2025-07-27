using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class PetQuestion
{
    public Guid Id { get; set; }

    public int Position { get; set; }

    public PetQuestionType Type { get; set; }

    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;
}