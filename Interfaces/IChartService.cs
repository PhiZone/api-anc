using PhiZoneApi.Dtos.ChartFormats;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IChartService
{
    Task<(ChartFormat, ChartFormatDto, int)?> Validate(IFormFile file);

    Task<(string, string, ChartFormat, int)?> Upload(string fileName, IFormFile file);

    Task<string> GetDisplayName(Chart chart);

    Task<string> GetDisplayName(ChartSubmission chart);
}