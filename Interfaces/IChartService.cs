using PhiZoneApi.Dtos.ChartFormats;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Interfaces;

public interface IChartService
{
    Task<(ChartFormat, ChartFormatDto, int)?> Validate(IFormFile file);

    Task<(string, string, ChartFormat, int)?> Upload(string fileName, IFormFile file, bool anonymizeChart = false, bool anonymizeSong = false);

    Task<(string, string, ChartFormat, int)?> Upload(string fileName, string filePath);
}