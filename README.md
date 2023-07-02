# Additional Configuration Sources

This is a spike to test using a SQL based IConfiguration source and one based on JSON files located on Azure.

## SQL Configuration Source

### Features

- It can refresh at a predefined interval
- Supports nested complex types
- Cannot be used (I don't think) to remove items from a collection/list/dictionary
- Collection/list/dictionary entries require the index or key although they are capable of handling gaps
- Can be combined with a default set read from Json files or env variables
- Can read connection strings defined in earlier configuration sources
- Unfortunately each property of each item of a collection/list/dictionary is specified by key so it can be quite verbose
- Hierarchical sources are still honoured e.g. appsettings -> appsettings.Environment -> Environment Variables -> Command Line -> Database
- Systems should use their own database wherever possible to store these values rather than coming back to a central db i.e. CmacCrm

### Setup

You need to create a database and table to store the config settings

```sql
CREATE TABLE [dbo].[Settings](
	[SettingKey] [nvarchar](50) NOT NULL,
	[SettingValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_Settings] PRIMARY KEY CLUSTERED
(
	[SettingKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
```

The syntax for how to input properties is colon delimited. Examples are:

```sql
INSERT INTO [dbo].[Settings]([SettingKey],[SettingValue])
SELECT N'WorkerSettings:IsEnabled', N'true' UNION ALL
SELECT N'WorkerSettings:NestedSettings:ArrayProp:0', N'2' UNION ALL
SELECT N'WorkerSettings:NestedSettings:ArrayProp:9', N'9' UNION ALL
SELECT N'WorkerSettings:NestedSettings:DicProp:xx', N'' UNION ALL
SELECT N'WorkerSettings:NestedSettings:GuidList:0', N'049D4D60-B896-4E7C-B7CC-0AF58F01A690' UNION ALL
SELECT N'WorkerSettings:NestedSettings:Prop2', N'also from db' UNION ALL
SELECT N'WorkerSettings:Prop1', N'From database'
GO
```

## Azure Blob Json File Configuration Source

### Features

- It can check for updates and refresh at a predefined interval
- Almost identical to the Json file/app settings configuration source which update checks
- Can read connection strings defined in earlier configuration sources
- Hierarchical sources are still honoured e.g. appsettings -> appsettings.Environment -> Environment Variables -> Command Line -> Database

### Setup

You need to create a storage account and container to store the config settings. You need both the connection string and the container name and the blob name/path. The first entry in the path is assumed to be the container name.

We can then create a file pretty much identical to the appsettings.

```json
{
  "WorkerSettings": {
    "IsEnabled": true,
    "NestedSettings": {
      "Prop2": "this is the prop2 value from blob",
      "DicProp": {
        "xx": "yy"
      }
    }
  }
}
```


## Key Points

The sample project uses a background service and therefore required some experimentation with IOptions, IOptionsSnapshot and IOptionsMonitor. Some are updated as expected, some are not. A summary is:

- IOptions: Resolved once and never updates
- IOptionsSnapshot: Resolved once per scope. So for long running services, only once, but for web apps, once per request
- IOptionsMonitor: Seems to reload as soon as the underlying IConfigurationRoot has it's values updated
