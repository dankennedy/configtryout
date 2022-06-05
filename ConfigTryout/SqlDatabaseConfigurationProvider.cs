using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ConfigTryout;

public class SqlDatabaseConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly Timer? _refreshTimer;
    private SqlCommand? _command;
    
    public SqlDatabaseConfigurationProvider(SqlDatabaseConfigurationSource source)
    {
        Source = source;

        if (Source.RefreshInterval.HasValue)
            _refreshTimer = new Timer(_ => ReadDatabaseSettings(true), null, Timeout.Infinite, Timeout.Infinite);
    }

    public SqlDatabaseConfigurationSource Source { get; }

    public override void Load()
    {
        if (string.IsNullOrWhiteSpace(Source.ConnectionString))
            throw new ArgumentException("No connection string specified to lod configuration");

        CreateSqlCommand();

        ReadDatabaseSettings(false);

        if (_refreshTimer != null && Source.RefreshInterval.HasValue)
            _refreshTimer.Change(Source.RefreshInterval.Value, Source.RefreshInterval.Value);
    }

    private void CreateSqlCommand()
    {
        var sql = "SELECT SettingKey, SettingValue FROM dbo.Settings";
        if (!string.IsNullOrEmpty(Source.Filter) || !string.IsNullOrEmpty(Source.Prefix)) sql += " WHERE";

        if (!string.IsNullOrEmpty(Source.Filter))
            sql += " Filter = @filter";
        if (!string.IsNullOrEmpty(Source.Prefix))
            sql += $"{(!string.IsNullOrEmpty(Source.Filter) ? " AND" : "")} SettingKey LIKE @prefix";

        _command = new SqlCommand(sql);

        if (!string.IsNullOrEmpty(Source.Filter))
            _command.Parameters.AddWithValue("@filter", Source.Filter);
        if (!string.IsNullOrEmpty(Source.Prefix))
            _command.Parameters.AddWithValue("@prefix", Source.Prefix + '%');
    }

    private void ReadDatabaseSettings(bool isReload)
    {
        Console.WriteLine("Loading database settings: isReload: {0}", isReload);
        
        using var connection = new SqlConnection(Source.ConnectionString);

        _command!.Connection = connection;

        try
        {
            connection.Open();
            var reader = _command.ExecuteReader();

            var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            while(reader.Read())
            {
                try
                {
                    if (reader.IsDBNull(1)) continue;
                    settings[reader.GetString(0)] = reader.GetString(1);
                }
                catch (Exception readerEx)
                {
                    Console.WriteLine(readerEx);
                }
            }

            reader.Close();
            reader.Dispose();
            
            if (!isReload || !SettingsMatch(Data, settings))
            {
                Console.WriteLine("Applying settings {0}", settings);
                
                Data = settings;

                if (isReload)
                    OnReload();
            }
        }
        catch (Exception sqlEx)
        {
            Console.WriteLine(sqlEx);
        }
    }

    private bool SettingsMatch(IDictionary<string, string?> oldSettings, IDictionary<string, string?> newSettings)
    {
        if (oldSettings.Count != newSettings.Count)
            return false;

        if (!oldSettings
                .OrderBy(s => s.Key)
                .SequenceEqual(newSettings.OrderBy(s => s.Key)))
            return false;

        foreach (var key in oldSettings.Keys)
        {
            if (oldSettings[key] != newSettings[key]) 
                return false;
        }

        return true;
    }

    public void Dispose()
    {
        _refreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _refreshTimer?.Dispose();
    }
}