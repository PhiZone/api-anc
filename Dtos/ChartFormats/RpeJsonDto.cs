using Newtonsoft.Json;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Dtos.ChartFormats;

public class RpeJsonDto : ChartFormatDto
{
    [JsonProperty("BPMList")] public List<BpmInfo> BpmList { get; set; } = null!;

    [JsonProperty("META")] public Meta Meta { get; set; } = null!;

    [JsonProperty("judgeLineGroup")] public List<string> JudgeLineGroup { get; set; } = null!;

    [JsonProperty("judgeLineList")] public List<JudgeLine> JudgeLineList { get; set; } = null!;
}

public class Meta
{
    [JsonProperty("RPEVersion")] public int RpeVersion { get; set; }

    [JsonProperty("background")] public string Background { get; set; } = null!;

    [JsonProperty("charter")] public string Charter { get; set; } = null!;

    [JsonProperty("composer")] public string Composer { get; set; } = null!;

    [JsonProperty("id")] public string Id { get; set; } = null!;

    [JsonProperty("level")] public string Level { get; set; } = null!;

    [JsonProperty("name")] public string Name { get; set; } = null!;

    [JsonProperty("offset")] public int Offset { get; set; }

    [JsonProperty("song")] public string Song { get; set; } = null!;
}

public class BpmInfo : IComparable<BpmInfo>
{
    [JsonProperty("bpm")] public double Bpm { get; set; }

    [JsonProperty("startTime")] public List<int> StartTime { get; set; } = null!;

    public int CompareTo(BpmInfo? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var startTime = ChartUtil.ConvertTime(StartTime);
        var otherStartTime = ChartUtil.ConvertTime(other.StartTime);
        return startTime.CompareTo(otherStartTime);
    }
}

public class JudgeLine
{
    [JsonProperty("Group")] public int Group { get; set; }

    [JsonProperty("Name")] public string Name { get; set; } = null!;

    [JsonProperty("Texture")] public string Texture { get; set; } = null!;

    [JsonProperty("alphaControl")] public List<AlphaControl>? AlphaControl { get; set; }

    [JsonProperty("bpmfactor")] public double BpmFactor { get; set; }

    [JsonProperty("eventLayers")] public List<EventLayer>? EventLayers { get; set; }

    [JsonProperty("extended")] public ExtendedEventLayer? Extended { get; set; }

    [JsonProperty("father")] public int Father { get; set; }

    [JsonProperty("isCover")] public int IsCover { get; set; }

    [JsonProperty("notes")] public List<Note>? Notes { get; set; }

    [JsonProperty("numOfNotes")] public int NumOfNotes { get; set; }

    [JsonProperty("posControl")] public List<PosControl>? PosControl { get; set; }

    [JsonProperty("sizeControl")] public List<SizeControl>? SizeControl { get; set; }

    [JsonProperty("skewControl")] public List<SkewControl>? SkewControl { get; set; }

    [JsonProperty("yControl")] public List<YControl>? YControl { get; set; }

    [JsonProperty("zOrder")] public int ZOrder { get; set; }
}

public class Note : IComparable<Note>
{
    [JsonProperty("above")] public int Above { get; set; }

    [JsonProperty("alpha")] public int Alpha { get; set; }

    [JsonProperty("endTime")] public List<int> EndTime { get; set; } = null!;

    [JsonProperty("isFake")] public int IsFake { get; set; }

    [JsonProperty("positionX")] public double PositionX { get; set; }

    [JsonProperty("size")] public double Size { get; set; }

    [JsonProperty("speed")] public double Speed { get; set; }

    [JsonProperty("startTime")] public List<int> StartTime { get; set; } = null!;

    [JsonProperty("type")] public int Type { get; set; }

    [JsonProperty("visibleTime")] public double VisibleTime { get; set; }

    [JsonProperty("yOffset")] public double YOffset { get; set; }

    public int CompareTo(Note? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var startTime = ChartUtil.ConvertTime(StartTime);
        var otherStartTime = ChartUtil.ConvertTime(other.StartTime);
        var startTimeComparison = startTime.CompareTo(otherStartTime);
        if (startTimeComparison != 0) return startTimeComparison;
        var endTime = ChartUtil.ConvertTime(EndTime);
        var otherEndTime = ChartUtil.ConvertTime(other.EndTime);
        return endTime.CompareTo(otherEndTime);
    }
}

public class EventLayer
{
    [JsonProperty("alphaEvents")] public List<AlphaEvent>? AlphaEvents { get; set; }

