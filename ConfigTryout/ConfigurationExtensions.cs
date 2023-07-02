using Microsoft.Extensions.Configuration;

namespace ConfigTryout;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddSqlDatabase(this IConfigurationBuilder builder, Action<SqlDatabaseConfigurationSource>? configurationSource)
        => builder.Add(configurationSource);
    
    public static IConfigurationBuilder AddAzureBlobJson(this IConfigurationBuilder builder, Action<AzureBlobJsonFileConfigurationSource>? configurationSource)
        => builder.Add(configurationSource);
}