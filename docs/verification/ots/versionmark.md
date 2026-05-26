## VersionMark

### Verification Approach

DemaConsulting.VersionMark reads version metadata for each dotnet tool used in the pipeline and
writes a versions markdown document included in the release artifacts. VersionMark is verified by
two complementary layers of evidence. First, each CI job runs
`versionmark --validate --results artifacts/versionmark-self-validation.trx`, exercising
VersionMark's built-in self-validation suite and recording results for ReqStream. Second, each CI
job runs `versionmark --capture` to collect tool-version JSON files, and the build-docs job runs
`versionmark --publish` to produce `docs/build_notes/generated/versions.md`. This file is included
in the Build Notes document compiled by Pandoc. If VersionMark failed to produce the versions
document, the Build Notes compilation would be incomplete. WeasyPrint renders the result to PDF and
FileAssert asserts its content. A CI build failure at any step is evidence that VersionMark did not
execute correctly. When this OTS dependency is updated, the full CI pipeline is re-executed and all
test scenarios must continue to pass before the update is accepted.

### Test Scenarios

**VersionMark_CapturesVersions**: VersionMark reads version metadata for each dotnet tool defined
in the pipeline, verifying that VersionMark correctly captures version data. VersionMark is expected
to exit 0 and capture version data for every tool.
This scenario is tested by `VersionMark_CapturesVersions`.

**VersionMark_GeneratesMarkdownReport**: VersionMark writes a versions markdown document to the
release artifacts, verifying the markdown report generation feature. VersionMark is expected to
exit 0 and produce a non-empty versions markdown file.
This scenario is tested by `VersionMark_GeneratesMarkdownReport`.

**VersionMark_LintPassesForValidConfig**: VersionMark self-validation exercises lint mode against a
valid `.versionmark.yaml` config, verifying that lint mode exits cleanly for well-formed
configuration. VersionMark is expected to exit 0 and report no errors.
This scenario is tested by `VersionMark_LintPassesForValidConfig`.

**VersionMark_LintReportsErrorsForInvalidConfig**: VersionMark self-validation exercises lint mode
against a malformed config with deliberate errors, verifying that lint mode correctly identifies
configuration problems. VersionMark is expected to correctly identify and report the configuration
errors.
This scenario is tested by `VersionMark_LintReportsErrorsForInvalidConfig`.
