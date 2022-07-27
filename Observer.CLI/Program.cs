using LeaderAnalytics.Vyntix.Fred.FredClient;
using Microsoft.Extensions.Configuration;

namespace LeaderAnalytics.Observer.CLI;

public class Program
{
    public static async Task Main(string[] args)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string logRoot = "logs";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logRoot, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, buffered: false)
            .CreateLogger(); 

        try
        {
            Console.Clear();
            Log.Information("Observer - Program.Main started.");
            Log.Information("Environment is: {env}", env ?? "Production");
            Log.Information("Log files will be written to {logRoot}", logRoot);
            Log.Information("Documentation for Observer can be found at https://vyntix.com/docs/");
            IHost host = CreateHostBuilder(args, env).Build();
            
            
            using (ILifetimeScope scope = host.Services.GetAutofacRoot().BeginLifetimeScope())
            {
                CommandParser parser = scope.Resolve<CommandParser>();
                await parser.Parse(args);
                string fredAPI_Key = scope.Resolve<IConfiguration>().GetValue<string>("FredAPI_Key");
                IAdaptiveClient<IObserverAPI_Manifest> apiClient = scope.Resolve<IAdaptiveClient<IObserverAPI_Manifest>>();
                var stuff = await apiClient.CallAsync(x => x.ObservationsService.GetLocalObservations("NROU"));
            }
        }
        catch (Exception ex)
        {
            if (ex is ObserverException)
                Log.Error(ex.Message);
            else
                Log.Error(ex.ToString());
        }
        finally
        {
            Log.CloseAndFlush();
            System.Threading.Thread.Sleep(1000);
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args, string env) =>
        Host.CreateDefaultBuilder(args)
        //.ConfigureDefaults(args)
        .ConfigureHostConfiguration(builder => {
            string fileName = string.IsNullOrEmpty(env) ? "appsettings.json" : $"appsettings.{env}.json";
            builder.AddJsonFile(fileName, false);
        })
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .UseSerilog()
        .ConfigureServices((config, services) => 
        {
            string apiKey = config.Configuration.GetValue<string>("FredAPI_Key");
            services.AddFredClient()
            .UseAPIKey(apiKey)
            .UseConfig(x => new FredClientConfig { MaxDownloadRetries = 1 });
        })
        .ConfigureContainer<ContainerBuilder>((config, containerBuilder) =>
        {
            containerBuilder.RegisterInstance<IConfiguration>(config.Configuration).SingleInstance();
            containerBuilder.RegisterType<CommandParser>().SingleInstance();
            IEnumerable<IEndPointConfiguration> endPoints = config.Configuration.GetSection("EndPoints").Get<IEnumerable<EndPointConfiguration>>();
            
            containerBuilder.RegisterModule(new LeaderAnalytics.Observer.Fred.Services.AutofacModule());
            RegistrationHelper registrationHelper = new RegistrationHelper(containerBuilder);
            new AdaptiveClientModule(endPoints).Register(registrationHelper);
        });
        
}
