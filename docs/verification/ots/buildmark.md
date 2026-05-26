## BuildMark

### Verification Approach

DemaConsulting.BuildMark queries the GitHub API to capture workflow run details and renders them as
a markdown build-notes document included in the release artifacts. BuildMark is verified by two
complementary layers of evidence. First, the CI pipeline runs
`buildmark --validate --results artifacts/buildmark-self-validation.trx`, exercising BuildMark's
built-in self-validation suite against mock data and recording results for ReqStream. Second, the
pipeline invokes BuildMark with live GitHub Actions metadata to generate
`docs/build_notes/generated/build_notes.md`. Pandoc converts this file to HTML; WeasyPrint renders
the HTML to a PDF artifact; and FileAssert asserts the PDF exists, has content, and contains
expected text. A CI build failure at any step is evidence that BuildMark did not produce the
required output. When this OTS dependency is updated, the full CI pipeline is re-executed and all
test scenarios must continue to pass before the update is accepted.

### Test Scenarios

**BuildMark_MarkdownReportGeneration**: A CI pipeline run triggers BuildMark with live GitHub
Actions metadata, verifying that BuildMark produces a build-notes document from real pipeline data.
BuildMark is expected to exit without error and produce a non-empty markdown build-notes document in
the release artifacts. This scenario is verified by a successful CI pipeline execution.

**BuildMark_GitIntegration**: BuildMark self-validation reads version tags and commits from a mock
Git history, verifying that BuildMark correctly processes Git metadata. BuildMark is expected to
exit 0 and correctly read version tags and commit history.
This scenario is tested by `BuildMark_GitIntegration`.

**BuildMark_IssueTracking**: BuildMark self-validation processes mock GitHub issues and pull
requests, verifying that BuildMark correctly fetches and handles issue-tracking data. BuildMark is
expected to exit 0 and correctly fetch and process mock issues and pull requests.
This scenario is tested by `BuildMark_IssueTracking`.

**BuildMark_KnownIssuesReporting**: BuildMark self-validation includes open bugs in the generated
report when requested, verifying the known-issues reporting feature. BuildMark is expected to exit 0
and correctly include known issues in the output.
This scenario is tested by `BuildMark_KnownIssuesReporting`.

**BuildMark_RulesRouting**: BuildMark self-validation assigns mock items to report sections based on
label and type rules, verifying the rules-based routing feature. BuildMark is expected to exit 0 and
correctly route items to the expected sections.
This scenario is tested by `BuildMark_RulesRouting`.
