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

public partial class ChartService : IChartService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ChartService> _logger;
    private readonly ISongRepository _songRepository;
    private readonly ISongSubmissionRepository _songSubmissionRepository;

    public ChartService(IFileStorageService fileStorageService, ISongRepository songRepository,
        ISongSubmissionRepository songSubmissionRepository, ILogger<ChartService> logger)
    {
        _fileStorageService = fileStorageService;
        _songRepository = songRepository;
        _songSubmissionRepository = songSubmissionRepository;
        _logger = logger;
    }

    public async Task<(string, string, ChartFormat, int)?> Upload(string fileName, IFormFile file)
    {
        var validationResult = await Validate(file);
        if (validationResult == null) return null;
        return await Upload(validationResult.Value, fileName);
    }

    public async Task<(string, string, ChartFormat, int)?> Upload(string fileName, string filePath)
    {
        var validationResult = await Validate(filePath);
        if (validationResult == null) return null;
        return await Upload(validationResult.Value, fileName);
    }

    private async Task<(string, string, ChartFormat, int)> Upload((ChartFormat, ChartFormatDto, int) validationResult, string fileName)
    {
        var serialized = validationResult.Item1 == ChartFormat.RpeJson
            ? Serialize(Standardize((RpeJsonDto)validationResult.Item2))
            : Serialize(Standardize((PecDto)validationResult.Item2));
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(serialized));
        var uploadResult =
            await _fileStorageService.Upload<Chart>(fileName, stream, GetExtension(validationResult.Item1));
        return new ValueTuple<string, string, ChartFormat, int>(uploadResult.Item1, uploadResult.Item2,
            validationResult.Item1, validationResult.Item3);
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

    public async Task<(ChartFormat, ChartFormatDto, int)?> Validate(string filePath)
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

    public async Task<string> GetDisplayName(Chart chart)
    {
        var title = chart.Title ?? (await _songRepository.GetSongAsync(chart.SongId)).Title;
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}]";
    }

    public async Task<string> GetDisplayName(ChartSubmission chart)
    {
        var title = chart.Title ?? (chart.SongId != null
            ? (await _songRepository.GetSongAsync(chart.SongId.Value)).Title
            : (await _songSubmissionRepository.GetSongSubmissionAsync(chart.SongSubmissionId!.Value)).Title);
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}]";
    }

    private static RpeJsonDto Standardize(RpeJsonDto dto)
    {
        dto.BpmList.Sort();
        foreach (var line in dto.JudgeLineList)
        {
            if (line.EventLayers != null)
                foreach (var layer in line.EventLayers)
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
            line.NumOfNotes = line.Notes?.Count(note => note.IsFake != 1) ?? 0;
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
            if (line.Notes == null) continue;
            noteCount += line.Notes.Count(note => note.IsFake != 1);
        }

        return noteCount;
    }

    private static int CountNotes(PecDto dto)
    {
        return dto.NoteCommands.Count(command => !command.IsFake);
    }

    private RpeJsonDto? ReadRpe(string input)
    {
        try
        {
            var dto = JsonConvert.DeserializeObject<RpeJsonDto>(input);
            if (dto == null) return null;

            foreach (var info in dto.BpmList)
                if (info.StartTime[1] != 0 && info.StartTime[2] == 0)
                    return null;

            foreach (var line in dto.JudgeLineList)
            {
                if (line.Notes != null)
                    foreach (var note in line.Notes)
                    {
                        if (note.StartTime[1] != 0 && note.StartTime[2] == 0) return null;
                        if (note.EndTime[1] != 0 && note.EndTime[2] == 0) return null;
                    }

                if (line.EventLayers != null)
                    foreach (var layer in line.EventLayers)
                    {
                        if (layer.AlphaEvents != null)
                            foreach (var e in layer.AlphaEvents)
                            {
                                if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            }

                        if (layer.MoveXEvents != null)
                            foreach (var e in layer.MoveXEvents)
                            {
                                if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            }

                        if (layer.MoveYEvents != null)
                            foreach (var e in layer.MoveYEvents)
                            {
                                if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            }

                        if (layer.RotateEvents != null)
                            foreach (var e in layer.RotateEvents)
                            {
                                if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            }

                        if (layer.SpeedEvents != null)
                            foreach (var e in layer.SpeedEvents)
                            {
                                if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                                if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                            }
                    }

                if (line.Extended != null)
                {
                    if (line.Extended.ColorEvents != null)
                        foreach (var e in line.Extended.ColorEvents)
                        {
                            if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                        }

                    if (line.Extended.InclineEvents != null)
                        foreach (var e in line.Extended.InclineEvents)
                        {
                            if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                        }

                    if (line.Extended.PaintEvents != null)
                        foreach (var e in line.Extended.PaintEvents)
                        {
                            if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                        }

                    if (line.Extended.ScaleXEvents != null)
                        foreach (var e in line.Extended.ScaleXEvents)
                        {
                            if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                        }

                    if (line.Extended.ScaleYEvents != null)
                        foreach (var e in line.Extended.ScaleYEvents)
                        {
                            if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                        }

                    if (line.Extended.TextEvents != null)
                        foreach (var e in line.Extended.TextEvents)
                        {
                            if (e.StartTime[1] != 0 && e.StartTime[2] == 0) return null;
                            if (e.EndTime[1] != 0 && e.EndTime[2] == 0) return null;
                        }
                }
            }

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.RpeJsonFailure, ex, "Failed to parse RPE JSON");
            return null;
        }
    }

    private PecDto? ReadPec(string input)
    {
        var dto = new PecDto
        {
            BpmCommands = new List<BpmCommand>(),
            NoteCommands = new List<NoteCommand>(),
            SpeedCommands = new List<SpeedCommand>(),
            MoveCommands = new List<MoveCommand>(),
            RotationCommands = new List<RotationCommand>(),
            AlphaCommands = new List<AlphaCommand>(),
            DurationalMoveCommands = new List<DurationalMoveCommand>(),
            DurationalRotationCommands = new List<DurationalRotationCommand>(),
            DurationalAlphaCommands = new List<DurationalAlphaCommand>()
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
            _logger.LogError(LogEvents.PecFailure, ex, "Failed to parse PEC");
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