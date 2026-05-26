## DemaConsulting.TestResults

DemaConsulting.TestResults is an OTS package produced by DemaConsulting that provides structured
test result collection and serialization to TRX and JUnit XML formats. DictionaryMark uses it
exclusively in `Validation` to record and export self-validation results.

### Purpose

`DemaConsulting.TestResults` is used exclusively in `Validation` to accumulate per-test outcomes
during the self-validation run and to serialize the results to an output file when the user
supplies `--results <file>`. It provides the data model (`TestResults`, `TestResult`) and the
serializers (`TrxSerializer`, `JUnitSerializer`) required to produce standard test result files
that CI pipelines and test dashboards can consume.

### Features Used

DictionaryMark uses the following APIs from the `DemaConsulting.TestResults` and
`DemaConsulting.TestResults.IO` namespaces:

- **`DemaConsulting.TestResults.TestResults`** — container for the full set of self-validation results
- **`TestResults.Name`** — set to `"DictionaryMark Self-Validation"` to label the result set
- **`TestResults.Results.Add(TestResult)`** — records the outcome of each individual self-test
- **`DemaConsulting.TestResults.TestResult`** — represents the outcome of a single named test;
  properties used: `Name`, `ClassName`, `CodeBase`, `Outcome`, `Duration`
- **`DemaConsulting.TestResults.TestOutcome`** — enum with `Passed` and `Failed` values used to
  set `TestResult.Outcome`
- **`TestResults.IO.TrxSerializer`** — serializes the result set to a TRX-format string when the
  output path extension is `.trx`
- **`TestResults.IO.JUnitSerializer`** — serializes the result set to a JUnit XML-format string
  when the output path extension is `.xml`

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
// ... additional tests ...
```

Each private test method creates a `TestResult`, sets `test.Outcome` to `TestOutcome.Passed` or
`TestOutcome.Failed`, then delegates to `FinalizeTestResult`, which records the elapsed duration
and appends the entry to the collection:

```csharp
test.Duration = DateTime.UtcNow - startTime;
testResults.Results.Add(test);
```

After all tests complete, `WriteResultsFile` determines the appropriate serializer by inspecting
the lowercased file extension of `context.ResultsFile`. Each serializer returns a formatted string
that is then written to disk via `File.WriteAllText`:

```csharp
var extension = Path.GetExtension(context.ResultsFile).ToLowerInvariant();
if (extension == ".trx")        content = TrxSerializer.Serialize(testResults);
else if (extension == ".xml")   content = JUnitSerializer.Serialize(testResults);
File.WriteAllText(context.ResultsFile, content);
```

### Error Handling

`WriteResultsFile` wraps all serialization and file-write operations in a `try/catch (Exception)`
block. Serialization failures from `TrxSerializer` or `JUnitSerializer` and file-system errors
from `File.WriteAllText` are both caught within `WriteResultsFile`; the exception message is
forwarded to `context.WriteError`, which sets the exit code to 1, and no exception propagates to
`Validation.Run`. An unsupported file extension causes `WriteResultsFile` to call
`context.WriteError` with an appropriate message without entering the try/catch path.

### Design Constraints

- `TrxSerializer` and `JUnitSerializer` are consumed from `DemaConsulting.TestResults.IO`; the
  exact serialization format is determined by the advertised contract of that package, not
  reimplemented locally.
- The internal design of `DemaConsulting.TestResults` is not replicated here; only the advertised
  public API is referenced.
