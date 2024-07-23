using Microsoft.Extensions.Configuration;
using Velopack;
using Velopack.Windows;

namespace LeaderAnalytics.Observer.CLI;

public class Program
{
    public const string DocumentationURL = "https://vyntix.com/docs/";
    public static string ConfigFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeaderAnalytics", "Vyntix", "Observer.CLI");
    public const string DevelopmentConfigFileSourceFolder = "O:\\LeaderAnalytics\\Config\\Observer.CLI";
    public static string ProgramUpdateUrl { get; private set; }

    private const string logRoot = "logs\\log.txt";
    // We never load the config directly from this folder - we copy from this folder to configFilePath if the development config file does not exist there.
    // Observer.Desktop and Observer.CLI share a config
    public static string exePath {  get; private set; }

    public static async Task Main(string[] args)
    {
        LeaderAnalytics.Core.EnvironmentName environmentName = LeaderAnalytics.Core.RuntimeEnvironment.GetEnvironmentName();
        CreateLogger();
        exePath = AppContext.BaseDirectory;

        try
        {
            VelopackApp.Build()
                .WithAfterInstallFastCallback(v => new Shortcuts().RemoveShortcutForThisExe(ShortcutLocation.StartMenu))
                .Run();
            Console.Clear();
            Log.Information("Observer - Program.Main started.");
            Log.Information("Environment is: {env}", environmentName);
            Log.Information("Log files will be written to {logRoot}", logRoot);
            Log.Information($"Documentation for Observer can be found at {DocumentationURL}");
            // if we are running in prod copy the config file from the same folder as the .exe, otherwise copy the dev config from a share
            string sourceFolder = environmentName == Core.EnvironmentName.production ? string.Empty : DevelopmentConfigFileSourceFolder;
            ConfigHelper.CopyConfigFromSource(environmentName, sourceFolder, ConfigFilePath);
            IConfigurationRoot config = await ConfigHelper.BuildConfig(environmentName, ConfigFilePath);
            ProgramUpdateUrl = config["ProgramUpdateURL"];
            IHost host = CreateHostBuilder(args, config).Build();
            
            
            using (ILifetimeScope scope = host.Services.GetAutofacRoot().BeginLifetimeScope())
            {
                CommandParser parser = scope.Resolve<CommandParser>();
                await parser.Parse(args);
                string fredAPI_Key = scope.Resolve<IConfiguration>().GetValue<string>("FredAPI_Key");
                IAdaptiveClient<IAPI_Manifest> apiClient = scope.Resolve<IAdaptiveClient<IAPI_Manifest>>();
            }
        }
        catch (Exception ex)
        {
            if (ex is ParseException)
                Log.Error(ex.Message);
            else
                Log.Error(ex.ToString());
        }
        finally
        {
            Log.CloseAndFlush();
            Thread.Sleep(1000);
        }
    }

    public static void CreateLogger()
    {
        Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
               .Enrich.FromLogContext()
               .WriteTo.Console()
               .WriteTo.File(logRoot, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, buffered: false)
               .CreateLogger();
    }

    public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot config)
    {
        HostBuilder builder = new();

        builder.ConfigureHostConfiguration(builder =>
        {
            
            
            builder.AddConfiguration(config);
        })
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .UseSerilog()
        .ConfigureServices((config, services) =>
        {
            string apiKey = config.Configuration.GetValue<string>("FredAPI_Key");

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception($"FRED API key not found. See the documentation at {DocumentationURL} for instructions on how to obtain a FRED API key and how to add a FredAPI_Key section to appsettings.json.");

            services.AddFredClient()
            .UseAPIKey(apiKey)
            .UseConfig(x => new FredClientConfig { MaxDownloadRetries = 3 }); // MaxDownloadRetries < 3 is not recomended
        })
        .ConfigureContainer<ContainerBuilder>((config, containerBuilder) =>
        {
            containerBuilder.RegisterInstance<IConfiguration>(config.Configuration).SingleInstance();
            containerBuilder.RegisterType<CommandParser>().SingleInstance();
            containerBuilder.RegisterType<DbHelper>().SingleInstance();

            IEnumerable<IEndPointConfiguration> endPoints = config.Configuration.GetSection("EndPoints").Get<IEnumerable<EndPointConfiguration>>();

            if (!(endPoints?.Any(x => x.IsActive) ?? false))
                throw new Exception("No active endPoints were found.  Make sure one or more endPoints are defined in appsettings.json and that IsActive is set to true for one endPoint only.");
            else if (endPoints.Count(x => x.IsActive) > 1)
                throw new Exception("Only one endPoint can be active at a time.  Check the EndPoints section in appsettings.json and make sure IsActive is set to True for one endPoint only.");

            containerBuilder.RegisterInstance(endPoints.First(x => x.IsActive)).SingleInstance();
            containerBuilder.AddFredDownloaderServices(endPoints);
            RegistrationHelper registrationHelper = new RegistrationHelper(containerBuilder);
            new AdaptiveClientModule(endPoints).Register(registrationHelper);
        });
        return builder;
    }
        
}
