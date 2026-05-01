namespace TrueAltitude.Domain.Entities;

public class Setting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
