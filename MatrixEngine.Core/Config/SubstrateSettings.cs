namespace MatrixEngine.Core.Substrate
{
    /// Settings for Substrate node connectivity
    public class SubstrateSettings
    {
        /// The ws endpoint for the TRN node
        public string NodeApiEndpoint { get; set; } = string.Empty;
        
        /// The current network we are on
        public string Network { get; set; } = string.Empty;
    }
} 