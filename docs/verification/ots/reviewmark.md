## ReviewMark

### Verification Approach

DemaConsulting.ReviewMark reads the `.reviewmark.yaml` configuration and the review evidence store
to produce a review plan and review report documenting file review coverage and currency. ReviewMark
is verified by two complementary layers of evidence. First, the CI pipeline runs
`reviewmark --validate --results artifacts/reviewmark-self-validation.trx`, exercising ReviewMark's
built-in self-validation suite against test review configurations and recording results for
ReqStream. Second, the pipeline invokes ReviewMark to generate
`docs/code_review_plan/generated/plan.md` and `docs/code_review_report/generated/report.md`. Pandoc
converts each to HTML; if either file were absent or malformed, Pandoc would fail. WeasyPrint
renders both to PDF and FileAssert asserts their content. A CI build failure at any step is evidence
that ReviewMark did not produce the required review documents. When this OTS dependency is updated,
the full CI pipeline is re-executed and all test scenarios must continue to pass before the update
is accepted.

### Test Scenarios

**ReviewMark_ReviewPlanGeneration**: ReviewMark self-validation uses `--definition` and `--plan` to
generate a review plan from a test configuration, verifying the plan generation feature. ReviewMark
is expected to exit 0 and produce a non-empty review plan markdown file.
This scenario is tested by `ReviewMark_ReviewPlanGeneration`.

**ReviewMark_ReviewReportGeneration**: ReviewMark self-validation uses `--definition` and `--report`
to generate a review report from a test configuration and evidence store, verifying the report
generation feature. ReviewMark is expected to exit 0 and produce a non-empty review report.
This scenario is tested by `ReviewMark_ReviewReportGeneration`.

**ReviewMark_IndexScan**: ReviewMark self-validation uses `--index` to scan PDF evidence files and
write an `index.json` catalog, verifying the evidence indexing feature. ReviewMark is expected to
exit 0 and produce a correctly structured `index.json`.
This scenario is tested by `ReviewMark_IndexScan`.

**ReviewMark_WorkingDirectoryOverride**: ReviewMark self-validation uses `--dir` to override the
working directory for file operations, verifying that path resolution is correctly relative to the
specified directory. ReviewMark is expected to exit 0 and resolve paths relative to the specified
directory.
This scenario is tested by `ReviewMark_WorkingDirectoryOverride`.

**ReviewMark_Enforce**: ReviewMark self-validation uses `--enforce` against a configuration with
review issues, verifying that enforcement mode correctly detects and reports problems. ReviewMark is
expected to exit with a non-zero exit code when review issues are present.
This scenario is tested by `ReviewMark_Enforce`.

**ReviewMark_Elaborate**: ReviewMark self-validation uses `--elaborate` to print a Markdown
elaboration of a named review set, verifying the review-set inspection feature. ReviewMark is
expected to exit 0 and print the review-set ID, fingerprint, and file list.
This scenario is tested by `ReviewMark_Elaborate`.

**ReviewMark_Lint**: ReviewMark self-validation uses `--lint` to validate a definition file and
report issues, verifying that structural and semantic problems are detected. ReviewMark is expected
to correctly report structural and semantic issues found in the test definition.
This scenario is tested by `ReviewMark_Lint`.
