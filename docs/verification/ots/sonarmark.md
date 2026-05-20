## SonarMark Verification

This document provides the verification evidence for the SonarMark OTS software item. Requirements
for this OTS item are defined in the SonarMark OTS Software Requirements document.

### Required Functionality

DemaConsulting.SonarMark retrieves quality-gate and metrics data from SonarCloud and renders it as
a markdown document included in the release artifacts. It runs in the same CI pipeline that produces
the TRX test results, so a successful pipeline run is evidence that SonarMark executed without
error.

### Qualification Evidence

SonarMark is verified by two complementary layers of evidence. First, the CI pipeline runs
`sonarmark --validate --results artifacts/sonarmark-self-validation.trx`, which exercises
SonarMark's built-in self-validation suite against mock SonarCloud data and records results
for ReqStream.

Second, the pipeline invokes SonarMark against the live SonarCloud API to generate
`docs/code_quality/generated/sonar-quality.md`. Pandoc converts this file (along with the
SarifMark output) to HTML; if the file were absent or malformed, Pandoc would fail. WeasyPrint
renders the result to PDF and FileAssert asserts the PDF contains expected content
(`WeasyPrint_CodeQualityPdf`). A CI build failure at any step is evidence that SonarMark did
not retrieve and render quality data correctly.

### Regression Approach

When this OTS dependency is updated, the full CI pipeline is re-executed. All test scenarios must
continue to pass before the update is accepted.

### Test Scenarios

#### SonarMark_QualityGateRetrieval

**Scenario**: SonarMark queries the SonarCloud API for quality-gate status.

**Expected**: Exits 0 and retrieves quality-gate data.

**Requirement coverage**: `DictionaryMark-OTS-SonarMark`.

#### SonarMark_IssuesRetrieval

**Scenario**: SonarMark queries the SonarCloud API for issues.

**Expected**: Exits 0 and retrieves issues data.

**Requirement coverage**: `DictionaryMark-OTS-SonarMark`.

#### SonarMark_HotSpotsRetrieval

**Scenario**: SonarMark queries the SonarCloud API for hot spots.

**Expected**: Exits 0 and retrieves hot-spots data.

**Requirement coverage**: `DictionaryMark-OTS-SonarMark`.

#### SonarMark_MarkdownReportGeneration

**Scenario**: SonarMark renders quality-gate, issues, and hot-spots data as a markdown report.

**Expected**: Exits 0 and produces a non-empty markdown quality report.

**Requirement coverage**: `DictionaryMark-OTS-SonarMark`.

### Requirements Coverage

- **`DictionaryMark-OTS-SonarMark`**: SonarMark_QualityGateRetrieval, SonarMark_IssuesRetrieval,
  SonarMark_HotSpotsRetrieval, SonarMark_MarkdownReportGeneration

### Suitability Conclusion

Based on the evidence above, SonarMark is considered suitable for use in the DictionaryMark CI pipeline.
