namespace ConfigTryout;

public class WorkerSettings
{
    public string Prop1 { get; set; }

    public bool IsEnabled { get; set; }
    
    public NestedSettingsClass NestedSettings { get; set; }
}

public class NestedSettingsClass
{
    private List<Guid> _guidList;
    public string Prop2 { get; set; }
    public int[] ArrayProp { get; set; }

    public List<Guid> GuidList
    {
        get => _guidList ?? new List<Guid>();
        set => _guidList = value;
    }

    public Dictionary<string, string> DicProp { get; set; }
    public Dictionary<string, Guid> Dic2Prop { get; set; }
}