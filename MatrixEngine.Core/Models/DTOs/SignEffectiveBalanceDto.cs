namespace MatrixEngine.Core.Models.DTOs;

public class SignEffectiveBalanceDto
{
    public string? Account { get; set; }
    public int EraIndex { get; set; }
    public int StartBlock { get; set; }
    public int EndBlock { get; set; }
    public string? EffectiveBalance { get; set; }
}