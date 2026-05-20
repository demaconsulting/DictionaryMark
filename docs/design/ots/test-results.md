## DemaConsulting.TestResults Integration Design

DemaConsulting.TestResults is a shared package produced by the DemaConsulting program that
provides structured test result collection and serialization to TRX and JUnit XML formats.
DictionaryMark uses it in the SelfTest subsystem to record and export self-validation results.

### Purpose

`DemaConsulting.TestResults` is used exclusively in `Validation` to accumulate per-test outcomes
during the self-validation run and to serialize the results to an output file when the user
supplies `--results <file>`. It provides the data model (`TestResults`, `TestResult`) and the
serializers (`TrxSerializer`, `JUnitSerializer`) required to produce standard test result files
that CI pipelines and test dashboards can consume.

### Features Used

DictionaryMark uses the following APIs from the `DemaConsulting.TestResults` and
`DemaConsulting.TestResults.IO` namespaces:

| Type / Member                            | Usage                                                                        |
| ---------------------------------------- | ---------------------------------------------------------------------------- |
| `DemaConsulting.TestResults.TestResults` | Container for the full set of self-validation test results.                  |
| `TestResults.Name`                       | Set to `"DictionaryMark Self-Validation"` to label the result set.           |
| `TestResults.Add(TestResult)`            | Records the outcome of each individual self-test.                            |
| `DemaConsulting.TestResults.TestResult`  | Represents the pass/fail outcome of a single named test.                     |
| `TestResults.IO.TrxSerializer`           | Serializes the result set to TRX format when the output path ends in `.trx`. |
| `TestResults.IO.JUnitSerializer`         | Serializes the result set to JUnit XML when the output path ends in `.xml`.  |

Features not used: result filtering, merging multiple result sets, custom serialization formats,
and parallel test scheduling primitives.

### Integration Pattern

`Validation.Run` creates a single `TestResults` instance at the start of the validation run and
passes it to each test method:

```csharp
var testResults = new DemaConsulting.TestResults.TestResults
{
    Name = "DictionaryMark Self-Validation"
};
RunVersionTest(context, testResults);
RunHelpTest(context, testResults);
// ... additional tests ...
```

Each private test method adds one result entry after executing the test scenario:

```csharp
testResults.Add(new TestResult { Name = "...", Passed = true/false, ... });
```

After all tests complete, `WriteResultsFile` selects the appropriate serializer based on the
file extension of `context.ResultsFile` and writes the collected results:

```csharp
if (context.ResultsFile.EndsWith(".trx"))   TrxSerializer.Serialize(testResults, path);
if (context.ResultsFile.EndsWith(".xml"))   JUnitSerializer.Serialize(testResults, path);
```

### Error Handling

- Serialization errors from `TrxSerializer` or `JUnitSerializer` are not explicitly caught
  within `WriteResultsFile`; file-system exceptions propagate to `Validation.Run`, which
  does not catch them either, allowing them to propagate to `Program.Main` where they are
  handled as unexpected exceptions.
- An unsupported file extension (not `.trx` or `.xml`) causes `WriteResultsFile` to call
  `context.WriteError` with an appropriate message, setting the exit code to 1 without
  throwing an exception.

### Design Constraints

- `TrxSerializer` and `JUnitSerializer` are consumed from `DemaConsulting.TestResults.IO`;
  the exact serialization format is determined by the advertised contract of that package,
  not re-implemented locally.
- The consuming repository does not replicate the internal design of `DemaConsulting.TestResults`;
  only the advertised public API is referenced here.
