## ReqStream

### Verification Approach

DemaConsulting.ReqStream processes requirements YAML and TRX test-result files to produce a
requirements report, justifications document, and traceability matrix. When run with `--enforce`,
it exits with a non-zero code if any requirement lacks test evidence, making unproven requirements
a build-breaking condition. ReqStream is verified by two complementary layers of evidence. First,
the CI pipeline runs `reqstream --validate --results artifacts/reqstream-self-validation.trx`,
exercising ReqStream's built-in self-validation suite against internal test requirements and
recording results. Second, the pipeline invokes `reqstream --enforce` with `requirements.yaml` and
all TRX test-evidence files accumulated during the build. If ReqStream failed or produced no
output, the subsequent Pandoc step would fail, breaking the CI build. Additionally, `--enforce`
exits non-zero if any requirement lacks test evidence, which would also fail the build. A passing
CI build proves ReqStream correctly processed the project's real requirements and found complete
test coverage. When this OTS dependency is updated, the full CI pipeline is re-executed and all
test scenarios must continue to pass before the update is accepted.

### Test Scenarios

**ReqStream_RequirementsProcessing**: ReqStream self-validation loads and processes a set of
requirements YAML files, verifying that ReqStream correctly parses requirements input. ReqStream is
expected to correctly load and parse all requirements.
This scenario is tested by `ReqStream_RequirementsProcessing`.

**ReqStream_TraceMatrix**: ReqStream self-validation generates a trace matrix from requirements and
TRX test results, verifying the traceability linking feature. ReqStream is expected to produce a
correctly linked trace matrix.
This scenario is tested by `ReqStream_TraceMatrix`.

**ReqStream_ReportExport**: ReqStream self-validation exports a requirements report to a markdown
file, verifying the report generation feature. ReqStream is expected to produce a correctly
formatted requirements report.
This scenario is tested by `ReqStream_ReportExport`.

**ReqStream_TagsFiltering**: ReqStream self-validation filters requirements by tags, verifying that
tag-based filtering returns only the matching subset of requirements. ReqStream is expected to
return only requirements matching the specified tags.
This scenario is tested by `ReqStream_TagsFiltering`.

**ReqStream_EnforcementMode**: ReqStream self-validation exercises enforcement mode where all
requirements have test coverage, verifying that `--enforce` exits 0 when coverage is complete.
ReqStream is expected to exit 0; it would exit non-zero if any requirement lacked coverage.
This scenario is tested by `ReqStream_EnforcementMode`.

**ReqStream_Lint**: ReqStream self-validation exercises lint mode against a requirements file with
deliberate issues, verifying that ReqStream correctly identifies and reports requirements file
problems. ReqStream is expected to correctly identify and report the issues.
This scenario is tested by `ReqStream_Lint`.
