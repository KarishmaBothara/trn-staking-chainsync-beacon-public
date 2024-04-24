namespace MatrixEngine.GraphQL.Config;

public class GraphqlApiOptions
{
   public static string SectionName { get; } = "GraphQLApi";

   public string BaseUrl { get; set; } = String.Empty; 
}