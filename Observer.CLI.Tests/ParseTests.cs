using Microsoft.Extensions.Hosting;

namespace LeaderAnalytics.Observer.CLI.Tests;

public class Tests
{
    private CommandParser commandParser;

    [SetUp]
    public void Setup()
    {
        IHost host = Program.CreateHostBuilder(null, "development").Build();
        ILifetimeScope scope = host.Services.GetAutofacRoot().BeginLifetimeScope();
        commandParser = scope.Resolve<CommandParser>();
    }

    [Test]
    public async Task CategoryTests()
    {
        string[] args = new string[] { DataProvider.Fred, FredDataType.Category, "125" };
        await commandParser.Parse(args);
        
        args = new string[] { DataProvider.Fred, FredDataType.Category, FredDataArg.Children, "13"};
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Category, FredDataArg.Related, "32073" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Category, FredDataArg.Series, "125" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Category, FredDataArg.Tags, "125" };
        await commandParser.Parse(args);
    }

    [Test]
    public async Task ReleasesTests()
    {
        string[] args = new string[] { DataProvider.Fred, FredDataType.Release, null };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Dates };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Release, "53" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Dates, "53" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Series, "51" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Sources, "51" };
        await commandParser.Parse(args);
    }

    [Test]
    public async Task ReleasesExceptionTests()
    {
        // empty ID
        string[] args = new string[] { DataProvider.Fred, FredDataType.Release, "" };
        Assert.ThrowsAsync<ArgumentNullException>(async () => await commandParser.Parse(args));

        // too many arguments
        args = new string[] { DataProvider.Fred, FredDataType.Release, "", "extra" };
        Assert.ThrowsAsync<Exception>(async () => await commandParser.Parse(args));

        // too many arguments
        args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Dates, "53", "extra" };
        Assert.ThrowsAsync<Exception>(async () => await commandParser.Parse(args));
        
        // bad ID
        args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Dates, "55555"};
        Assert.ThrowsAsync<Exception>(async () => await commandParser.Parse(args));
    }
}