using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IFeishuService
{
    Task Notify(SongSubmission submission, params int[] chats);

    Task Notify(ChartSubmission submission, params int[] chats);

    Task Notify(PetAnswer answer, DateTimeOffset dateStarted, params int[] chats);
}