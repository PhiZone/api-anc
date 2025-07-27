namespace PhiZoneApi.Models;

public class PetChoice
{
    public Guid Id { get; set; }

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public Guid QuestionId { get; set; }

    public PetQuestion Question { get; set; } = null!;
}