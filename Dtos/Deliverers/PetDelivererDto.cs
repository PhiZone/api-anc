using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Deliverers;

public class PetDelivererDto
{
    public List<PetQuestionDeliverer> Questions { get; set; } = new();

    public int Score { get; set; }

    public DateTimeOffset DateStarted { get; set; }
}

public class PetQuestionDeliverer
{
    public Guid Id { get; set; }

    public PetQuestionType Type { get; set; }

    public List<PetChoiceDeliverer>? Choices { get; set; }
}

public class PetChoiceDeliverer
{
    public Guid Id { get; set; }

    public bool IsCorrect { get; set; }
}