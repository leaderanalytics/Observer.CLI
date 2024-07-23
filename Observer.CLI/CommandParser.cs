using LeaderAnalytics.AdaptiveClient.EntityFrameworkCore;
using Serilog.Sinks.SystemConsole.Themes;
using System.Net.NetworkInformation;
using Velopack;

namespace LeaderAnalytics.Observer.CLI;

public class CommandParser
{
    private const string pattern0 = $"(?i)(^{DataProvider.Fred}$|^{CommandArgument.Config}$|^{CommandArgument.Help}$)";
    private IAdaptiveClient<IAPI_Manifest> client;
    private readonly DbHelper dbHelper;
    private  HashSet<string> fredDataTypes;

    public CommandParser(IAdaptiveClient<IAPI_Manifest> client, DbHelper dbHelper)
    {
        this.client = client;
        this.dbHelper = dbHelper;
        fredDataTypes = typeof(FredDataType).GetProperties().Select(x => x.Name).ToHashSet();
    }

    public async Task Parse(string[] args)
    {
        if(args is null || args.Length == 0)
            args = new string[1];

        if (string.IsNullOrEmpty(args[0]))
            args[0] = "--help";

        for (int i = 0; i < args.Length; i++)
            args[i] = (args[i]?.Trim() ?? String.Empty).ToLower();

        await ParseCommand(args);
    }

    private async Task ParseCommand(string[] args)
    {
        string cmd = args[0];

        if (cmd == DataProvider.Fred)
        {
            if ((await dbHelper.VerifyDatabase()) != DatabaseStatus.ConsistentWithModel)
                throw new Exception("The database does not exist or needs to be updated.  Run obs --config updatedb.");

            await ParseFredCommand(args);
        }
        else if (cmd == CommandArgument.Help)
            ShowHelp();
        else if (cmd == CommandArgument.Config)
            await ParseConfig(args);
        else if (cmd == CommandArgument.Update)
            await CheckForUpdate(args);
        else
        {
            string msg = $"{args[0]} is not a recognized command.  Try obs --help.";
            throw new ParseException(msg);
        }
    }


