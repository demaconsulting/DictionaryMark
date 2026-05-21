# OTS Dependencies

DictionaryMark depends on three runtime OTS packages: YamlDotNet, Microsoft.Extensions.FileSystemGlobbing,
and DemaConsulting.TestResults. Each is covered by a dedicated integration design file in `docs/design/ots/`.

## Selection Criteria

OTS items are selected when they provide well-established, actively maintained implementations of
standard functionality that would be costly to implement and maintain in-house. The following criteria
are applied:

- **License compatibility**: MIT or Apache 2.0 preferred; must permit redistribution as a .NET tool.
- **Community support**: active maintenance, documented API, and an established user base.
- **Security track record**: no known unpatched critical vulnerabilities at the time of selection.
- **Maturity**: stable API; breaking changes handled through version pinning and Dependabot alerts.

## Version Management Policy

Runtime dependency versions are declared in the project file
(`src/DemaConsulting.DictionaryMark/DemaConsulting.DictionaryMark.csproj`) and managed by Dependabot.
Minor and patch upgrades are merged when CI passes. Major version upgrades are reviewed against the
OTS integration design before merging. No version numbers are recorded in this document — current
pinned versions are authoritative in the SBOM generated during each build.

## General Integration Approach

All three OTS packages are consumed through their published NuGet APIs without wrapper or adapter
layers, keeping integration code minimal and direct. The integration points are contained within
specific units (`YamlDictionaryLoader`, `GlobMatcher`, `Validation`) so that any future replacement
or upgrade requires changes in at most one unit per OTS item.

Error handling follows the DictionaryMark convention: exceptions surfaced by OTS calls are either
re-thrown as-is or wrapped in a `InvalidOperationException` with a descriptive message before
propagating to the caller.

## Qualification Strategy

OTS items are qualified through integration tests in the DictionaryMark test suite. The self-validation
mode (`--validate`) further exercises each OTS item in the deployment environment at run time. No
separate OTS qualification test project is maintained; the integration tests in
`test/DemaConsulting.DictionaryMark.Tests/` constitute the primary qualification evidence.
