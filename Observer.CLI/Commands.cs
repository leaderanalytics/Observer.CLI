namespace LeaderAnalytics.Observer.CLI;

public static class CommandArgument
{
    public const string Help = "--help";
    public const string Export = "--export";
    public const string Config = "--config";
    public const string Show = "--show";
    public const string UpdateDB = "--updatedb";
    public const int MaxCommandArgs = 4; 
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
    public const string Sources = "sources";
    public const string Source = "source";
    public const string Tags = "tags";
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
