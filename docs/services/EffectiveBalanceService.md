# Effective Balance Service

## Introduction

The `IEffectiveBalanceService` interface and its concrete implementation `EffectiveBalanceService` are components of a software system designed to manage and query effective balance data for blockchain accounts. This document provides an in-depth look at their functionality, design, and operational context, ensuring a clear understanding of their role within the `MatrixEngine.Core.Services` namespace.

## Overview of Components

This section outlines the services provided by these components, their dependencies, and their integration within a broader system that interacts with blockchain data.

### Namespace and Dependencies

- **Namespace**: `MatrixEngine.Core.Services`
- **Key Dependency**:
  - `IMongoDatabase`: The MongoDB database connection that stores and retrieves blockchain-related data.

## Interface: `IEffectiveBalanceService`

This interface defines the operations necessary for managing effective balances, which are critical for accurate accounting and auditing within blockchain financial systems.

### Methods Defined

1. **GetEffectiveBalancesByAccount**:
   - **Purpose**: Retrieves a list of effective balance models for a specified account.
   - **Parameter**: `string account`—the blockchain account identifier.
   - **Returns**: An asynchronous task that yields a list of `EffectiveBalanceModel`.

2. **UpsertEffectiveBalance**:
   - **Purpose**: Inserts or updates effective balance data in the database.
   - **Parameter**: `List<EffectiveBalanceModel> data`—the data to be upserted.
   - **Returns**: An asynchronous task that performs the insert/update operation.

## Implementation: `EffectiveBalanceService`

This class provides the implementation details of the `IEffectiveBalanceService`, utilizing MongoDB to store and manage effective balance data.

### Constructor and Dependency Injection

```csharp
public EffectiveBalanceService(IMongoDatabase database)
{
    _database = database;
}
```

The constructor injects an `IMongoDatabase` object, establishing a direct link to the database used for data storage and retrieval.

### Detailed Method Implementations

#### GetEffectiveBalancesByAccount

- **Logic**: Constructs a MongoDB filter based on the account identifier and retrieves matching records from the `EffectiveBalance` collection.
- **MongoDB Operations**: Utilizes the `Find` method combined with a filter on the `Account` field, followed by `ToListAsync` to fetch the data.
- **Usage**: Called whenever there is a need to fetch all effective balance entries associated with a specific blockchain account, typically used in financial queries or audits.

#### UpsertEffectiveBalance

- **Logic**: Performs an upsert operation, which updates existing records if they match certain criteria or inserts new records if no matching records exist.
- **MongoDB Operations**: Likely utilizes the `ReplaceOneAsync` method with an upsert option, though the specifics are abstracted in the snippet provided.
- **Usage**: Essential for maintaining current and accurate balance data, used during transactions, balance updates, or when new balance data is calculated and needs to be stored.

## Use Case Example

To illustrate the practical application of `EffectiveBalanceService`, consider a scenario in a blockchain application that requires updating the balance following a transaction:

1. **Transaction Processing**: After a transaction is confirmed, the system calculates the new effective balance for the involved accounts.
2. **Updating Database**: The `UpsertEffectiveBalance` method is called with the new balance data, ensuring the database reflects the latest state.
3. **Audit or Query**: Later, an audit or query operation uses `GetEffectiveBalancesByAccount` to retrieve and verify the balance records for specific accounts, validating the transaction's effects.

## Conclusion

The `IEffectiveBalanceService` and `EffectiveBalanceService` provide crucial functionalities for managing effective balances within a blockchain context, integrating with a MongoDB backend to offer robust data handling capabilities. Their implementation supports essential features such as transaction updates, balance queries, and data integrity checks, which are vital for the accurate and reliable operation of blockchain financial systems.