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
            throw new Exception(Resources.CommandError.Replace("{0}", args[0]));
        
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
            case FredDataType.Release:
                await GetReleases(args);
                break;
            case FredDataType.Series:
                await GetSeries(args);
                break;
            case FredDataType.Sources:
                await GetSources(args);
                break;
            case FredDataType.Tags:
                await GetTags(args);
                break;
            default:
                ShowFredHelp();
                break;
        }
    }

    private async Task GetCategories(string[] args)
    {
        bool getCategory = ! new string[] { FredDataArg.Children, FredDataArg.Related, FredDataArg.Series, FredDataArg.Tags }.Contains(args.Try(2));
        
        string? id = getCategory ?  args.Try(2) : args.Try(3) ;

        if (string.IsNullOrEmpty(id))
            throw new Exception("Category ID is required.");
        else if(args.Length > (getCategory ? 3 : 4))
            throw new Exception("Too many arguments.");

        if (getCategory)
            await client.CallAsync(x => x.CategoriesService.DownloadCategory(id));
        else
        {
            switch (args[2].ToLower())
            {
                case FredDataArg.Children:
                    await client.CallAsync(x => x.CategoriesService.DownloadCategoryChildren(id));
                    break;
                case FredDataArg.Related:
                    await client.CallAsync(x => x.CategoriesService.DownloadRelatedCategories(id));
                    break;
                case FredDataArg.Series:
                    await client.CallAsync(x => x.SeriesService.DownloadSeriesCategoriesForCategory(id));
                    break;
                case FredDataArg.Tags:
                    await client.CallAsync(x => x.CategoriesService.DownloadCategoryTags(id));
                    break;
                default:
                    throw new Exception($"Argument {args[2]} is not recognized.");
            }
        }
    }

    private async Task GetReleases(string[] args) 
    {
        bool idRequired = new string[] { FredDataArg.Dates, FredDataArg.Series, FredDataArg.Sources }.Contains(args.Try(2));
        string? id = idRequired ? args.Try(3) : args.Try(2); // id can be null in which case we get all releases

        if (args.Length > (idRequired ? 4 : 3))
            throw new Exception("Too many arguments.");

        if (! idRequired)
        {
            if (id is null)
                await client.CallAsync(x => x.ReleasesService.DownloadAllReleases());
            else
                await client.CallAsync(x => x.ReleasesService.DownloadRelease(id));
        }
        else
        { 
            switch (args[2].ToLower())
            {
                case FredDataArg.Dates:
                
                    if(id is null)
                        await client.CallAsync(x => x.ReleasesService.DownloadAllReleaseDates());
                    else
                        await client.CallAsync(x => x.ReleasesService.DownloadReleaseDates(id));
                    
                    break;
                case FredDataArg.Series:
                    await client.CallAsync(x => x.ReleasesService.DownloadReleaseSeries(id));
                    break;
                case FredDataArg.Sources:
                    await client.CallAsync(x => x.ReleasesService.DownloadReleaseSources(id));
                    break;
                default:
                    throw new Exception($"Argument {args[2]} is not recognized.");
            }
        }
    }


   
    public async Task GetSeries(string[] args) {  }
    public async Task GetSources(string[] args) {  }
    public async Task GetSource(string[] args) {  }
    public async Task GetTags(string[] args) {  }
    

    public void ShowFredHelp()
    {
    }
}
