namespace LeaderAnalytics.Observer.CLI;

public static class ExtensionMethods
{
    public static T? Try<T>(this T[] array, int index) => array.Length > index ? array[index] : default(T);
}
