namespace FocusBlock.Contracts;

public class AppConfig
{
    public List<BlockRuleConfig> BlockRules { get; set; } = [];
    public SecurityConfig Security { get; set; } = new();
}