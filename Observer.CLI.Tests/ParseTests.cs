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

        args = new string[] { DataProvider.Fred, FredDataType.Release };
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
        // empty ID for FredDataArg
        string[] args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Series };
        Assert.ThrowsAsync<ParseException>(async () => await commandParser.Parse(args));

        // too many arguments
        args = new string[] { DataProvider.Fred, FredDataType.Release, "", "extra" };
        Assert.ThrowsAsync<ParseException>(async () => await commandParser.Parse(args));

        // too many arguments
        args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Dates, "53", "extra" };
        Assert.ThrowsAsync<ParseException>(async () => await commandParser.Parse(args));
        
        // bad ID
        args = new string[] { DataProvider.Fred, FredDataType.Release, FredDataArg.Dates, "55555"};
        Assert.ThrowsAsync<Exception>(async () => await commandParser.Parse(args)); // Thrown by the service, not the parser
    }

    [Test]
    public async Task SeriesTests()
    {
        string[] args = new string[] { DataProvider.Fred, FredDataType.Series, "GNPCA" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Series, "GNPCA,NROU" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Series, FredDataArg.Observations, "GNPCA,CPIAUCSL"};
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Series, FredDataArg.Categories, "GNPCA" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Series, FredDataArg.Release, "GNPCA" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Series, FredDataArg.Tags, "GNPCA" };
        await commandParser.Parse(args);
    }

    [Test]
    public async Task SeriesExceptionTests()
    {
        // missing ID
        string[] args = new string[] { DataProvider.Fred, FredDataType.Series };
        Assert.ThrowsAsync<ParseException>(async () => await commandParser.Parse(args));

        // empty ID
        args = new string[] { DataProvider.Fred, FredDataType.Series, " " };
        Assert.ThrowsAsync<ParseException>(async () => await commandParser.Parse(args));

        // wrong arg
        args = new string[] { DataProvider.Fred, FredDataType.Series, FredDataArg.Dates,  "GNPCA" };
        Assert.ThrowsAsync<ParseException>(async () => await commandParser.Parse(args));

        // multiple id's with spaces.  When parsed from the command line the last symbol will be an extra arg because a space is included 
        args = new string[] { DataProvider.Fred, FredDataType.Series, "GNPCA", "CPIAUSCL " };
        Assert.ThrowsAsync<ParseException>(async () => await commandParser.Parse(args));
    }

    [Test]
    public async Task SourcesTests()
    {
        string[] args = new string[] { DataProvider.Fred, FredDataType.Source};
        await commandParser.Parse(args);
        
        args = new string[] { DataProvider.Fred, FredDataType.Source, "1" };
        await commandParser.Parse(args);

        args = new string[] { DataProvider.Fred, FredDataType.Source, FredDataArg.Releases, "1" };
        await commandParser.Parse(args);
    }
}