## Pandoc

### Verification Approach

DemaConsulting.PandocTool converts Markdown source documents to HTML as part of the documentation
build pipeline. Pandoc is verified by self-validation evidence from the CI pipeline. Each scenario
is a FileAssert assertion that runs after Pandoc converts a specific Markdown document to HTML.
FileAssert validates that each generated HTML file exists, has a non-trivial size, contains a valid
HTML title element, and includes expected document content. A passing pipeline run for all scenarios
constitutes evidence that Pandoc executed correctly and produced meaningful output. When this OTS
dependency is updated, the full CI pipeline is re-executed and all test scenarios must continue to
pass before the update is accepted.

### Test Scenarios

**Pandoc_BuildNotesHtml**: FileAssert asserts the build-notes HTML file exists, is non-trivially
sized, contains a valid HTML title element, and includes expected document content, verifying that
Pandoc correctly converted the build-notes Markdown to HTML. FileAssert is expected to exit 0 for
the build-notes HTML document.
This scenario is tested by `Pandoc_BuildNotesHtml`.

**Pandoc_CodeQualityHtml**: FileAssert asserts the code-quality HTML file exists, is non-trivially
sized, contains a valid HTML title element, and includes expected document content, verifying that
Pandoc correctly converted the code-quality Markdown to HTML. FileAssert is expected to exit 0 for
the code-quality HTML document.
This scenario is tested by `Pandoc_CodeQualityHtml`.

**Pandoc_ReviewPlanHtml**: FileAssert asserts the review plan HTML file exists, is non-trivially
sized, contains a valid HTML title element, and includes expected document content, verifying that
Pandoc correctly converted the review plan Markdown to HTML. FileAssert is expected to exit 0 for
the review plan HTML document.
This scenario is tested by `Pandoc_ReviewPlanHtml`.

**Pandoc_ReviewReportHtml**: FileAssert asserts the review report HTML file exists, is non-trivially
sized, contains a valid HTML title element, and includes expected document content, verifying that
Pandoc correctly converted the review report Markdown to HTML. FileAssert is expected to exit 0 for
the review report HTML document.
This scenario is tested by `Pandoc_ReviewReportHtml`.

**Pandoc_DesignHtml**: FileAssert asserts the design document HTML file exists, is non-trivially
sized, contains a valid HTML title element, and includes expected document content, verifying that
Pandoc correctly converted the design Markdown to HTML. FileAssert is expected to exit 0 for the
design document HTML.
This scenario is tested by `Pandoc_DesignHtml`.

**Pandoc_VerificationHtml**: FileAssert asserts the verification HTML file exists, is non-trivially
sized, contains a valid HTML title element, and includes expected verification document content,
verifying that Pandoc correctly converted the verification Markdown to HTML. FileAssert is expected
to exit 0 for the verification document.
This scenario is tested by `Pandoc_VerificationHtml`.

**Pandoc_UserGuideHtml**: FileAssert asserts the user guide HTML file exists, is non-trivially
sized, contains a valid HTML title element, and includes expected document content, verifying that
Pandoc correctly converted the user guide Markdown to HTML. FileAssert is expected to exit 0 for
the user guide HTML document.
This scenario is tested by `Pandoc_UserGuideHtml`.
