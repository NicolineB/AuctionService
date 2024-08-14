using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class LoggingServiceProvider
{
    public static ServiceProvider GetServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(configure => configure.AddConsole());
        return serviceCollection.BuildServiceProvider();
    }

    public static ILogger<T> CreateLogger<T>()
    {
        var serviceProvider = GetServiceProvider();
        var factory = serviceProvider.GetService<ILoggerFactory>();
        if (factory == null)
        {
            throw new Exception("Failed to get ILoggerFactory from the service provider.");
        }
        return factory.CreateLogger<T>();
    }
}
