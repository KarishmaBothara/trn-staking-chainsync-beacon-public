namespace MatrixEngine.Core.Events;

/// <summary>
/// This class is responsible for fetching all events from the indexer for all calculations
/// </summary>
public class EventFetcher
{
    public EventFetcher()
    {
        
    }
    
    /*
     * Fetch transactional events from the indexer
     * Including bond and withdrawn (unbond event dose not take any effect to calculate the balance change)
     */
    public void FetchTransactionalEvents()
    {
        
    }
}