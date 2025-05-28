namespace PhiZoneApi.Dtos.ChartFormats;

public class PecDto : ChartFormatDto
{
    public int Offset { get; set; }

    public List<BpmCommand> BpmCommands { get; init; } = null!;

    public List<NoteCommand> NoteCommands { get; init; } = null!;

    public List<SpeedCommand> SpeedCommands { get; init; } = null!;

    public List<MoveCommand> MoveCommands { get; init; } = null!;

    public List<RotationCommand> RotationCommands { get; init; } = null!;

    public List<AlphaCommand> AlphaCommands { get; init; } = null!;

    public List<DurationalMoveCommand> DurationalMoveCommands { get; init; } = null!;

    public List<DurationalRotationCommand> DurationalRotationCommands { get; init; } = null!;

    public List<DurationalAlphaCommand> DurationalAlphaCommands { get; init; } = null!;
}

public class BpmCommand : Command
{
    public new const string Id = "bp";

    public double Bpm { get; init; }
}

public class NoteCommand : DurationalCommand
{
    public new double? EndTime { get; init; }

    public double PositionX { get; init; }

    public int Direction { get; init; }

    public bool IsFake { get; init; }

    public double Speed { get; set; }

    public double Size { get; set; }
}

public class SpeedCommand : Command
{
    public new const string Id = "cv";

    public double Speed { get; init; }
}

public class MoveCommand : Command
{
    public new const string Id = "cp";

    public double X { get; init; }

    public double Y { get; init; }
}

public class RotationCommand : Command
{
    public new const string Id = "cd";

    public double Arc { get; init; }
}

public class AlphaCommand : Command
{
    public new const string Id = "ca";

    public double Alpha { get; init; }
}

public class DurationalMoveCommand : DurationalCommand
{
    public new const string Id = "cm";

    public double X { get; init; }

    public double Y { get; init; }

    public int MotionType { get; init; }
}

public class DurationalRotationCommand : DurationalCommand
{
    public new const string Id = "cr";

    public double Arc { get; init; }

    public int MotionType { get; init; }
}

public class DurationalAlphaCommand : DurationalCommand
{
    public new const string Id = "cf";

    public double Alpha { get; init; }
}

public class DurationalCommand : Command
{
    public double EndTime { get; init; }
}

public class Command : IComparable<Command>
{
    public string Id { get; init; } = null!;

    public int JudgeLine { get; init; }

    public double Time { get; init; }

    public int CompareTo(Command? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var idComparison = string.Compare(Id, other.Id, StringComparison.Ordinal);
        if (idComparison != 0) return idComparison;
        var judgeLineComparison = JudgeLine.CompareTo(other.JudgeLine);
        return judgeLineComparison != 0 ? judgeLineComparison : Time.CompareTo(other.Time);
    }
}