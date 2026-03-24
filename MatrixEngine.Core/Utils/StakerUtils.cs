using MatrixEngine.Core.Constants;

namespace MatrixEngine.Core.Utils;

public class StakerUtils
{
    public static decimal GetStakerRate(string type)
    {
        // Note we only have 5 decimal places of precision for staker rate
        // Any values below 0.00001 are rounded to 0.00000 in the EffectiveBalanceResolver
        return type switch
        {
            StakerType.Validator => 0.0739m,
            StakerType.Nominator => 0.0492m,
            _ => 0.0246m
        };
    }
}