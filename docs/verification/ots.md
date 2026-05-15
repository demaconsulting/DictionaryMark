# Off-the-Shelf (OTS) Items

This document describes the verification approach for Off-the-Shelf (OTS) software items
used by DictionaryMark.

## Verification Approach

OTS items are verified by exercising their required functionality within the DictionaryMark
CI pipeline. Each OTS item has a dedicated requirements file under `docs/reqstream/ots/` and
a verification document under `docs/verification/ots/`. Verification evidence is drawn from
CI pipeline execution logs, self-validation output, and artifact assertions.

## OTS Items

The following OTS items are covered:

- BuildMark: `docs/verification/ots/buildmark.md`
- FileAssert: `docs/verification/ots/fileassert.md`
- Pandoc: `docs/verification/ots/pandoc.md`
- ReqStream: `docs/verification/ots/reqstream.md`
- ReviewMark: `docs/verification/ots/reviewmark.md`
- SarifMark: `docs/verification/ots/sarifmark.md`
- SonarMark: `docs/verification/ots/sonarmark.md`
- VersionMark: `docs/verification/ots/versionmark.md`
- WeasyPrint: `docs/verification/ots/weasyprint.md`
- xUnit: `docs/verification/ots/xunit.md`
