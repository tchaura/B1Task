using System.IO;
using Microsoft.Extensions.Configuration;

namespace B1Task;

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