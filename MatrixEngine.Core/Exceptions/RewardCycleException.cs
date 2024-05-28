namespace MatrixEngine.Core.Exceptions;

public class RewardCycleException : Exception
{
    public RewardCycleException()
    {
    }

    public RewardCycleException(string message) : base(message)
    {
    }

    public RewardCycleException(string message, Exception inner) : base(message, inner)
    {
    }
}