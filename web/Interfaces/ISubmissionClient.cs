using PhiZoneApi.Enums;

namespace PhiZoneApi.Interfaces;

public interface ISubmissionClient
{
    Task ReceiveFileProgress(SessionFileStatus status, string? detail, double? progress);
}