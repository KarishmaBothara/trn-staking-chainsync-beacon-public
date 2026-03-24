How to run individual integration tests

```bash
# Run TestCase3
dotnet test --filter "TestCase3.Test_Scenario_1" --logger "console;verbosity=verbose"
# Run TestCase4
dotnet test --filter "TestCase4.Test_Scenario_1" --logger "console;verbosity=verbose"
# ...
```

To run all the tests

```bash
cd MatrixEngine.Core.IntegrationTest && dotnet test
```