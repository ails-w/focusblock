namespace FocusBlock.Contracts;

public class BlockRuleConfig
{
    public string AppName { get; set; } = "";
    public TimeOnly StartTime { get; set; } = new(9, 0);
    public TimeOnly EndTime { get; set; } = new(17, 0);
    public bool Enabled { get; set; } = true;
}