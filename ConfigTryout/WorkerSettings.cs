namespace ConfigTryout;

public class WorkerSettings
{
    public string Prop1 { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public NestedSettingsClass NestedSettings { get; set; } = new();
}

public class NestedSettingsClass
{
    public string Prop2 { get; set; } = string.Empty;
    
    public int[] ArrayProp { get; set; } = Array.Empty<int>();
    
    public Dictionary<string, string> DicProp { get; set; } = new();
    
    public Dictionary<string, Guid> Dic2Prop { get; set; } = new();
}