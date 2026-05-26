## WeasyPrint

### Verification Approach

DemaConsulting.WeasyPrintTool converts HTML documents to PDF as part of the documentation build
pipeline. WeasyPrint is verified by self-validation evidence from the CI pipeline. Each scenario is
a FileAssert assertion that runs after WeasyPrint converts a specific HTML document to PDF.
FileAssert validates that each generated PDF file exists, has a non-trivial size, contains at least
one page, and includes expected document content in the rendered text. A passing pipeline run for
all scenarios constitutes evidence that WeasyPrint executed correctly and produced meaningful output.
When this OTS dependency is updated, the full CI pipeline is re-executed and all test scenarios must
continue to pass before the update is accepted.

### Test Scenarios

**WeasyPrint_BuildNotesPdf**: FileAssert asserts the build-notes PDF file exists, is non-trivially
sized, contains at least one page, and includes expected document content, verifying that WeasyPrint
correctly converted the build-notes HTML to PDF. FileAssert is expected to exit 0 for the
build-notes PDF document.
This scenario is tested by `WeasyPrint_BuildNotesPdf`.

**WeasyPrint_CodeQualityPdf**: FileAssert asserts the code-quality PDF file exists, is non-trivially
sized, contains at least one page, and includes expected document content, verifying that WeasyPrint
correctly converted the code-quality HTML to PDF. FileAssert is expected to exit 0 for the
code-quality PDF document.
This scenario is tested by `WeasyPrint_CodeQualityPdf`.

**WeasyPrint_ReviewPlanPdf**: FileAssert asserts the review plan PDF file exists, is non-trivially
sized, contains at least one page, and includes expected document content, verifying that WeasyPrint
correctly converted the review plan HTML to PDF. FileAssert is expected to exit 0 for the review
plan PDF document.
This scenario is tested by `WeasyPrint_ReviewPlanPdf`.

**WeasyPrint_ReviewReportPdf**: FileAssert asserts the review report PDF file exists, is
non-trivially sized, contains at least one page, and includes expected document content, verifying
that WeasyPrint correctly converted the review report HTML to PDF. FileAssert is expected to exit 0
for the review report PDF document.
This scenario is tested by `WeasyPrint_ReviewReportPdf`.

**WeasyPrint_DesignPdf**: FileAssert asserts the design document PDF file exists, is non-trivially
sized, contains at least one page, and includes expected document content, verifying that WeasyPrint
correctly converted the design HTML to PDF. FileAssert is expected to exit 0 for the design
document PDF.
This scenario is tested by `WeasyPrint_DesignPdf`.

**WeasyPrint_VerificationPdf**: FileAssert asserts the verification PDF file exists, is
non-trivially sized, contains at least one page, and includes expected verification document
content, verifying that WeasyPrint correctly converted the verification HTML to PDF. FileAssert is
expected to exit 0 for the verification PDF.
This scenario is tested by `WeasyPrint_VerificationPdf`.

**WeasyPrint_UserGuidePdf**: FileAssert asserts the user guide PDF file exists, is non-trivially
sized, contains at least one page, and includes expected document content, verifying that WeasyPrint
correctly converted the user guide HTML to PDF. FileAssert is expected to exit 0 for the user guide
PDF document.
This scenario is tested by `WeasyPrint_UserGuidePdf`.
