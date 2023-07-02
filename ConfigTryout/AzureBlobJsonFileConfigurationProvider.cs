using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace ConfigTryout;

public class AzureBlobJsonFileConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly Timer _refreshTimer;
    private BlobClient? _blobClient;
    private DateTimeOffset _lastModified = DateTimeOffset.MinValue;
    private Stack<string> _paths = new();
    private Dictionary<string, string?> _data = new(StringComparer.OrdinalIgnoreCase);

    public AzureBlobJsonFileConfigurationSource Source { get; }

    public AzureBlobJsonFileConfigurationProvider(AzureBlobJsonFileConfigurationSource source)
    {
        Source = source;
        _refreshTimer = new Timer(_ => ReadFile(true), null, Timeout.Infinite, Timeout.Infinite);
    }

    public override void Load()
    {
        if (string.IsNullOrWhiteSpace(Source.ConnectionString))
            throw new ArgumentException("No connection string specified to load configuration file");

        if (string.IsNullOrWhiteSpace(Source.BlobPath))
            throw new ArgumentException("No blob path specified to load configuration file");

        if (Source.RefreshInterval.TotalSeconds < 10)
            throw new ArgumentException("Refresh interval must be at least 10 seconds");

        CreateBlobClientAndCheckFileExists();

        ReadFile(false);

        _refreshTimer.Change(Source.RefreshInterval, Source.RefreshInterval);
    }

    private void CreateBlobClientAndCheckFileExists()
    {
        var pathParts =
            Source.BlobPath.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length <= 1 || !Path.HasExtension(pathParts[pathParts.Length - 1]))
            throw new ArgumentException(
                "The blob path specified is invalid. It should be made up of the container name and path to the file to load which has an extensions of .json");

        var blobServiceClient = new BlobServiceClient(Source.ConnectionString);
        _blobClient = blobServiceClient.GetBlobContainerClient(pathParts[0])
            .GetBlobClient(string.Join('/', pathParts.Skip(1)));
        
        _lastModified = _blobClient.GetProperties().Value.LastModified;
    }

    private void ReadFile(bool isReload)
    {
        if (_blobClient == null)
            throw new InvalidOperationException("The blob client has not been initialised");

        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var cancellationToken = cancellationTokenSource.Token;

            // If we have a last modified date and we're reloading, check if the file has changed
            if (isReload)
            {
                Console.WriteLine("Checking if file has changed...");
                var props = _blobClient.GetProperties(cancellationToken: cancellationToken);
                if (props.Value.LastModified <= _lastModified)
                    return;
            }

            Console.WriteLine("Reloading...");
            
            using (var stream = new MemoryStream())
            {
                _blobClient.DownloadTo(stream, cancellationToken: cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);
                Data = Parse(stream);

                if (Source.OnLood != null)
                    Source.OnLood();

                _lastModified = _blobClient.GetProperties(cancellationToken: cancellationToken).Value.LastModified;
            }

            if (isReload)
                OnReload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    // DK: This is a copy of the JsonConfigurationFileParser.Parse method as it's marked internal
    private IDictionary<string, string?> Parse(Stream input)
    {
        _data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        _paths = new Stack<string>();
        
        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        using (var reader = new StreamReader(input))
        using (var doc = JsonDocument.Parse(reader.ReadToEnd(), jsonDocumentOptions))
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("Invalid json contents found in config file");
            }

            VisitElement(doc.RootElement);
        }

        return _data;
    }

    private void VisitElement(JsonElement element)
    {
        var isEmpty = true;

        foreach (var property in element.EnumerateObject())
        {
            isEmpty = false;
            EnterContext(property.Name);
            VisitValue(property.Value);
            ExitContext();
        }

        if (isEmpty && _paths.Count > 0)
        {
            _data[_paths.Peek()] = null;
        }
    }

    private void VisitValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                VisitElement(value);
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var arrayElement in value.EnumerateArray())
                {
                    EnterContext(index.ToString());
                    VisitValue(arrayElement);
                    ExitContext();
                    index++;
                }

                break;

            case JsonValueKind.Number:
            case JsonValueKind.String:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                var key = _paths.Peek();
                if (_data.ContainsKey(key))
                {
                    throw new FormatException($"Configuration key {key} is duplicated");
                }

                _data[key] = value.ToString();
                break;

            default:
                throw new FormatException("Unsupported JSON token type");
        }
    }

    private void EnterContext(string context) =>
        _paths.Push(_paths.Count > 0 ? _paths.Peek() + ConfigurationPath.KeyDelimiter + context : context);

    private void ExitContext() => _paths.Pop();

    public void Dispose()
    {
        _refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _refreshTimer.Dispose();
    }
}