namespace MatrixEngine.Core.Exceptions;

public class BalanceSnapshotException : Exception
{
    public BalanceSnapshotException()
    {
    }

    public BalanceSnapshotException(string message) : base(message)
    {
    }

    public BalanceSnapshotException(string message, Exception inner) : base(message, inner)
    {
    }
}