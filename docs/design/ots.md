# OTS Dependencies

DictionaryMark depends on three runtime OTS packages: YamlDotNet,
Microsoft.Extensions.FileSystemGlobbing, and DemaConsulting.TestResults. Each is covered by a
dedicated integration design file in the `ots/` sub-folder.

## Selection Criteria

OTS items are selected when they provide well-established, actively maintained implementations of
standard functionality that would be costly to implement and maintain in-house. License
compatibility is required: MIT or Apache 2.0 licenses are preferred, and the license must permit
redistribution as a .NET global tool. Community support is evaluated by checking for active
maintenance, a documented public API, and an established user base. Security is assessed by
verifying that no known unpatched critical vulnerabilities exist at the time of selection.
Maturity is confirmed by a stable public API; breaking changes are handled through version
pinning and Dependabot alerts.

## Version Management Policy

Runtime dependency versions are declared in the project file and managed by Dependabot. Minor and
patch upgrades are merged when CI passes without additional review. Major version upgrades are
reviewed against the OTS integration design before merging, to confirm that the integration
pattern documented here remains accurate. No version numbers are recorded in this document; the
SBOM generated during each build is the authoritative source for currently pinned versions.

## General Integration Approach

All three OTS packages are consumed through their published NuGet APIs without wrapper or adapter
layers, keeping integration code minimal and direct. Each integration point is contained within a
specific unit: YamlDotNet in `YamlDictionaryLoader`, Microsoft.Extensions.FileSystemGlobbing in
`GlobMatcher`, and DemaConsulting.TestResults in `Validation`. This containment means that any
future replacement or upgrade affects at most one unit per OTS item. Exceptions surfaced by OTS
calls are either re-thrown as-is or wrapped in an `InvalidOperationException` with a descriptive
message before propagating to the caller.

## Qualification Strategy

OTS items are qualified through integration tests in the DictionaryMark test suite located in
`test/DemaConsulting.DictionaryMark.Tests/`. These tests exercise each OTS item through the
consuming unit and constitute the primary qualification evidence. The self-validation mode
(`--validate`) further exercises each OTS item in the deployment environment at run time. No
separate OTS qualification test project is maintained.
