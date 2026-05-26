## SonarMark

### Verification Approach

DemaConsulting.SonarMark retrieves quality-gate and metrics data from SonarCloud and renders it as
a markdown document included in the release artifacts. SonarMark is verified by two complementary
layers of evidence. First, the CI pipeline runs
`sonarmark --validate --results artifacts/sonarmark-self-validation.trx`, exercising SonarMark's
built-in self-validation suite against mock SonarCloud data and recording results for ReqStream.
Second, the pipeline invokes SonarMark against the live SonarCloud API to generate
`docs/code_quality/generated/sonar-quality.md`. Pandoc converts this file to HTML; WeasyPrint
renders the result to PDF; and FileAssert asserts the PDF contains expected content. A CI build
failure at any step is evidence that SonarMark did not retrieve and render quality data correctly.
When this OTS dependency is updated, the full CI pipeline is re-executed and all test scenarios
must continue to pass before the update is accepted.

### Test Scenarios

**SonarMark_QualityGateRetrieval**: SonarMark self-validation exercises quality-gate retrieval
against mock SonarCloud data, verifying that SonarMark correctly retrieves quality-gate data.
SonarMark is expected to exit 0 and retrieve quality-gate data.
This scenario is tested by `SonarMark_QualityGateRetrieval`.

**SonarMark_IssuesRetrieval**: SonarMark self-validation exercises issues retrieval against mock
SonarCloud data, verifying that SonarMark correctly retrieves issue data. SonarMark is expected to
exit 0 and retrieve issue data.
This scenario is tested by `SonarMark_IssuesRetrieval`.

**SonarMark_HotSpotsRetrieval**: SonarMark self-validation exercises hot-spot retrieval against mock
SonarCloud data, verifying that SonarMark correctly retrieves security hot-spot data. SonarMark is
expected to exit 0 and retrieve hot-spot data.
This scenario is tested by `SonarMark_HotSpotsRetrieval`.

**SonarMark_MarkdownReportGeneration**: SonarMark self-validation exercises markdown report
generation against mock SonarCloud data, verifying that SonarMark produces a correct quality report.
SonarMark is expected to exit 0 and produce a non-empty markdown quality report.
This scenario is tested by `SonarMark_MarkdownReportGeneration`.
