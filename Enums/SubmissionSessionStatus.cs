namespace PhiZoneApi.Enums;

public enum SubmissionSessionStatus
{
    Waiting, // Waiting for the user to start the session
    SongFinished, // The song has finished uploading
    ChartFinished // The chart has finished uploading
}