# Staker Service

## Introduction

The Staker Service is a crucial component of blockchain platforms that involve staking mechanisms, such as those found in Proof of Stake (PoS) systems. This service, encapsulated within the `MatrixEngine.Core.Services` namespace, provides functionalities related to managing and querying staker-related information, which is essential for determining the roles and attributes of participants in various staking processes.

## Overview of Components

This document details the `IStakerService` interface and its implementation, `StakerService`, which are designed to interact with a MongoDB database to handle data regarding stakers' activities and roles within specified eras of a blockchain.

### Namespace and Dependencies

- **Namespace**: `MatrixEngine.Core.Services`
- **Key Dependencies**:
  - `IMongoDatabase`: The MongoDB database connection used for data storage and retrieval.
  - `ILogger<StakerService>`: Provides logging capabilities for debugging and monitoring service operations.

## Interface: `IStakerService`

The `IStakerService` interface defines various methods to assist in managing and retrieving information about stakers, which is vital for the functionality of staking-based blockchain networks.

### Methods Defined

1. **GetAccountType**:
   - **Purpose**: Retrieves the type of account for a given staker within a specific era.
   - **Parameters**:
     - `string account`: The account identifier.
     - `int eraIndex`: The index of the era.
   - **Returns**: An asynchronous task that yields the type of the staker as a string, possibly null if no data exists.

2. **GetAccountStakerTypesByEraIndexes**:
   - **Purpose**: Fetches staking details for a single account across multiple eras.
   - **Parameters**:
     - `string account`: The account identifier.
     - `List<int> eraIndexes`: A list of era indices to query.
   - **Returns**: An asynchronous task that yields a list of `StakerModel` instances.

3. **GetAccountsStakerTypesByEraIndexes**:
   - **Purpose**: Retrieves staking information for multiple accounts across specified eras.
   - **Parameters**:
     - `List<string> accounts`: A list of account identifiers.
     - `List<int> eraIndexes`: A list of era indices.
   - **Returns**: An asynchronous task that yields a list of `StakerModel` instances.

4. **ResolveStakersAndSave**:
   - **Purpose**: Processes and saves staker data from a list of staker node types.
   - **Parameters**:
     - `List<StakerNodeType> stakerTypes`: A list of staker node types to be processed and saved.
   - **Returns**: An asynchronous task.

5. **GetLatestEraFetchedStakerTypes**:
   - **Purpose**: Retrieves the index of the latest era for which staker types have been fetched and processed.
   - **Returns**: An asynchronous task that yields the era index.

## Implementation: `StakerService`

This class provides the implementation for the `IStakerService` interface, utilizing a MongoDB database to perform operations related to staker data.

### Constructor and Dependency Injection

```csharp
public StakerService(IMongoDatabase database, ILogger<StakerService> logger)
{
    _logger = logger;
    _database = database;
}
```

The constructor ensures that the necessary dependencies for database interaction and logging are provided, facilitating effective data management and operational transparency.

### Detailed Method Implementations

#### GetAccountType

- **Logic**: Queries the database to find the staker type for a specific account in a given era.
- **MongoDB Operations**: Utilizes `Find` with appropriate filters based on account and era index.

#### GetAccountStakerTypesByEraIndexes & GetAccountsStakerTypesByEraIndexes

- **Logic**: These methods extend the functionality to handle single and multiple accounts, respectively, retrieving staker information for specified eras.
- **MongoDB Operations**: Executes complex queries that involve filtering based on multiple criteria (accounts and era indices).

#### ResolveStakersAndSave

- **Logic**: Processes incoming staker node types and updates or inserts the data into the database, ensuring that the latest information is accurately recorded.
- **MongoDB Operations**: Likely involves bulk operations such as `BulkWrite` to efficiently handle multiple data entries.

#### GetLatestEraFetchedStakerTypes

- **Logic**: Retrieves the highest era index for which staker data has been processed, helping to synchronize data processing tasks.
- **MongoDB Operations**: A query that sorts the era indices and retrieves the maximum value.

## Use Case Example

Imagine a blockchain network transitioning to a new era:

1. **Staker Role Update**: As the new era begins, `GetAccountsStakerTypesByEraIndexes` is invoked to fetch updated staker roles for all active accounts.
2. **Data Processing**: During this transition,

 `ResolveStakersAndSave` processes new staker information as nodes declare their intentions for the new era.
3. **Historical Reference**: Periodically, `GetLatestEraFetchedStakerTypes` is called to ensure that all staker information is up to date and that no era has been skipped in data processing.

## Conclusion

The `IStakerService` and `StakerService` provide essential functionalities for managing staker information within a staking-based blockchain network. Their implementation ensures robust data management, operational transparency, and supports the dynamic nature of staking activities in blockchain systems.