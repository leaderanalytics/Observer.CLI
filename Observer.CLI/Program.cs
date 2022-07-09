using Microsoft.Extensions.Configuration;

namespace Observer.CLI;

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
            string[] cmd = CommandParser.Parse(args);

            using (ILifetimeScope scope = host.Services.GetAutofacRoot().BeginLifetimeScope())
            {
                string fredAPI_Key = scope.Resolve<IConfiguration>().GetValue<string>("FredAPI_Key");
                IAdaptiveClient <IAPI_Manifest> apiClient = scope.Resolve<IAdaptiveClient<IAPI_Manifest>>();
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
        .ConfigureDefaults(args)
        .ConfigureHostConfiguration(builder => {
            string fileName = string.IsNullOrEmpty(env) ? "appsettings.json" : $"appsettings.{env}.json";
            builder.AddJsonFile(fileName, false);  
        })
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .UseSerilog()
        .ConfigureContainer<ContainerBuilder>((config, containerBuilder) =>
        {
            containerBuilder.RegisterInstance<IConfiguration>(config.Configuration).SingleInstance();
            IEnumerable<IEndPointConfiguration> endPoints = config.Configuration.GetSection("EndPoints").Get<IEnumerable<EndPointConfiguration>>();
            containerBuilder.RegisterModule(new LeaderAnalytics.Observer.Fred.Services.AutofacModule());
            RegistrationHelper registrationHelper = new RegistrationHelper(containerBuilder);
            new AdaptiveClientModule(endPoints).Register(registrationHelper);
        });
        
}