    private async Task ParseFredCommand(string[] args)
    {
        switch (args[1])
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
            case CommandArgument.Path:
                await ParsePathCommand(args);
                break;
            default:
                Console.WriteLine($"Argument \"{args[1]}\" is not recognized.");
                ShowHelp();
                break;
        }
    }

    private async Task ParsePathCommand(string[] args)
    {
        switch (args[2])
        {
            case PathTypes.SeriesPath:
                await ParseSeriesPathCommand(args);
                break;
            case PathTypes.CategoryPath:
                await ParseCategoryPathCommand(args);
                break;
            default:
                ShowHelp();
                break;
        }
    }

    private async Task ParseCategoryPathCommand(string[] args)
    {
        int catID;
        string idsArg = args.Try(3);
                
        if (string.IsNullOrEmpty(idsArg))
            throw new ParseException("A Category ID is required.");
        else
            int.TryParse(idsArg, out catID);

        if (catID == 0) 
            throw new ParseException("Category ID must be a numeric value.");

        FredDownloadArgs fda = new FredDownloadArgs { CategoryID = catID.ToString() };

        for(int i=4; i<args.Length; i++) 
        {
            string? arg = args.Try(i);

            switch (arg) 
            {
                case PathArgs.Series:
                    fda.Series = true;
                    break;
                case PathArgs.Disc:
                    fda.IncludeDiscontinuedSeries = true;
                    break;
                case PathArgs.ChildCat:
                    fda.ChildCategories = true;
                    break;
                case PathArgs.SeriesTag:
                    fda.SeriesTags = true;
                    break;
                case PathArgs.CatTag:
                    fda.CategoryTags = true;
                    break;
                case PathArgs.RelCat:
                    fda.RelatedCategories = true;
                    break;
                case PathArgs.Rel:
                    fda.Releases = true;
                    break;
                case PathArgs.RelDate:
                    fda.ReleaseDates = true;
                    break;
                case PathArgs.Source:
                    fda.Sources = true;
                    break;
                case PathArgs.Obs:
                    fda.Observations = true;
                    break;
                case PathArgs.Recurse:
                    fda.Recurse = true;
                    break;
                default:
                    throw new ParseException($"Argument {arg} is unrecognized or invalid for category path.");
            }
        }
        await client.CallAsync(x => x.DownloadService.Download(fda, null));
        Log.Information("Category path download completed successfully.");

    }
     
    private async Task ParseSeriesPathCommand(string[] args)
    {
        string[]? symbols = null; 
        string? idsArg = args.Try(3);

        if (string.IsNullOrEmpty(idsArg))
            throw new ParseException("One or more series IDs is required.");
        else
            symbols = idsArg.Split(',', StringSplitOptions.TrimEntries);

        if(!(symbols?.Any() ?? false))
            throw new ParseException("One or more series IDs is required.");

        FredDownloadArgs fda = new FredDownloadArgs { Symbols = symbols};

        for (int i = 4; i < args.Length; i++)
        {
            string? arg = args.Try(i);

            switch (arg)
            {
                case PathArgs.Disc:
                    fda.IncludeDiscontinuedSeries = true;
                    break;
                case PathArgs.SeriesCat:
                    fda.SeriesCategories = true;
                    break;
                case PathArgs.ChildCat:
                    fda.ChildCategories = true;
                    break;
                case PathArgs.SeriesTag:
                    fda.SeriesTags = true;
                    break;
                case PathArgs.CatTag:
                    fda.CategoryTags = true;
                    break;
                case PathArgs.RelCat:
                    fda.RelatedCategories = true;
                    break;
                case PathArgs.Rel:
                    fda.Releases = true;
                    break;
                case PathArgs.RelDate:
                    fda.ReleaseDates = true;
                    break;
                case PathArgs.Source:
                    fda.Sources = true;
                    break;
                case PathArgs.Obs:
                    fda.Observations = true;
                    break;
                default:
                    throw new ParseException($"Argument {arg} is unrecognized or invalid for series path.");
            }
        }
        await client.CallAsync(x => x.DownloadService.Download(fda, null));
        Log.Information("Series path download completed successfully.");
    }


    private async Task GetCategories(string[] args)
    {
        bool getCategory = ! new string[] { FredDataArg.Children, FredDataArg.Related, FredDataArg.Series, FredDataArg.Tags }.Contains(args.Try(2));
        
        string? idsArg = getCategory ?  args.Try(2) : args.Try(3) ;

        if (string.IsNullOrEmpty(idsArg))
            throw new ParseException("One or more Category ID's is required.");
        else if(args.Length > (getCategory ? 3 : 4))
            throw new ParseException("Too many arguments.");

        string[] ids = idsArg.Split(',');

        foreach (string id in ids)
        {
            Log.Information("Starting Category download for ID {id}.", id);

            if (getCategory)
                await client.CallAsync(x => x.CategoriesService.DownloadCategory(id, null));
            else
            {
                switch (args[2])
                {
                    case FredDataArg.Children:
                        await client.CallAsync(x => x.CategoriesService.DownloadCategoryChildren(id, null));
                        break;
                    case FredDataArg.Related:
                        await client.CallAsync(x => x.CategoriesService.DownloadRelatedCategories(id, null));
                        break;
                    case FredDataArg.Series:
                        await client.CallAsync(x => x.CategoriesService.DownloadCategorySeries(id, null));
                        break;
                    case FredDataArg.Tags:
                        await client.CallAsync(x => x.CategoriesService.DownloadCategoryTags(id, null));
                        break;
                    default:
                        throw new ParseException($"Argument {args[2]} is not recognized.");
                }
            }
        }
        Log.Information("Category download completed successfully.");
    }

    private async Task GetReleases(string[] args) 
    {
        bool idRequired = new string[] { FredDataArg.Dates, FredDataArg.Series, FredDataArg.Sources }.Contains(args.Try(2));
        string? idsArg = idRequired ? args.Try(3) : args.Try(2); 

        if (args.Length > (idRequired ? 4 : 3))
            throw new ParseException("Too many arguments.");
        else if(string.IsNullOrEmpty(idsArg) && new string[] { FredDataArg.Series, FredDataArg.Sources}.Contains(args.Try(2)))
            throw new ParseException("One or more symbols are required.");

        string[] ids = idsArg?.Split(",") ?? new string[1] { string.Empty };

        foreach (string id in ids)
        {
            if(string.IsNullOrEmpty(id))
                Log.Information("Starting Release download.");
            else
                Log.Information("Starting Release download for ID {id}.", id);

            if (!idRequired)
            {
                if (string.IsNullOrEmpty(id))
                    await client.CallAsync(x => x.ReleasesService.DownloadAllReleases(null));
                else
                    await client.CallAsync(x => x.ReleasesService.DownloadRelease(id, null));
            }
            else
            {
                switch (args[2])
                {
                    case FredDataArg.Dates:

                        if (string.IsNullOrEmpty(id))
                            await client.CallAsync(x => x.ReleasesService.DownloadAllReleaseDates(null));
                        else
                            await client.CallAsync(x => x.ReleasesService.DownloadReleaseDates(id, null));

                        break;
                    case FredDataArg.Series:
                        await client.CallAsync(x => x.ReleasesService.DownloadReleaseSeries(id, null));
                        break;
                    case FredDataArg.Sources:
                        await client.CallAsync(x => x.ReleasesService.DownloadReleaseSources(id, null));
                        break;
                    default:
                        throw new ParseException($"Argument {args[2]} is not recognized.");
                }
            }
        }
        Log.Information("Release download completed successfully.");
    }
       
    public async Task GetSeries(string[] args) 
    {
        bool isID = ! new string[] { FredDataArg.Categories, FredDataArg.Release, FredDataArg.Tags, FredDataArg.Observations }.Contains(args.Try(2));
        string? idsArg = isID ? args.Try(2) : args.Try(3); // id can be not null 
        
        if (args.Length > (isID ? 3 : 4))
            throw new ParseException("Too many arguments.  If specifying multiple symbols seperate each symbol with a comma and do not include spaces: GNPCA,CPIAUSCL.");
        else if (string.IsNullOrEmpty(idsArg))
            throw new ParseException("Symbol is required.");

        string[] ids = idsArg.Split(",");

        foreach (string id in ids)
        {
            Log.Information("Starting Series download for ID {id}.", id);

            if (isID)
                await client.CallAsync(x => x.SeriesService.DownloadSeries(id, null));
            else
            {
                switch (args[2])
                {
                    case FredDataArg.Observations:
                        await client.CallAsync(x => x.ObservationsService.DownloadObservations(id, null));
                        break;
                    case FredDataArg.Categories:
                        await client.CallAsync(x => x.CategoriesService.DownloadCategoriesForSeries(id, null));
                        
                        break;
                    case FredDataArg.Release:
                        await client.CallAsync(x => x.SeriesService.DownloadSeriesRelease(id, null));
                        break;
                    case FredDataArg.Tags:
                        await client.CallAsync(x => x.SeriesService.DownloadSeriesTags(id, null));
                        break;
                    default:
                        throw new ParseException($"Argument {args[2]} is not recognized.");
                }
            }
        }
        Log.Information("Series download completed successfully.");
    }

    public async Task GetSources(string[] args) 
    {
        bool isID = new string[] { FredDataArg.Releases }.Contains(args.Try(2));
        string? idsArg = isID ? args.Try(3) : args.Try(2);

        if (args.Length > (isID ? 4 : 3))
            throw new ParseException("Too many arguments.  If specifying multiple symbols seperate each symbol with a comma and do not include spaces: GNPCA,CPIAUSCL.");
        else if(isID && string.IsNullOrEmpty(idsArg))
            throw new ParseException("Symbol is required.");

        if (string.IsNullOrEmpty(idsArg))
        {
            Log.Information("Starting Sources download.");
            await client.CallAsync(x => x.ReleasesService.DownloadAllSources(null));
        }
        else
        {
            string[] ids = idsArg.Split(",");

            foreach (string id in ids)
            {
                Log.Information("Starting Sources download for ID {id}.", id);

                if (!isID)
                {
                    await client.CallAsync(x => x.ReleasesService.DownloadSource(id, null));
                }
                else
                {
                    switch (args[2])
                    {
                        case FredDataArg.Releases:
                            await client.CallAsync(x => x.ReleasesService.DownloadSourceReleases(id, null));
                            break;
                        default:
                            throw new ParseException($"Argument {args[2]} is not recognized.");
                    }
                }
            }
        }
        Log.Information("Sources download completed successfully.");
    }

    private void ShowHelp()
    {
        // Do not clear the console - user may need to see an error message.
        Console.Write(File.ReadAllText("help.txt"));
    }

    private async Task ParseConfig(string[] args)
    {
        Console.WriteLine($"Observer CLI version {Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}");
        DatabaseStatus status = await dbHelper.VerifyDatabase();
        Console.WriteLine(dbHelper.EndPoint.ConnectionString);
        
        if (status == DatabaseStatus.ConsistentWithModel)
            Console.WriteLine("The database is up to date.");
        else
        {
            Console.WriteLine($"Database status is: {status}");
            
            if (args.Try(1) != ConfigArgument.UpdateDB)
                Console.WriteLine("Try running obs --config updatedb");
        }
        
        if (args.Try(1) == ConfigArgument.UpdateDB)
        {
            if (status != DatabaseStatus.ConsistentWithModel)
            {
                if (status == DatabaseStatus.NotConsistentWithModel)
                    Console.WriteLine("The database will be updated.");
                else 
                    Console.WriteLine("The database will be created.");
                
                Console.Write("Are you sure you want to continue? (y/n):");
                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.Y)
                {
                    try
                    {
                        Console.WriteLine("Database update starting...");
                        await dbHelper.UpdateDatabase();
                        Console.WriteLine("Database update completed sucessfully.");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Database update failed.");
                        throw new Exception("An error occured while attempting to create or update the database.", ex);
                    }
                }
            }
            else
                Console.WriteLine("The database is already up to date.  No action taken.");
        }
        else if(! string.IsNullOrEmpty(args.Try(1)))
            throw new ParseException($"Argument {args.Try(1)} is not recognized.");
    }

    private async Task CheckForUpdate(string[] args)
    {
        if (args.Length > 2)
            throw new ParseException("Too many arguments.  --update accepts one optional parameter, y, which will install an update if one is found.");

        string? installConfirmed = args.Try(1);

        if(! string.IsNullOrEmpty(installConfirmed) && installConfirmed != "y")
            throw new ParseException($"Invalid argument: {installConfirmed}.  --update accepts one optional parameter, y, which will install an update if one is found.");


        // Velopack
        var mgr = new UpdateManager(Program.ProgramUpdateUrl);

        // check for new version
        UpdateInfo? newVersion = null;

        try
        {
            newVersion = await mgr.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Log.Information("Check for program update failed.  The exception is: {e}", ex.ToString());
            return;
        }

        if (newVersion == null)
        {
            Log.Information("No update was found.  You are running the latest version of Observer.CLI");
            return; // no update available
        }
        // download new version
        if (! string.IsNullOrEmpty(installConfirmed))
        {
            Log.Information("An update for Observer.CLI is available and is being installed.");
            await mgr.DownloadUpdatesAsync(newVersion);

            // install new version and restart app
            mgr.ApplyUpdatesAndRestart(newVersion);
        }
        else
            Log.Information("An update for Observer.CLI is available however it will not be installed because the y argument was not passed.  To install the update run \"Obs.exe --update y\".");

    }


    
}
