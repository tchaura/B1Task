namespace B1Task;

using Microsoft.Extensions.Configuration;
using System.IO;

public class ConfigurationHelper
{
    public static IConfigurationRoot GetConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddUserSecrets<ConfigurationHelper>();

        return builder.Build();
    }
}