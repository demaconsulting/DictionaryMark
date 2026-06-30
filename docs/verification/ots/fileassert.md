## FileAssert

### Verification Approach

DemaConsulting.FileAssert validates HTML and PDF documents produced during the build, asserting that
each document exists, has a non-trivial size, is structurally valid, and contains expected content.
FileAssert is verified by two complementary layers of evidence. First, the CI pipeline runs
`fileassert --validate --results artifacts/fileassert-self-validation.trx` after all documents have
been generated, exercising FileAssert's built-in self-validation suite and recording functional test
results for ReqStream. Second, FileAssert is used throughout the pipeline to validate every
generated document before ReqStream runs — Build Notes, Code Quality, Review Plan, Review Report,
Design, Verification, and User Guide. If FileAssert were non-functional, these validation steps
would fail or produce incorrect results, causing `reqstream --enforce` to report missing test
coverage and fail the build. A passing CI build therefore constitutes transitive evidence that
FileAssert correctly asserted document content at each pipeline stage. When this OTS dependency is
updated, the full CI pipeline is re-executed and all test scenarios must continue to pass before the
update is accepted.

### Test Scenarios

**FileAssert_Results**: FileAssert self-validation exercises the `--results` option, generating TRX
test results containing both passing and failing outcomes, verifying that FileAssert correctly
produces test result files. A TRX results file with correctly classified pass and fail entries is
expected.
This scenario is tested by `FileAssert_Results`.

**FileAssert_File**: FileAssert self-validation exercises a test configuration using a glob
pattern to assert file existence, verifying that FileAssert correctly detects present files. The
assertion is expected to pass when the specified file is present.
This scenario is tested by `FileAssert_File`.

**FileAssert_Text**: FileAssert self-validation exercises a test configuration using a
`contains` assertion to verify file content, verifying that FileAssert correctly checks file
content. The assertion is expected to pass when the specified content is present.
This scenario is tested by `FileAssert_Text`.
