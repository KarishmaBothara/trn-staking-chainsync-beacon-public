## Case 1 Data

### Assumption
Running during a cycle and all transaction data has been fetched. 

### Cycle to calculate 
Cycle starts from Era 349 and not finished yet. Current Era is 419.  
 - start era: 349
 - current era 419
 - end era 438
 - start Block: 8973464

### Data Information
 - balance-snapshots.json: Balance snapshots ending at previous cycle. End block is 8973463.
 - eras.json: Eras from 349 to 419. 420 is the current era with end block -1.
 - reward-circle.json: Reward circle up to current.
 - transactions.json: Transactions that occurred from 349 to 419
 - stakers.json: Record the stakers' type and staked amount in each era. Can be validator or nominator.

### Testing Goal
* Eras and reward cycle data are able to used to determine which era are balance change located in.
* The balance snapshots are able to be picked up as base balance when calculating balance changes.
* The transaction events are able to be calculated to balance changes.
* The stakers types are able to be used as part of effective balance calculation.

