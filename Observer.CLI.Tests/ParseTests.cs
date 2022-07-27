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
    public async Task Test1()
    {
        string[] args = new string[] { DataProvider.Fred, FredDataType.Category, FredDataArg.Children, "125"};
        await commandParser.Parse(args);
        
        //args[0] = CommandArgument.Config;
        //Assert.DoesNotThrow(() => commandParser.Parse(args));
        
        //args[0] = CommandArgument.Help;
        //Assert.DoesNotThrow(() => commandParser.Parse(args));
        
        //args[0] = "blah";
        //Assert.Throws<ObserverException>(() => commandParser.Parse(args));

    }
}