namespace LeaderAnalytics.Observer.CLI;

public class ParseException : Exception
{
    internal ParseException() { }
    internal ParseException(string message) : base(message) { }
}
