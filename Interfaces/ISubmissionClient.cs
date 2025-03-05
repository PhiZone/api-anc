using PhiZoneApi.Enums;

namespace PhiZoneApi.Interfaces;

public interface ISubmissionClient
{
    Task ReceiveFileProgress(SessionFileStatus status, string? name, string? detail, double progress, long bytesProcessed);
}