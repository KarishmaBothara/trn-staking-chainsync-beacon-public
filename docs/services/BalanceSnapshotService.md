# Balance Snapshot Service

## Introduction

The Balance Snapshot Service, implemented within the `MatrixEngine.Core.Services` namespace, is an essential component of a blockchain-based system, designed to manage and store balance snapshots at specific points in a blockchain’s lifecycle.These snapshots capture the state of account balances at given block heights and are critical for operations like reward calculations, auditing, and historical data analysis.

## Overview of Components

The Balance Snapshot Service comprises the `IBalanceSnapshotService` interface and its implementation, `BalanceSnapshotService`, which utilize MongoDB for data storage and management.This service is crucial for efficiently retrieving and updating balance information as part of blockchain operations.

### Namespace and Dependencies

- **Namespace**: `MatrixEngine.Core.Services`
- **Key Dependencies**:
  - `IMongoDatabase`: The MongoDB database connection that facilitates data storage and retrieval operations.
  - `BalanceSnapshotModel`: The data model representing a balance snapshot, detailing the account balance at a specific end block.

## Interface: `IBalanceSnapshotService`

The interface defines the methods necessary for managing balance snapshots, ensuring robust functionality for various system components that require historical balance data.

### Methods Defined

1. **GetBalanceSnapshotByAccount**:
   - **Purpose**: Fetches the balance snapshot for a specific account.
   - **Parameter**: `string account`—the identifier for the blockchain account.
   - **Returns**: An asynchronous task that yields a `BalanceSnapshotModel` for the specified account.

2. **GetBalanceSnapshotByEndBlock**:
   - **Purpose**: Retrieves all balance snapshots associated with a specific end block number.
   - **Parameter**: `int endBlock`—the block number at which the snapshot was recorded.
   - **Returns**: An asynchronous task that yields a list of `BalanceSnapshotModel`.

3. **HasCycleHaveBaseBalance**:
   - **Purpose**: Checks whether a given reward cycle has a base balance snapshot available.
   - **Parameter**: `RewardCycleModel cycle`—the reward cycle under consideration.
   - **Returns**: An asynchronous task that yields a boolean indicating the availability of a base balance snapshot.

4. **UpsertBalanceSnapshots**:
   - **Purpose**: Inserts or updates multiple balance snapshots in the database.
   - **Parameter**: `List<BalanceSnapshotModel> balanceSnapshots`—the list of balance snapshots to be upserted.
   - **Returns**: An asynchronous task performing the upsert operation.

## Implementation: `BalanceSnapshotService`

This class provides concrete implementation of the `IBalanceSnapshotService`, effectively managing the lifecycle of balance snapshots within the system’s database.

### Constructor and Dependency Injection

```csharp
public BalanceSnapshotService(IMongoDatabase database)
{
	_database = database;
}
```

The constructor ensures the MongoDB database instance is injected, facilitating direct interactions with the database for performing data operations.

### Detailed Method Implementations

#### GetBalanceSnapshotByAccount

- ** Logic**: Queries the MongoDB collection to find the balance snapshot for a specific account, ensuring that any query targets are indexed for performance.
- ** MongoDB Operations**: Uses `Find` combined with a filter on the `Account` field, and `SingleOrDefaultAsync` to retrieve the specific snapshot.

#### GetBalanceSnapshotByEndBlock

- ** Logic**: Retrieves snapshots for all accounts that have their last recorded balance at a specified end block, which is critical for end-of-cycle operations or historical analyses.
- ** MongoDB Operations**: Executes a `Find` operation with an equality filter on `EndBlock`, followed by `ToListAsync`.

#### HasCycleHaveBaseBalance

- ** Logic**: Determines the availability of initial balance data for a reward cycle, which is essential for starting new cycles or for recovery mechanisms.
- **MongoDB Operations**: May use `CountDocuments` to check for the existence of snapshots that match the start of the reward cycle.

#### UpsertBalanceSnapshots

- **Logic**: Performs an upsert operation for a batch of balance snapshots, ensuring that existing records are updated or new entries are created as needed.
- ** MongoDB Operations**: Likely uses `BulkWrite` with a combination of `ReplaceOneModel` with upsert options.

## Use Case Example

Consider the scenario of a blockchain system transitioning from one reward cycle to another:

1. ** End of Cycle**: At the end of a reward cycle, `GetBalanceSnapshotByEndBlock` is invoked to retrieve all balance snapshots at the last block of the cycle.
2. ** Snapshot Update**: As new transactions are processed that affect account balances, `UpsertBalanceSnapshots` is used to update the balance state at the end of the new cycle.
3. ** Cycle Initialization**: When initializing a new reward cycle, `HasCycleHaveBaseBalance` checks for existing snapshots to establish a starting point.

## Conclusion

The `IBalanceSnapshotService` and `BalanceSnapshotService` are integral to managing and maintaining accurate and timely snapshots of account balances within a blockchain environment.Their efficient implementation supports crucial functionalities such as reward distribution, auditing, and financial analysis