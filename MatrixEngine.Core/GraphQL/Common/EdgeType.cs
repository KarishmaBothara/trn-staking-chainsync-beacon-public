namespace MatrixEngine.Core.GraphQL.Common;

public class EdgeType<T>
{
   public string Cursor { get; set; }
   
   public T Node { get; set; }
}