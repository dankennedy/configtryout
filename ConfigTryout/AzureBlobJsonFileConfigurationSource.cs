using Microsoft.Extensions.Configuration;

namespace ConfigTryout;

public class AzureBlobJsonFileConfigurationSource : IConfigurationSource
{
    public string ConnectionString { get; set; } = "";
    public string BlobPath { get; set; } = "";
    public TimeSpan RefreshInterval { get; set; }
    public Action? OnLood { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new AzureBlobJsonFileConfigurationProvider(this);
}