## FileAssert Verification

This document provides the verification evidence for the FileAssert OTS software item. Requirements
for this OTS item are defined in the FileAssert OTS Software Requirements document.

### Required Functionality

DemaConsulting.FileAssert validates HTML and PDF documents produced during the build, asserting that
each document exists, has a non-trivial size, is structurally valid, and contains expected content.
It provides OTS evidence for Pandoc and WeasyPrint and independently confirms file assertion is
functioning. Self-validation proves the tool itself is operational before ReqStream consumes the
results.

### Qualification Evidence

FileAssert is verified by two complementary layers of evidence. First, the CI pipeline runs
`fileassert --validate --results artifacts/fileassert-self-validation.trx` after all documents
have been generated, exercising FileAssert's built-in self-validation suite and recording
functional test results for ReqStream.

Second, FileAssert is used throughout the pipeline to validate every generated document before
ReqStream runs - Build Notes, Code Quality, Review Plan, Review Report, Design, Verification,
and User Guide. If FileAssert were non-functional, these validation steps would fail or produce
incorrect results, causing `reqstream --enforce` to report missing test coverage and fail the
build. A passing CI build therefore constitutes transitive evidence that FileAssert correctly
asserted document content at each stage of the pipeline.

### Regression Approach

When this OTS dependency is updated, the full CI pipeline is re-executed. All test scenarios must
continue to pass before the update is accepted.

### Test Scenarios

#### FileAssert_Results

**Scenario**: FileAssert self-validation exercises the `--results` option, generating TRX test
results containing both passing and failing outcomes.

**Expected**: Writes a TRX results file with correctly classified pass and fail entries.

**Requirement coverage**: `DictionaryMark-OTS-FileAssert`.

#### FileAssert_Exists

**Scenario**: FileAssert self-validation exercises a test configuration using a glob pattern to
assert file existence.

**Expected**: Passes when the specified file is present.

**Requirement coverage**: `DictionaryMark-OTS-FileAssert`.

#### FileAssert_Contains

**Scenario**: FileAssert self-validation exercises a test configuration using a `contains` assertion
to verify file content.

**Expected**: Passes when the specified content is present.

**Requirement coverage**: `DictionaryMark-OTS-FileAssert`.

### Requirements Coverage

- **`DictionaryMark-OTS-FileAssert`**: FileAssert_Results, FileAssert_Exists, FileAssert_Contains

### Suitability Conclusion

DemaConsulting.FileAssert is suitable for use in this project. It is actively maintained, provides
assertions matching the test requirements, and is verified through the CI pipeline.
