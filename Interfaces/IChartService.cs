using PhiZoneApi.Dtos.ChartFormats;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Interfaces;

public interface IChartService
{
    Task<(string, string, ChartFormat, int)?> Upload(string fileName, IFormFile file, bool anonymizeChart = false, bool anonymizeSong = false);

    Task<(string, string, ChartFormat, int)?> Upload(string fileName, string filePath);
    
    Task<(ChartFormat, ChartFormatDto, int)?> Validate(IFormFile file);

    Task<(string, string, ChartFormat, int)> Upload((ChartFormat, ChartFormatDto, int) validationResult,
        string fileName, bool anonymizeChart = false, bool anonymizeSong = false);

    Task<(ChartFormat, ChartFormatDto, int)?> Validate(string filePath);

    RpeJsonDto Standardize(RpeJsonDto dto, bool anonymizeChart = false, bool anonymizeSong = false);

    PecDto Standardize(PecDto dto);

    string Serialize(RpeJsonDto dto);

    string Serialize(PecDto dto);

    int CountNotes(RpeJsonDto dto);

    int CountNotes(PecDto dto);

    RpeJsonDto? ReadRpe(string input);

    PecDto? ReadPec(string input);
}