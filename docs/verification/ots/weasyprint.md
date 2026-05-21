## WeasyPrint Verification

This document provides the verification evidence for the WeasyPrint OTS software item. Requirements
for this OTS item are defined in the WeasyPrint OTS Software Requirements document.

### Required Functionality

DemaConsulting.WeasyPrintTool converts HTML documents to PDF as part of the documentation build
pipeline. FileAssert validates that each generated PDF file exists, has a non-trivial size, contains
at least one page, and includes expected document content in the rendered text. Passing FileAssert
assertions for each document type proves WeasyPrint executed correctly and produced meaningful
output.

### Qualification Evidence

WeasyPrint is verified by self-validation evidence from the CI pipeline. Each scenario is a
FileAssert assertion that runs after WeasyPrint converts a specific HTML document to PDF. A passing
pipeline run for all scenarios constitutes evidence that the requirement is satisfied.

### Regression Approach

When this OTS dependency is updated, the full CI pipeline is re-executed. All test scenarios must
continue to pass before the update is accepted.

### Test Scenarios

#### WeasyPrint_BuildNotesPdf

**Scenario**: FileAssert asserts the build-notes PDF file exists, is non-trivially sized, contains
at least one page, and includes expected document content.

**Expected**: FileAssert exits 0 for the build-notes PDF document.

**Requirement coverage**: `DictionaryMark-OTS-WeasyPrint`.

#### WeasyPrint_CodeQualityPdf

**Scenario**: FileAssert asserts the code-quality PDF file exists, is non-trivially sized, contains
at least one page, and includes expected document content.

**Expected**: FileAssert exits 0 for the code-quality PDF document.

**Requirement coverage**: `DictionaryMark-OTS-WeasyPrint`.

#### WeasyPrint_ReviewPlanPdf

**Scenario**: FileAssert asserts the review plan PDF file exists, is non-trivially sized, contains
at least one page, and includes expected document content.

**Expected**: FileAssert exits 0 for the review plan PDF document.

**Requirement coverage**: `DictionaryMark-OTS-WeasyPrint`.

#### WeasyPrint_ReviewReportPdf

**Scenario**: FileAssert asserts the review report PDF file exists, is non-trivially sized, contains
at least one page, and includes expected document content.

**Expected**: FileAssert exits 0 for the review report PDF document.

**Requirement coverage**: `DictionaryMark-OTS-WeasyPrint`.

#### WeasyPrint_DesignPdf

**Scenario**: FileAssert asserts the design document PDF file exists, is non-trivially sized,
contains at least one page, and includes expected document content.

**Expected**: FileAssert exits 0 for the design document PDF.

**Requirement coverage**: `DictionaryMark-OTS-WeasyPrint`.

#### WeasyPrint_VerificationPdf

**Scenario**: FileAssert asserts the verification PDF file exists, is non-trivially sized, contains
at least one page, and includes expected verification document content.

**Expected**: FileAssert exits 0 for the verification PDF.

**Requirement coverage**: `DictionaryMark-OTS-WeasyPrint`.

#### WeasyPrint_UserGuidePdf

**Scenario**: FileAssert asserts the user guide PDF file exists, is non-trivially sized, contains
at least one page, and includes expected document content.

**Expected**: FileAssert exits 0 for the user guide PDF document.

**Requirement coverage**: `DictionaryMark-OTS-WeasyPrint`.

### Requirements Coverage

- **`DictionaryMark-OTS-WeasyPrint`**: WeasyPrint_BuildNotesPdf, WeasyPrint_CodeQualityPdf,
  WeasyPrint_ReviewPlanPdf, WeasyPrint_ReviewReportPdf, WeasyPrint_DesignPdf,
  WeasyPrint_VerificationPdf, WeasyPrint_UserGuidePdf

### Suitability Conclusion

Based on the evidence above, WeasyPrint is considered suitable for use in the DictionaryMark CI pipeline.
