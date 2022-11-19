using LeaderAnalytics.Vyntix.Fred.FredClient;
using Microsoft.Extensions.Configuration;

namespace LeaderAnalytics.Observer.CLI;

public class Program
{
    public const string DocumentationURL = "https://vyntix.com/docs/";
    private const string logRoot = "logs\\log.txt";

    public static async Task Main(string[] args)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        CreateLogger();

        try
        {
            Console.Clear();
            Log.Information("Observer - Program.Main started.");
            Log.Information("Environment is: {env}", env ?? "Production");
            Log.Information("Log files will be written to {logRoot}", logRoot);
            Log.Information($"Documentation for Observer can be found at {DocumentationURL}");
            IHost host = CreateHostBuilder(args, env).Build();
            
            
            using (ILifetimeScope scope = host.Services.GetAutofacRoot().BeginLifetimeScope())
            {
                CommandParser parser = scope.Resolve<CommandParser>();
                await parser.Parse(args);
                string fredAPI_Key = scope.Resolve<IConfiguration>().GetValue<string>("FredAPI_Key");
                IAdaptiveClient<IObserverAPI_Manifest> apiClient = scope.Resolve<IAdaptiveClient<IObserverAPI_Manifest>>();
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

    public static IHostBuilder CreateHostBuilder(string[] args, string env)
    {
        HostBuilder builder = new();

        builder.ConfigureHostConfiguration(builder =>
        {
            string fileName = string.IsNullOrEmpty(env) ? "appsettings.json" : $"appsettings.{env}.json";
            builder.AddJsonFile(fileName, false);
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
            containerBuilder.RegisterModule(new LeaderAnalytics.Observer.Fred.Services.AutofacModule());
            RegistrationHelper registrationHelper = new RegistrationHelper(containerBuilder);
            new AdaptiveClientModule(endPoints).Register(registrationHelper);
        });
        return builder;
    }
        
}
