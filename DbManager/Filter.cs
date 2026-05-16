namespace DbManager;
public class Filter
{
    public string Gop { get; set; } = "and";
    public List<Filter>Filters { get; set; } = new List<Filter>();
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Op { get; set; } = "equal";

}
