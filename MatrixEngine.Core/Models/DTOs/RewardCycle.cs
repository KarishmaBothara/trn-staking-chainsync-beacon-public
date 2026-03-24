namespace MatrixEngine.Core.Models.DTOs;

public class RewardCycle
{
   public int VtxDistributionId { get; set; }
   public int StartEraIndex { get; set; }

   public int EndEraIndex { get; set; }

   public int StartBlock { get; set; }

   public int EndBlock { get; set; }

   // Useful debug function to log the state of a Reward Cycle
   public void DebugLog()
   {
      Console.WriteLine("\n\nReward Cycle:");
      Console.WriteLine($"Start Era Index: {StartEraIndex}");
      Console.WriteLine($"End Era Index: {EndEraIndex}");
      Console.WriteLine($"Start Block: {StartBlock}");
      Console.WriteLine($"End Block: {EndBlock}");
      Console.WriteLine($"Vtx Distribution ID: {VtxDistributionId}");
      Console.WriteLine();
   }
}