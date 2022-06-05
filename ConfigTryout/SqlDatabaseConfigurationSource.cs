using Microsoft.Extensions.Configuration;

namespace ConfigTryout;

public class SqlDatabaseConfigurationSource : IConfigurationSource
{
    public string? ConnectionString { get; set; }
    public TimeSpan? RefreshInterval { get; set; }
    public string Filter { get; set; }
    public string Prefix { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new SqlDatabaseConfigurationProvider(this);
}