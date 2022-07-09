namespace Observer.CLI;
public static class CommandParser
{
    public static string[] Parse(string[] args)
    {
        if(args is null || args.Length == 0)
            args = new string[1];

        if (string.IsNullOrEmpty(args[0]))
            args[0] = "--help";
            
        return args;
    }
}
