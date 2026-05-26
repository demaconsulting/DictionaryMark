## SarifMark

### Verification Approach

DemaConsulting.SarifMark reads the SARIF output produced by CodeQL code scanning and renders it as
a human-readable markdown document included in the release artifacts. SarifMark is verified by two
complementary layers of evidence. First, the CI pipeline runs
`sarifmark --validate --results artifacts/sarifmark-self-validation.trx`, exercising SarifMark's
built-in self-validation suite against mock SARIF data and recording results for ReqStream. Second,
the pipeline invokes SarifMark with the CodeQL SARIF output to generate
`docs/code_quality/generated/codeql-quality.md`. Pandoc converts this file to HTML; WeasyPrint
renders the result to PDF; and FileAssert asserts the PDF contains expected content. A CI build
failure at any step is evidence that SarifMark did not produce the required output. When this OTS
dependency is updated, the full CI pipeline is re-executed and all test scenarios must continue to
pass before the update is accepted.

### Test Scenarios

**SarifMark_SarifReading**: SarifMark self-validation reads a mock SARIF results file, verifying
that SarifMark correctly reads and parses SARIF content. SarifMark is expected to exit 0 and
successfully read the SARIF content.
This scenario is tested by `SarifMark_SarifReading`.

**SarifMark_MarkdownReportGeneration**: SarifMark self-validation renders the mock SARIF input as a
markdown report, verifying the report generation feature. SarifMark is expected to exit 0 and
produce a non-empty markdown report.
This scenario is tested by `SarifMark_MarkdownReportGeneration`.

**SarifMark_Enforcement**: SarifMark self-validation processes a SARIF file containing known issues
in enforcement mode, verifying that SarifMark correctly signals a non-zero exit code when issues
are found. SarifMark is expected to return a non-zero exit code when issues are present.
This scenario is tested by `SarifMark_Enforcement`.
