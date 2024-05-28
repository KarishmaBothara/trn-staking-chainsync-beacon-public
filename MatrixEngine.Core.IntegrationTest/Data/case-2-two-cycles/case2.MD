## Case 2 Data

### Assumption
Running behind a cycle. Need to calculate two cycles.
Going to fetch eras and transactions of data when needed.
It is expected the first cycle can be calculated and complete. 
Then it starts a new cycle and calculates up to the latest era.   

### Cycle to calculate
Cycle 1 starts from Era 349 and not finished yet.
Current Era Index in cycle 1 is 432, which means latest calculation finished at era 432. 
It is way behind from the current era 492. The latest finished is 492. The last unfinished Era is 493. 

Cycle 1: Need to calculate first
- start era: 349
- end era 438
- start Block: 8973464

Cycle 2: Second round to calculate
- start era: 439
- end era: 528
- start block: unknown. Need to get from Era data.

### Data Information
- eras.json: Eras from 349 to 493 (Era 493 end block is -1).
- balance-snapshots.json: Balance snapshots from cycle 0, it can be base balance for cycle 1. End block is 8973463.
- reward-circle.json: Reward circle up to cycle 1 and the current era index is 492. 
- transactions.json: Transactions that occurred from 349(block starts 8973464) to current era 492(block ends 12074059). Need a fixture to load by block range when need. 
- stakers.json: Record the stakers' type and staked amount in each era. Can be validator or nominator.

### Testing Goal
* Eras and reward cycle data are able to used to determine which era are balance change located in.
* The balance snapshots are able to be picked up as base balance when calculating balance changes.
* The transaction events are able to be calculated to balance changes.
* The stakers types are able to be used as part of effective balance calculation.
* (Highlighted) The second cycle can be created after the first one finishes.
* (Highlighted) The balance snapshot for cycle 1 can be created as base balance for cycle 2.


