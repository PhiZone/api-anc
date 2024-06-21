using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.ChartFormats;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public partial class ChartService(IFileStorageService fileStorageService, ILogger<ChartService> logger) : IChartService
{
    public async Task<(string, string, ChartFormat, int)?> Upload(string fileName, IFormFile file,
        bool anonymizeChart = false, bool anonymizeSong = false)
    {
        var validationResult = await Validate(file);
        if (validationResult == null) return null;
        return await Upload(validationResult.Value, fileName, anonymizeChart, anonymizeSong);
    }

    public async Task<(string, string, ChartFormat, int)?> Upload(string fileName, string filePath)
    {
        var validationResult = await Validate(filePath);
        if (validationResult == null) return null;
        return await Upload(validationResult.Value, fileName);
    }

    public async Task<(ChartFormat, ChartFormatDto, int)?> Validate(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        var rpeJson = ReadRpe(content);
        if (rpeJson != null)
            return new ValueTuple<ChartFormat, ChartFormatDto, int>(ChartFormat.RpeJson, rpeJson, CountNotes(rpeJson));
        var pec = ReadPec(content);
        if (pec != null) return new ValueTuple<ChartFormat, ChartFormatDto, int>(ChartFormat.Pec, pec, CountNotes(pec));
        return null;
    }

    private async Task<(string, string, ChartFormat, int)> Upload((ChartFormat, ChartFormatDto, int) validationResult,
        string fileName, bool anonymizeChart = false, bool anonymizeSong = false)
    {
        var serialized = validationResult.Item1 == ChartFormat.RpeJson
            ? Serialize(Standardize((RpeJsonDto)validationResult.Item2, anonymizeChart, anonymizeSong))
            : Serialize(Standardize((PecDto)validationResult.Item2));
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(serialized));
        var uploadResult =
            await fileStorageService.Upload<Chart>(fileName, stream, GetExtension(validationResult.Item1));
        return new ValueTuple<string, string, ChartFormat, int>(uploadResult.Item1, uploadResult.Item2,
            validationResult.Item1, validationResult.Item3);
    }

    private async Task<(ChartFormat, ChartFormatDto, int)?> Validate(string filePath)
    {
        using var reader = new StreamReader(filePath);
        var content = await reader.ReadToEndAsync();
        var rpeJson = ReadRpe(content);
        if (rpeJson != null)
            return new ValueTuple<ChartFormat, ChartFormatDto, int>(ChartFormat.RpeJson, rpeJson, CountNotes(rpeJson));
        var pec = ReadPec(content);
        if (pec != null) return new ValueTuple<ChartFormat, ChartFormatDto, int>(ChartFormat.Pec, pec, CountNotes(pec));
        return null;
    }

    private static RpeJsonDto Standardize(RpeJsonDto dto, bool anonymizeChart = false, bool anonymizeSong = false)
    {
        if (anonymizeChart)
        {
            dto.Meta.Charter = "Anonymous";
        }

        if (anonymizeSong)
        {
            dto.Meta.Composer = "Anonymous";
        }

        dto.BpmList.Sort();
        foreach (var line in dto.JudgeLineList.OfType<JudgeLine>())
        {
            if (line.EventLayers != null)
                foreach (var layer in line.EventLayers.OfType<EventLayer>())
                {
                    layer.AlphaEvents?.Sort();
                    layer.MoveXEvents?.Sort();
                    layer.MoveYEvents?.Sort();
                    layer.RotateEvents?.Sort();
                    layer.SpeedEvents?.Sort();
                }

            if (line.Extended != null)
            {
                line.Extended.ColorEvents?.Sort();
                line.Extended.InclineEvents?.Sort();
                line.Extended.PaintEvents?.Sort();
                line.Extended.ScaleXEvents?.Sort();
                line.Extended.ScaleYEvents?.Sort();
                line.Extended.TextEvents?.Sort();
            }

            line.Notes?.Sort();
            line.NumOfNotes = line.Notes?.Count(note => note != null && note.IsFake != 1) ?? 0;
        }

        return dto;
    }

    private static PecDto Standardize(PecDto dto)
    {
        dto.BpmCommands.Sort();
        dto.NoteCommands.Sort();
        dto.SpeedCommands.Sort();
        dto.MoveCommands.Sort();
        dto.RotationCommands.Sort();
        dto.AlphaCommands.Sort();
        dto.DurationalMoveCommands.Sort();
        dto.DurationalRotationCommands.Sort();
        dto.DurationalAlphaCommands.Sort();
        return dto;
    }

    private static string Serialize(RpeJsonDto dto)
    {
        return JsonConvert.SerializeObject(dto);
    }

    private static string Serialize(PecDto dto)
    {
        var builder = new StringBuilder($"{dto.Offset}\r\n");
        foreach (var command in dto.BpmCommands) builder.Append($"{BpmCommand.Id} {command.Time} {command.Bpm}\r\n");

        builder.Append("\r\n");

        foreach (var command in dto.NoteCommands)
        {
            if (command.EndTime == null)
                builder.Append(
                    $"{command.Id} {command.JudgeLine} {command.Time} {command.PositionX} {command.Direction} {Convert.ToInt32(command.IsFake)}\r\n");
            else
                builder.Append(
                    $"{command.Id} {command.JudgeLine} {command.Time} {command.EndTime} {command.PositionX} {command.Direction} {Convert.ToInt32(command.IsFake)}\r\n");

            builder.Append($"# {command.Speed}\r\n& {command.Size}\r\n");
        }

        builder.Append("\r\n");

        foreach (var command in dto.SpeedCommands)
            builder.Append($"{SpeedCommand.Id} {command.JudgeLine} {command.Time} {command.Speed}\r\n");

        builder.Append("\r\n");

        foreach (var command in dto.MoveCommands)
            builder.Append($"{MoveCommand.Id} {command.JudgeLine} {command.Time} {command.X} {command.Y}\r\n");

        builder.Append("\r\n");

        foreach (var command in dto.RotationCommands)
            builder.Append($"{RotationCommand.Id} {command.JudgeLine} {command.Time} {command.Arc}\r\n");

        builder.Append("\r\n");

        foreach (var command in dto.AlphaCommands)
            builder.Append($"{AlphaCommand.Id} {command.JudgeLine} {command.Time} {command.Alpha}\r\n");

        builder.Append("\r\n");

        foreach (var command in dto.DurationalMoveCommands)
            builder.Append(
                $"{DurationalMoveCommand.Id} {command.JudgeLine} {command.Time} {command.EndTime} {command.X} {command.Y} {command.MotionType}\r\n");

        builder.Append("\r\n");

        foreach (var command in dto.DurationalRotationCommands)
            builder.Append(
                $"{DurationalRotationCommand.Id} {command.JudgeLine} {command.Time} {command.EndTime} {command.Arc} {command.MotionType}\r\n");

        builder.Append("\r\n");

        foreach (var command in dto.DurationalAlphaCommands)
            builder.Append(
                $"{DurationalAlphaCommand.Id} {command.JudgeLine} {command.Time} {command.EndTime} {command.Alpha}\r\n");

        return builder.ToString();
    }

    private static int CountNotes(RpeJsonDto dto)
    {
        var noteCount = 0;

        foreach (var line in dto.JudgeLineList)
        {
            if (line?.Notes == null) continue;
            noteCount += line.Notes.Count(note => note != null && note.IsFake != 1);
        }

        return noteCount;
    }

    private static int CountNotes(PecDto dto)
    {
        return dto.NoteCommands.Count(command => !command.IsFake);
    }

    private static RpeJsonDto? ReadRpe(string input)
    {
        try
        {
            var dto = JsonConvert.DeserializeObject<RpeJsonDto>(input);
            if (dto == null) return null;
            dto.BpmList = dto.BpmList.Where(e => e != null).ToList();
            if (dto.BpmList.Any(info => info!.StartTime[1] != 0 && info.StartTime[2] == 0)) return null;
            dto.JudgeLineGroup = dto.JudgeLineGroup.Where(e => e != null).ToList();
            dto.JudgeLineList = dto.JudgeLineList.Where(e => e != null).ToList();
            foreach (var line in dto.JudgeLineList)
            {
                if (line!.Notes != null)
                {
                    line.Notes = line.Notes.Where(e => e != null).ToList();
                    foreach (var note in line.Notes)
                    {
                        if (note!.StartTime[1] != 0 && note.StartTime[2] == 0) return null;
                        if (note.EndTime[1] != 0 && note.EndTime[2] == 0) return null;
                    }
                }

                if (line.AlphaControl != null)
                    line.AlphaControl = line.AlphaControl.Where(e => e != null).ToList();
                else
                    line.AlphaControl = new List<AlphaControl?>
                    {
                        new() { Alpha = 1.0, Easing = 1, X = 0.0 }, new() { Alpha = 1.0, Easing = 1, X = 9999999.0 }
                    };
                if (line.PosControl != null)
                    line.PosControl = line.PosControl.Where(e => e != null).ToList();
                else
                    line.PosControl = new List<PosControl?>
                    {
                        new() { Pos = 1.0, Easing = 1, X = 0.0 }, new() { Pos = 1.0, Easing = 1, X = 9999999.0 }
                    };
                if (line.SizeControl != null)
                    line.SizeControl = line.SizeControl.Where(e => e != null).ToList();
                else
                    line.SizeControl = new List<SizeControl?>
                    {
                        new() { Size = 1.0, Easing = 1, X = 0.0 }, new() { Size = 1.0, Easing = 1, X = 9999999.0 }
                    };
                if (line.SkewControl != null)
                    line.SkewControl = line.SkewControl.Where(e => e != null).ToList();
                else
                    line.SkewControl = new List<SkewControl?>
                    {
                        new() { Skew = 0.0, Easing = 1, X = 0.0 }, new() { Skew = 0.0, Easing = 1, X = 9999999.0 }
                    };
                if (line.YControl != null)
                    line.YControl = line.YControl.Where(e => e != null).ToList();
                else
                    line.YControl = new List<YControl?>
                    {
                        new() { Y = 1.0, Easing = 1, X = 0.0 }, new() { Y = 1.0, Easing = 1, X = 9999999.0 }
                    };

                if (line.EventLayers != null)
                {
                    line.EventLayers = line.EventLayers.Where(e => e != null).ToList();
                    foreach (var layer in line.EventLayers)
                    {
                        if (layer!.AlphaEvents != null)
                        {
                            layer.AlphaEvents = layer.AlphaEvents.Where(e => e != null).ToList();
                            foreach (var e in layer.AlphaEvents)
                            {
                                if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                                if (e.Bezier == null || e.BezierPoints == null)
                                {
                                    e.Bezier = 0;
                                    e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                                }
                            }
                        }

                        if (layer.MoveXEvents != null)
                        {
                            layer.MoveXEvents = layer.MoveXEvents.Where(e => e != null).ToList();
                            foreach (var e in layer.MoveXEvents)
                            {
                                if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                                if (e.Bezier == null || e.BezierPoints == null)
                                {
                                    e.Bezier = 0;
                                    e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                                }
                            }
                        }

                        if (layer.MoveYEvents != null)
                        {
                            layer.MoveYEvents = layer.MoveYEvents.Where(e => e != null).ToList();
                            foreach (var e in layer.MoveYEvents)
                            {
                                if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                                if (e.Bezier == null || e.BezierPoints == null)
                                {
                                    e.Bezier = 0;
                                    e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                                }
                            }
                        }

                        if (layer.RotateEvents != null)
                        {
                            layer.RotateEvents = layer.RotateEvents.Where(e => e != null).ToList();
                            foreach (var e in layer.RotateEvents)
                            {
                                if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                                if (e.Bezier == null || e.BezierPoints == null)
                                {
                                    e.Bezier = 0;
                                    e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                                }
                            }
                        }

                        if (layer.SpeedEvents != null)
                        {
                            layer.SpeedEvents = layer.SpeedEvents.Where(e => e != null).ToList();
                            foreach (var e in layer.SpeedEvents)
                            {
                                if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            }
                        }
                    }
                }

                if (line.Extended != null)
                {
                    if (line.Extended.ColorEvents != null)
                    {
                        line.Extended.ColorEvents = line.Extended.ColorEvents.Where(e => e != null).ToList();
                        foreach (var e in line.Extended.ColorEvents)
                        {
                            if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            if (e.Bezier == null || e.BezierPoints == null)
                            {
                                e.Bezier = 0;
                                e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                            }
                        }
                    }

                    if (line.Extended.InclineEvents != null)
                    {
                        line.Extended.InclineEvents = line.Extended.InclineEvents.Where(e => e != null).ToList();
                        foreach (var e in line.Extended.InclineEvents)
                        {
                            if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            if (e.Bezier == null || e.BezierPoints == null)
                            {
                                e.Bezier = 0;
                                e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                            }
                        }
                    }

                    if (line.Extended.PaintEvents != null)
                    {
                        line.Extended.PaintEvents = line.Extended.PaintEvents.Where(e => e != null).ToList();
                        foreach (var e in line.Extended.PaintEvents)
                        {
                            if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            if (e.Bezier == null || e.BezierPoints == null)
                            {
                                e.Bezier = 0;
                                e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                            }
                        }
                    }

                    if (line.Extended.ScaleXEvents != null)
                    {
                        line.Extended.ScaleXEvents = line.Extended.ScaleXEvents.Where(e => e != null).ToList();
                        foreach (var e in line.Extended.ScaleXEvents)
                        {
                            if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            if (e.Bezier == null || e.BezierPoints == null)
                            {
                                e.Bezier = 0;
                                e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                            }
                        }
                    }

                    if (line.Extended.ScaleYEvents != null)
                    {
                        line.Extended.ScaleYEvents = line.Extended.ScaleYEvents.Where(e => e != null).ToList();
                        foreach (var e in line.Extended.ScaleYEvents)
                        {
                            if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            if (e.Bezier == null || e.BezierPoints == null)
                            {
                                e.Bezier = 0;
                                e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                            }
                        }
                    }

                    if (line.Extended.TextEvents != null)
                    {
                        line.Extended.TextEvents = line.Extended.TextEvents.Where(e => e != null).ToList();
                        foreach (var e in line.Extended.TextEvents)
                        {
                            if (e!.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            if (e.Bezier == null || e.BezierPoints == null)
                            {
                                e.Bezier = 0;
                                e.BezierPoints = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                            }
                        }
                    }
                }
            }

            return dto;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private PecDto? ReadPec(string input)
    {
        var dto = new PecDto
        {
            BpmCommands = [],
            NoteCommands = [],
            SpeedCommands = [],
            MoveCommands = [],
            RotationCommands = [],
            AlphaCommands = [],
            DurationalMoveCommands = [],
            DurationalRotationCommands = [],
            DurationalAlphaCommands = []
        };

        try
        {
            foreach (var line in input.Split(GetLineSeparator(input)))
            {
                if (line == string.Empty || !PecCommandRegex().IsMatch(line)) continue;

                if (int.TryParse(line, out var number))
                {
                    dto.Offset = number;
                    continue;
                }

                var args = line.Split(' ');

                switch (line[0])
                {
                    case 'b':
                        if (line[1] != 'p') continue;
                        dto.BpmCommands.Add(
                            new BpmCommand { Time = double.Parse(args[1]), Bpm = double.Parse(args[2]) });
                        continue;
                    case 'n':
                    {
                        var durational = line[1] == '2';
                        var adjuster = durational ? 1 : 0;
                        dto.NoteCommands.Add(new NoteCommand
                        {
                            Id = args[0],
                            JudgeLine = int.Parse(args[1]),
                            Time = double.Parse(args[2]),
                            EndTime = durational ? double.Parse(args[3]) : null,
                            PositionX = double.Parse(args[3 + adjuster]),
                            Direction = int.Parse(args[4 + adjuster]),
                            IsFake = args[5 + adjuster][0] == '1'
                        });
                        continue;
                    }
                    case '#':
                    {
                        dto.NoteCommands[^1].Speed = double.Parse(args[1]);
                        continue;
                    }
                    case '&':
                    {
                        dto.NoteCommands[^1].Size = double.Parse(args[1]);
                        continue;
                    }
                    case 'c':
                    {
                        switch (line[1])
                        {
                            case 'v':
                            {
                                dto.SpeedCommands.Add(new SpeedCommand
                                {
                                    JudgeLine = int.Parse(args[1]),
                                    Time = double.Parse(args[2]),
                                    Speed = double.Parse(args[3])
                                });
                                continue;
                            }
                            case 'p':
                            {
                                dto.MoveCommands.Add(new MoveCommand
                                {
                                    JudgeLine = int.Parse(args[1]),
                                    Time = double.Parse(args[2]),
                                    X = double.Parse(args[3]),
                                    Y = double.Parse(args[4])
                                });
                                continue;
                            }
                            case 'd':
                            {
                                dto.RotationCommands.Add(new RotationCommand
                                {
                                    JudgeLine = int.Parse(args[1]),
                                    Time = double.Parse(args[2]),
                                    Arc = double.Parse(args[3])
                                });
                                continue;
                            }
                            case 'a':
                            {
                                dto.AlphaCommands.Add(new AlphaCommand
                                {
                                    JudgeLine = int.Parse(args[1]),
                                    Time = double.Parse(args[2]),
                                    Alpha = double.Parse(args[3])
                                });
                                continue;
                            }
                            case 'm':
                            {
                                dto.DurationalMoveCommands.Add(new DurationalMoveCommand
                                {
                                    JudgeLine = int.Parse(args[1]),
                                    Time = double.Parse(args[2]),
                                    EndTime = double.Parse(args[3]),
                                    X = double.Parse(args[4]),
                                    Y = double.Parse(args[5]),
                                    MotionType = int.Parse(args[6])
                                });
                                continue;
                            }
                            case 'r':
                            {
                                dto.DurationalRotationCommands.Add(new DurationalRotationCommand
                                {
                                    JudgeLine = int.Parse(args[1]),
                                    Time = double.Parse(args[2]),
                                    EndTime = double.Parse(args[3]),
                                    Arc = double.Parse(args[4]),
                                    MotionType = int.Parse(args[5])
                                });
                                continue;
                            }
                            case 'f':
                            {
                                dto.DurationalAlphaCommands.Add(new DurationalAlphaCommand
                                {
                                    JudgeLine = int.Parse(args[1]),
                                    Time = double.Parse(args[2]),
                                    EndTime = double.Parse(args[3]),
                                    Alpha = double.Parse(args[4])
                                });
                                continue;
                            }
                        }

                        continue;
                    }
                }
            }

            return dto.BpmCommands.Count > 0 ? dto : null;
        }
        catch (Exception ex)
        {
            logger.LogError(LogEvents.ChartFailure, ex, "[{Now}] Failed to parse chart",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return null;
        }
    }

    private static string GetExtension(ChartFormat format)
    {
        return format switch
        {
            ChartFormat.RpeJson => "json",
            ChartFormat.Pec => "pec",
            ChartFormat.Phigrim => "json",
            ChartFormat.PhiZone => "json",
            ChartFormat.Unsupported => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private static string GetLineSeparator(string input)
    {
        return input.Contains("\r\n") ? "\r\n" : input.Contains('\r') ? "\r" : "\n";
    }

    [GeneratedRegex("^(-?[0-9]+)|(n[1-4]|bp|cp|cm|cd|cr|ca|cf|cv|#|&)[ -.0-9]+$")]
    private static partial Regex PecCommandRegex();
}