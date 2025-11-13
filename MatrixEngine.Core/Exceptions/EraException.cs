namespace MatrixEngine.Core.Exceptions;

public class EraException : Exception
{
    public EraException()
    {
    }

    public EraException(string message) : base(message)
    {
    }

    public EraException(string message, Exception inner) : base(message, inner)
    {
    }
}