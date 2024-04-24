using MatrixEngine.Core.Services;
using Microsoft.Extensions.Configuration;

namespace MatrixEngine.Core;

public class App
{
    private readonly IConfiguration _configuration;
    private readonly IRewardCycleService _rewardCycleService;

    public App(IConfiguration configuration, IRewardCycleService rewardCycleService)
    {
        _rewardCycleService = rewardCycleService;
        _configuration = configuration;
    }

    public void Run(string[] args)
    {
        // version settings
        var version = _configuration["Version"];
        Console.WriteLine("version " + version);

        // reward cycle
        var rewardCycle = _rewardCycleService.GetCurrentRewardCycle().Result;
        Console.WriteLine("reward cycle " + rewardCycle.StartBlock + " - " + rewardCycle.EndBlock);
    }
}