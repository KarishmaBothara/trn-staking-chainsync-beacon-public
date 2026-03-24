using System.Numerics;
using System.Collections.Generic;

namespace MatrixEngine.Core.Models.DTOs
{
    // Represents the ledger of a (bonded) stash on a Substrate chain
    public class StakingLedgerModel
    {
        // The stash account whose balance is actually locked and at stake
        public string Stash { get; set; }
        
        // The total amount of the stash's balance that is currently accounted for.
        // It's just `active` plus all the `unlocking` balances.
        public BigInteger Total { get; set; }
        
        // The total amount of the stash's balance that will be at stake in any forthcoming rounds
        public BigInteger Active { get; set; }
    }
} 