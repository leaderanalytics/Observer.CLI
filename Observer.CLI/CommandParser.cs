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
        for(int i = 0; i < args.Length; i++)
            args[i] = (args[i]?.Trim() ?? String.Empty).ToLower();

        if (!Regex.IsMatch(args[0], pattern0))
            throw new Exception(Resources.CommandError.Replace("{0}", args[0]));
        
        string cmd = args[0];

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
            case FredDataType.Source:
                await GetSources(args);
                break;
            case FredDataType.Tag:
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
                    await client.CallAsync(x => x.CategoriesService.DownloadCategorySeries(id));
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
        string? id = idRequired ? args.Try(3) : args.Try(2); 

        if (args.Length > (idRequired ? 4 : 3))
            throw new Exception("Too many arguments.");
        else if(string.IsNullOrEmpty(id) && new string[] { FredDataArg.Series, FredDataArg.Sources}.Contains(args.Try(2)))
            throw new Exception("One or more symbols are required.");

        if (! idRequired)
        {
            if (string.IsNullOrEmpty(id))
                await client.CallAsync(x => x.ReleasesService.DownloadAllReleases());
            else
                await client.CallAsync(x => x.ReleasesService.DownloadRelease(id));
        }
        else
        { 
            switch (args[2])
            {
                case FredDataArg.Dates:
                
                    if(string.IsNullOrEmpty(id))
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
       
    public async Task GetSeries(string[] args) 
    {
        bool isID = ! new string[] { FredDataArg.Categories, FredDataArg.Release, FredDataArg.Tags, FredDataArg.Observations }.Contains(args.Try(2));
        string? idsArg = isID ? args.Try(2) : args.Try(3); // id can be not null 
        
        if (args.Length > (isID ? 3 : 4))
            throw new Exception("Too many arguments.  If specifying multiple symbols seperate each symbol with a comma and do not include spaces: GNPCA,CPIAUSCL.");
        else if (string.IsNullOrEmpty(idsArg))
            throw new Exception("Symbol is required.");

        List<string> ids = idsArg.Split(",").ToList();

        foreach (string id in ids)
        {
            if (isID)
                await client.CallAsync(x => x.SeriesService.DownloadSeries(id));
            else
            {
                switch (args[2])
                {
                    case FredDataArg.Observations:
                        await client.CallAsync(x => x.ObservationsService.DownloadObservations(id));
                        break;
                    case FredDataArg.Categories:
                        await client.CallAsync(x => x.SeriesService.DownloadCategoriesForSeries(id));
                        break;
                    case FredDataArg.Release:
                        await client.CallAsync(x => x.SeriesService.DownloadSeriesRelease(id));
                        break;
                    case FredDataArg.Tags:
                        await client.CallAsync(x => x.SeriesService.DownloadSeriesTags(id));
                        break;
                    default:
                        throw new Exception($"Argument {args[2]} is not recognized.");
                }
            }
        }
    }

    public async Task GetSources(string[] args) 
    {
        bool isID = new string[] { FredDataArg.Releases }.Contains(args.Try(2));
        string? idsArg = isID ? args.Try(3) : args.Try(2);

        if (args.Length > (isID ? 4 : 3))
            throw new Exception("Too many arguments.  If specifying multiple symbols seperate each symbol with a comma and do not include spaces: GNPCA,CPIAUSCL.");
        else if(isID && string.IsNullOrEmpty(idsArg))
            throw new Exception("Symbol is required.");

        if (string.IsNullOrEmpty(idsArg))
        {
            await client.CallAsync(x => x.ReleasesService.DownloadAllSources());
        }
        else
        {
            IEnumerable<string> ids = idsArg.Split(",");

            foreach (string id in ids)
            {
                if (!isID)
                {
                    await client.CallAsync(x => x.ReleasesService.DownloadSource(id));
                }
                else
                {
                    switch (args[2].ToLower())
                    {
                        case FredDataArg.Releases:
                            await client.CallAsync(x => x.ReleasesService.DownloadSourceReleases(id));
                            break;
                        default:
                            throw new Exception($"Argument {args[2]} is not recognized.");
                    }
                }
            }
        }
    }

    
    public async Task GetTags(string[] args) 
    {
        DateTime? realTimeStart = null;
        DateTime? realTimeEnd = null;
        string? tagNames = null;
        string groupID = null;
        string searchText = null;

        for (int i = 2; i < args.Length; i++)
        {
            string[] keyvalue = args[i].Split(':');
            
            if (keyvalue.Length != 2)
                throw new Exception($"Argument {args[i]} is invalid.  A colon seperated argument name and value is expected: --arg_name:arg_value.");

            switch (keyvalue[0])
            {
                case CommandArgument.RealTimeStart:
                    break;
                case CommandArgument.RealTimeEnd:
                    break;
                case CommandArgument.TagNames:
                    break;
                case CommandArgument.TagGroupID:
                    break;
                case CommandArgument.SearchText:
                    break;
                default:
                    throw new Exception($"Argument {keyvalue[0]} is not recognized.");
            }
        }
    }
    

    public void ShowFredHelp()
    {
    }
}
