using MatrixEngine.Core.Constants;

namespace MatrixEngine.Core.Utils;

public class StakerUtils
{
    public static decimal GetStakerRate(string type)
    {
        return type switch
        {
            StakerType.Validator => 0.0739m,
            StakerType.Nominator => 0.0492m,
            _ => 0.0246m
        };
    }
}