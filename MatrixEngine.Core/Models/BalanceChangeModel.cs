using System.Numerics;

namespace MatrixEngine.Core.Models;

public class BalanceChangeModel: BaseModel
{
    public string Account { get; set; }
    public int StartBlock { get; set; }
    public int EndBlock { get; set; }
    // Balance details about the current bonded balance
    public BalanceChangeDetail Bonded { get; set; }
    // Balance details about the current unlocking balance
    public BalanceChangeDetail Unlocking { get; set; }
    
    public void DebugLog()
    {
        Console.WriteLine($"\n\nAccount:          {Account}");
        Console.WriteLine($"StartBlock:       {StartBlock}");
        Console.WriteLine($"EndBlock:         {EndBlock}");
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Unlocking.PreviousBalance:     {Unlocking.PreviousBalance}");
        Console.WriteLine($"Unlocking.BalanceChange:       {Unlocking.BalanceChange}");
        Console.WriteLine($"Unlocking.BalanceInBlockRange: {Unlocking.BalanceInBlockRange}");
        Console.WriteLine($"Unlocking.StakerType:          {Unlocking.StakerType}");
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Bonded.PreviousBalance:        {Bonded.PreviousBalance}");
        Console.WriteLine($"Bonded.BalanceChange:          {Bonded.BalanceChange}");
        Console.WriteLine($"Bonded.BalanceInBlockRange:    {Bonded.BalanceInBlockRange}");
        Console.WriteLine($"Bonded.StakerType:             {Bonded.StakerType}");
        Console.WriteLine("--------------------------------");
    }
}

public class BalanceChangeDetail
{
    public BigInteger PreviousBalance { get; set; }
    public BigInteger BalanceChange { get; set; }
    public BigInteger BalanceInBlockRange { get; set; }
    public string StakerType { get; set; }
    
    // Constructor for UnlockingBalanceDetail without staker type, defaults to Staker
    public BalanceChangeDetail(BigInteger previousBalance, BigInteger balanceChange, BigInteger balanceInBlockRange)
    {
        PreviousBalance = previousBalance;
        BalanceChange = balanceChange;
        BalanceInBlockRange = balanceInBlockRange;
        StakerType = Constants.StakerType.Staker;
    }
    
    // Constructor for BondedBalanceDetail with specified staker type
    public BalanceChangeDetail(BigInteger previousBalance, BigInteger balanceChange, BigInteger balanceInBlockRange, string stakerType)
    {
        PreviousBalance = previousBalance;
        BalanceChange = balanceChange;
        BalanceInBlockRange = balanceInBlockRange;
        StakerType = stakerType;
    }
    
}
