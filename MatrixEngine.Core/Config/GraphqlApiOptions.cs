namespace MatrixEngine.Core.Config;

public class GraphqlApiOptions
{
   public static string SectionName = "GraphQLApi";

   public string BaseUrl { get; set; } = String.Empty; 
}