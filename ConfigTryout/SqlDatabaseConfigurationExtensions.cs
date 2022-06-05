﻿using Microsoft.Extensions.Configuration;

namespace ConfigTryout;

public static class SqlDatabaseConfigurationExtensions
{
    public static IConfigurationBuilder AddSqlDatabase(this IConfigurationBuilder builder, Action<SqlDatabaseConfigurationSource>? configurationSource)
        => builder.Add(configurationSource);
}