# RewardCycleResolver Classes

## Namespace

`MatrixEngine.Core.Engine`

## Overview

The `RewardCycleResolver` suite, housed within the `MatrixEngine.Core.Engine` namespace, is engineered to manage the computation of reward cycles in a blockchain environment, specifically tailored to handle era-based reward distributions. These classes are crucial for ensuring accurate and timely reward calculations post-era closures.

## Interfaces and Classes

### IRewardCycleResolver Interface

This interface outlines the necessary functionalities to identify and compute the reward cycles awaiting calculation, ensuring that all network participants receive their due rewards promptly.

#### Methods

1. **GetToBeCalculatedCycles**
   - **Purpose**: Fetches a list of reward cycles that are ready but not yet calculated, assuming all relevant era data are available.
   - **Returns**: A task that resolves to a list of `RewardCycle` objects needing calculation, or null if an error occurs.

### RewardCycleResolver Class

Implements `IRewardCycleResolver` and is tasked with calculating reward cycles by integrating services that manage eras and reward cycles.

#### Constructor

- **Parameters**:
  - `IEraService eraService`: Provides era-related data.
  - `IRewardCycleService rewardCycleService`: Manages reward cycle data.

#### Methods

1. **GetToBeCalculatedCycles**
   - Retrieves due reward cycles by assessing the gap between the latest finished era and the current reward cycle, ensuring robustness in reward calculation through exception handling.
   - **Implementation Details**:
     - Determines the number of reward cycles due based on the latest data.
     - Computes start and end indexes for each cycle and fetches corresponding block numbers.
     - Returns a list of reward cycles with detailed era indexes and block ranges.

2. **CalculateNotFinishedRewardCycles**
   - **Purpose**: Computes details of pending reward cycles based on the start index of the current cycle and the index of the latest finished era.
   - **Parameters**:
     - `int currentCycleStartEraIndex`: Start index of the current reward cycle.
     - `int latestFinishedEraIndex`: Index of the latest finished era.
     - `int cycleNumbers`: Number of cycles requiring calculation.
   - **Returns**: List of `RewardCycle` objects with detailed information for processing.

3. **CalculateCycleNumbers**
   - **Purpose**: Determines the number of reward cycles needing processing by analyzing the difference between the latest finished era index and the current cycle start era index.
   - **Parameters**:
     - `int latestFinishedEraIndex`: Index of the latest finished era.
     - `int currentCycleStartEraIndex`: Start index of the current reward cycle.
   - **Returns**: Count of cycles that exceed the threshold and need calculation.

## Usage Examples

### Example: Calculating Reward Cycles in a Blockchain Application

This example illustrates how to utilize the `RewardCycleResolver` to identify and calculate pending reward cycles within a blockchain application, an essential task for systems where timely reward updates are crucial.

```csharp
// Assume services are properly instantiated.
IEraService eraService = new EraServiceImplementation();
IRewardCycleService rewardCycleService = new RewardCycleServiceImplementation();

// Instantiate RewardCycleResolver.
RewardCycleResolver resolver = new RewardCycleResolver(eraService, rewardCycleService);

// Fetch reward cycles needing calculation.
List<RewardCycle>? pendingCycles = await resolver.GetToBeCalculatedCycles();

// Output pending cycles.
if (pendingCycles != null && pendingCycles.Count > 0)
{
    Console.WriteLine("Pending Reward Cycles to be Calculated:");
    foreach (var cycle in pendingCycles)
    {
        Console.WriteLine($"Start Era: {cycle.StartEraIndex}, End Era: {cycle.EndEraIndex}, Start Block: {cycle.StartBlock}, End Block: {cycle.EndBlock}");
    }
}
else
{
    Console.WriteLine("No pending reward cycles need calculation at this time.");
}
