namespace PhiZoneApi.Constants;

public static class LogEvents
{
    public const int UserInfo = 1000;
    public const int SongInfo = 1001;
    public const int ChartInfo = 1002;
    public const int RecordInfo = 1003;
    public const int FeishuInfo = 1004;
    public const int SchedulerInfo = 1005;
    public const int MessengerInfo = 1006;
    public const int TapGhostInfo = 1007;
    public const int DatabaseSeederInfo = 1008;
    public const int InitializerInfo = 1009;
    public const int PhigrimInfo = 1010;

    public const int AudioFailure = 2000;
    public const int MailFailure = 2001;
    public const int ChartFailure = 2002;
    public const int FeishuFailure = 2003;
    public const int MessengerFailure = 2004;
    public const int TapGhostFailure = 2005;
    public const int PhigrimFailure = 2006;

    public const int RecordDebug = 3000;
    public const int MessengerDebug = 3001;

    public const int DataMigration = 4000;
    public const int ChartMigration = 4001;
    public const int FileMigration = 4002;

    public const int DataConsistencyMaintenance = 5000;

    public const int ScriptInfo = 6000;
    public const int ScriptFailure = 6001;
}