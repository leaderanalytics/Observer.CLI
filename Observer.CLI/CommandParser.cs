namespace LeaderAnalytics.Observer.CLI;

public class CommandParser
{
    
    private const string pattern0 = $"(?i)(^{DataProvider.Fred}$|^{CommandArgument.Config}$|^{CommandArgument.Help}$)";
    private IAdaptiveClient<IObserverAPI_Manifest> client;
    private  HashSet<string> fredDataTypes;

    public CommandParser(IAdaptiveClient<IObserverAPI_Manifest> client)
    {
        this.client = client;
        fredDataTypes = typeof(FredDataType).GetProperties().Select(x => x.Name).ToHashSet();
    }

    public async Task Parse(string[] args)
    {
        if(args is null || args.Length == 0)
            args = new string[1];

        if (string.IsNullOrEmpty(args[0]))
            args[0] = "--help";

        await ParseCommand(args);
    }

    private async Task ParseCommand(string[] args)
    {
        if (!Regex.IsMatch(args[0], pattern0))
            throw new ObserverException(Resources.CommandError.Replace("{0}", args[0]));
        
        string cmd = args[0].ToLower();

        if (cmd == DataProvider.Fred)
            await ParseFredCommand(args);
    }


    private async Task ParseFredCommand(string[] args)
    {
        switch (args[1]?.ToLower())
        {
            case FredDataType.Category:
                await GetCategories(args);
                break;
            case FredDataType.Releases:
                GetReleases(args);
                break;
            case FredDataType.Release:
                GetRelease(args);
                break;
            case FredDataType.Series:
                GetSeries(args);
                break;
            case FredDataType.Sources:
                GetSources(args);
                break;
            case FredDataType.Tags:
                GetTags(args);
                break;
            case FredDataType.RelatedTags:
                GetRelatedTags(args);
                break;
            default:
                ShowFredHelp();
                break;
        }
    }

    private async Task GetCategories(string[] args)
    {
        // Get the id argument - if arg[4] contains the name of a fred argument, use arg[5] other use arg[4] as the id.
        string? id = new string[] { FredDataArg.Children, FredDataArg.Related, FredDataArg.Series, FredDataArg.Tags, FredDataArg.RelatedTags }.Contains(args.Try(2)) ? args.Try(3) : args.Try(2);

        if (string.IsNullOrEmpty(id))
            throw new ObserverException("Category ID is required.");


        switch (args[2].ToLower())
        {
            case FredDataArg.Children:
                await client.CallAsync(x => x.CategoriesService.DownloadCategory(id));
                break;
                
        }
    }

    public void GetReleases(string[] args) {  }
    public void GetRelease(string[] args) {  }
    public void GetSeries(string[] args) {  }
    public void GetSources(string[] args) {  }
    public void GetSource(string[] args) {  }
    public void GetTags(string[] args) {  }
    public void GetRelatedTags(string[] args) {  }

    public void ShowFredHelp()
    {
    }
}
