# EffectiveBalanceResolver Classes

## Namespace

`MatrixEngine.Core.Engine`

## Overview

The `EffectiveBalanceResolver` classes are integral to a module designed to compute the effective balances and rewards for accounts based on their historical balance changes. These calculations are specifically tailored for environments involving staking where balances and corresponding rewards can fluctuously be influenced by factors such as the type of staker and the number of eras they actively participated in.

## Interfaces and Classes

### IEffectiveBalanceResolver Interface

This interface outlines the methods required to accurately calculate effective balances for one or multiple accounts, based on balance changes observed over time.

#### Methods

1. **CalculateOneAccountEffectiveBalance**
   - **Purpose**: Determines the effective balances for a given account by evaluating its balance changes throughout various eras.
   - **Parameters**:
     - `string account`: Identifier for the account in question.
     - `List<BalanceChangeModel> balanceChangesForAccount`: A list detailing the balance changes for the specified account.
   - **Returns**: A list of instances of `EffectiveBalanceModel`, each representing the effective balance for different eras and associated details.

2. **CalculateEffectiveBalanceWithPrecisions**
   - **Purpose**: Computes the effective balance taking into consideration the total balance and the effective eras, while adhering to a specified decimal precision.
   - **Parameters**:
     - `decimal balanceEffectiveEras`: The total number of effective eras.
     - `BigInteger balanceTotalBalance`: The cumulative balance across the specified block range.
     - `int decimalPlaces`: Optional. Specifies the number of decimal places to use in the calculation (default is 2).
   - **Returns**: A decimal representing the calculated effective balance.

3. **CalculateEffectiveBalances**
   - **Purpose**: Calculates effective balances for a collection of accounts, provided their respective balance changes.
   - **Parameters**:
     - `Dictionary<string, List<BalanceChangeModel>> balanceChanges`: A dictionary where each key corresponds to an account identifier and each value is a list of balance changes for that account.
   - **Returns**: A dictionary where each key is an account identifier and the value is a list of `EffectiveBalanceModel` detailing the effective balances.

### EffectiveBalanceResolver Class

Implements the `IEffectiveBalanceResolver` interface.

#### Constants

- **ErasInCycle**: A constant integer value set to 90, signifying the total number of eras in one staking cycle.

#### Methods

1. **CalculateEffectiveBalances**
   - This method iterates through each account in the provided dictionary, computes the effective balances for each, and compiles the results into a new dictionary.
   - **Parameters**: Inherits from the interface.
   - **Returns**: Inherits from the interface.

2. **CalculateOneAccountEffectiveBalance**
   - This method processes each balance change for a specific account to compute effective balances, determine staker rates, and calculate potential rewards.
   - **Parameters**: Inherits from the interface.
   - **Returns**: Inherits from the interface.

3. **CalculateEffectiveBalanceWithPrecisions**
   - Computes the effective balance using fixed decimal places based on the specified number of eras within a cycle and the balance for these eras.
   - **Parameters**: Inherits from the interface.
   - **Returns**: Inherits from the interface.

#### Private Methods

1. **GetStakerRate**
   - **Purpose**: Fetches the applicable rate for a staker based on their designated type (e.g., Validator, Nominator).
   - **Parameters**:
     - `string type`: Specifies the staker type.
   - **Returns**: The rate applicable to the staker type, expressed as a decimal.

## Usage Examples

### Calculating Effective Balances for Multiple Accounts

```csharp
// Initialize balance change details for multiple accounts
Dictionary<string, List<BalanceChangeModel>> balanceChanges = new Dictionary<string, List<BalanceChangeModel>>
{
    {"account1", new List<BalanceChangeModel>
        {
            new BalanceChangeModel { ... },
            new BalanceChangeModel { ... }
        }
    },
    {"account2", new List<BalanceChangeModel>
        {
            new BalanceChangeModel { ... },
            new BalanceChangeModel { ... }
        }
    }
};

// Create an instance of EffectiveBalanceResolver
IEffectiveBalanceResolver resolver = new EffectiveBalanceResolver();

// Calculate effective balances for all accounts
Dictionary<string, List<EffectiveBalanceModel>> effectiveBalances = resolver.CalculateEffectiveBalances(balanceChanges);

// Output the results
foreach (var account in effectiveBalances.Keys)
{
    Console.WriteLine($"Account: {account}");
    foreach (var balance in effectiveBalances[account])
    {
        Console.WriteLine($"Effective Balance: {balance.EffectiveBalance}, Reward: {balance.Reward}");
    }
}
```
