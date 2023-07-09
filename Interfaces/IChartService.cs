using PhiZoneApi.Dtos.ChartFormats;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Interfaces;

public interface IChartService
{
    Task<(ChartFormat, ChartFormatDto, int)?> Validate(IFormFile file);
    
    Task<(string, ChartFormat, int)?> Upload(string fileName, IFormFile file);
}