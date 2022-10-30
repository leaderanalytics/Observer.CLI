namespace LeaderAnalytics.Observer.CLI;

public static class CommandArgument
{
    public const string Help = "--help";
    public const string Export = "--export";
    public const string Config = "--config";
    public const string Show = "--show";
    public const string RealTimeStart = "--realtime_start";
    public const string RealTimeEnd = "--realtime_end";
    public const string TagNames = "--tag_names";
    public const string TagGroupID = "--tag_group_id";
    public const string SearchText = "--search_text";
}

public static class DataProvider
{
    public const string Fred = "fred";
}

public static class FredDataType 
{
    public const string Category = "category";
    public const string Release = "release";
    public const string Series = "series";
    public const string Source = "source";
    public const string Tag = "tag";
}

public static class FredDataArg 
{
    public const string Children = "children";
    public const string Related = "related";
    public const string Series = "series";
    public const string Tags = "tags";
    public const string Dates = "dates";
    public const string Sources = "sources";
    public const string Tables = "tables";
    public const string Categories = "categories";
    public const string Observations = "observations";
    public const string Release = "release";
    public const string VintageDates = "vintagedates";
    public const string Releases = "releases";
}

public static class ConfigArgument
{
    public const string UpdateDB = "updatedb";
    public const string VerifyDB = "verifydb";
}