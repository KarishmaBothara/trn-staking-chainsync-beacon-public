using System.Numerics;

namespace MatrixEngine.Core.Models;

public class BalanceChangeModel: BaseModel
{
    public string Account { get; set; }

    public int EraIndex { get; set; }

    public BigInteger PreviousBalance { get; set; }
    public BigInteger BalanceChange { get; set; }
    public BigInteger BalanceInBlockRange { get; set; }
    public int StartBlock { get; set; }
    public int EndBlock { get; set; }

    public int EffectiveBlocks { get; set; }

    public string? StakerType { get; set; }
    public decimal EffectiveEras { get; set; }
}