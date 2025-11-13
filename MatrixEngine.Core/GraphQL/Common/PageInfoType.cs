namespace MatrixEngine.Core.GraphQL.Common;

public class PageInfoType
{
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string StartCursor { get; set; }
    public string EndCursor { get; set; }
}