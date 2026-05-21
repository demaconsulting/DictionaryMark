# Off-the-Shelf (OTS) Items

This document describes the verification approach for Off-the-Shelf (OTS) software items
used by DictionaryMark.

## Verification Strategy

OTS items are verified by exercising their required functionality within the DictionaryMark
CI pipeline. Each OTS item has a dedicated requirements file under `docs/reqstream/ots/` and
a verification document under `docs/verification/ots/`. Verification evidence is drawn from
CI pipeline execution logs, self-validation output, and artifact assertions.

## OTS Items

The following OTS items are covered:

- BuildMark: `docs/verification/ots/buildmark.md`
- DemaConsulting.TestResults: `docs/verification/ots/test-results.md`
- FileAssert: `docs/verification/ots/fileassert.md`
- Microsoft.Extensions.FileSystemGlobbing: `docs/verification/ots/file-system-globbing.md`
- Pandoc: `docs/verification/ots/pandoc.md`
- ReqStream: `docs/verification/ots/reqstream.md`
- ReviewMark: `docs/verification/ots/reviewmark.md`
- SarifMark: `docs/verification/ots/sarifmark.md`
- SonarMark: `docs/verification/ots/sonarmark.md`
- VersionMark: `docs/verification/ots/versionmark.md`
- WeasyPrint: `docs/verification/ots/weasyprint.md`
- xUnit: `docs/verification/ots/xunit.md`
- YamlDotNet: `docs/verification/ots/yaml-dot-net.md`

## Qualification Evidence

Evidence is collected for each OTS item through one or more of the following mechanisms:

- **Self-validation TRX results**: Tools that ship with a built-in self-validation suite
  (BuildMark, FileAssert, ReqStream, ReviewMark, SarifMark, SonarMark, VersionMark, and
  WeasyPrint) are run with `--validate --results <trx-file>` in CI. The resulting TRX files
  are collected as CI artifacts and consumed by ReqStream to provide requirements coverage
  evidence.
- **Transitive pipeline evidence**: The CI pipeline chains tool outputs through downstream
  validators (Pandoc → FileAssert → WeasyPrint → FileAssert → ReqStream). A passing pipeline
  run constitutes transitive evidence that each tool in the chain produced correct output at
  every stage.
- **xUnit test-run results**: xUnit is verified by self-validation evidence embedded in the
  CI test run itself; every test discovered, executed, and reported by xUnit provides evidence
  that the framework is functioning correctly.

## Regression Approach

On an OTS version upgrade the following steps are performed:

1. The CI pipeline is re-executed with the new version. All self-validation TRX files are
   reviewed for regressions against the previously passing baseline.
2. The transitive pipeline evidence chain (Pandoc → FileAssert → WeasyPrint → FileAssert →
   ReqStream) is verified to remain passing end-to-end.
3. The release notes for the upgraded tool are reviewed; new or changed features that affect
   documented requirements trigger a requirements review before the upgrade is accepted.
4. The per-item verification evidence documented in `docs/verification/ots/` is re-confirmed
   against the new version's output.