    [JsonProperty("moveXEvents")] public List<Event>? MoveXEvents { get; set; }

    [JsonProperty("moveYEvents")] public List<Event>? MoveYEvents { get; set; }

    [JsonProperty("rotateEvents")] public List<Event>? RotateEvents { get; set; }

    [JsonProperty("speedEvents")] public List<SpeedEvent>? SpeedEvents { get; set; }
}

public class ExtendedEventLayer
{
    [JsonProperty("colorEvents")] public List<ColorEvent>? ColorEvents { get; set; }

    [JsonProperty("inclineEvents")] public List<Event>? InclineEvents { get; set; }

    [JsonProperty("paintEvents")] public List<Event>? PaintEvents { get; set; }

    [JsonProperty("scaleXEvents")] public List<Event>? ScaleXEvents { get; set; }

    [JsonProperty("scaleYEvents")] public List<Event>? ScaleYEvents { get; set; }

    [JsonProperty("textEvents")] public List<TextEvent>? TextEvents { get; set; }
}

public class AlphaEvent : Event
{
    [JsonProperty("end")] public new int End { get; set; }

    [JsonProperty("start")] public new int Start { get; set; }
}

public class ColorEvent : Event
{
    [JsonProperty("end")] public new List<int> End { get; set; } = null!;

    [JsonProperty("start")] public new List<int> Start { get; set; } = null!;
}

public class TextEvent : Event
{
    [JsonProperty("end")] public new string End { get; set; } = null!;

    [JsonProperty("start")] public new string Start { get; set; } = null!;
}

public class Event : IComparable<Event>
{
    [JsonProperty("bezier")] public int Bezier { get; set; }

    [JsonProperty("bezierPoints")] public List<double> BezierPoints { get; set; } = null!;

    [JsonProperty("easingLeft")] public double EasingLeft { get; set; }

    [JsonProperty("easingRight")] public double EasingRight { get; set; }

    [JsonProperty("easingType")] public int EasingType { get; set; }

    [JsonProperty("end")] public double End { get; set; }

    [JsonProperty("endTime")] public List<int> EndTime { get; set; } = null!;

    [JsonProperty("linkgroup")] public int LinkGroup { get; set; }

    [JsonProperty("start")] public double Start { get; set; }

    [JsonProperty("startTime")] public List<int> StartTime { get; set; } = null!;

    public int CompareTo(Event? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var startTime = ChartUtil.ConvertTime(StartTime);
        var otherStartTime = ChartUtil.ConvertTime(other.StartTime);
        var startTimeComparison = startTime.CompareTo(otherStartTime);
        if (startTimeComparison != 0) return startTimeComparison;
        var endTime = ChartUtil.ConvertTime(EndTime);
        var otherEndTime = ChartUtil.ConvertTime(other.EndTime);
        return endTime.CompareTo(otherEndTime);
    }
}

public class SpeedEvent : IComparable<SpeedEvent>
{
    [JsonProperty("end")] public double End { get; set; }

    [JsonProperty("endTime")] public List<int> EndTime { get; set; } = null!;

    [JsonProperty("linkgroup")] public int LinkGroup { get; set; }

    [JsonProperty("start")] public double Start { get; set; }

    [JsonProperty("startTime")] public List<int> StartTime { get; set; } = null!;

    public int CompareTo(SpeedEvent? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var startTime = ChartUtil.ConvertTime(StartTime);
        var otherStartTime = ChartUtil.ConvertTime(other.StartTime);
        var startTimeComparison = startTime.CompareTo(otherStartTime);
        if (startTimeComparison != 0) return startTimeComparison;
        var endTime = ChartUtil.ConvertTime(EndTime);
        var otherEndTime = ChartUtil.ConvertTime(other.EndTime);
        return endTime.CompareTo(otherEndTime);
    }
}

public class AlphaControl : Control
{
    [JsonProperty("alpha")] public double Alpha { get; set; }
}

public class PosControl : Control
{
    [JsonProperty("pos")] public double Pos { get; set; }
}

public class SizeControl : Control
{
    [JsonProperty("size")] public double Size { get; set; }
}

public class SkewControl : Control
{
    [JsonProperty("skew")] public double Skew { get; set; }
}

public class YControl : Control
{
    [JsonProperty("y")] public double Y { get; set; }
}

public class Control
{
    [JsonProperty("easing")] public int Easing { get; set; }

    [JsonProperty("x")] public double X { get; set; }
}