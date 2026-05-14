# DictionaryMark System Verification

This document describes system-level verification for DictionaryMark.

## Verification Approach

System requirements are verified through integration tests in `IntegrationTests.cs`
that invoke the compiled DLL via the `dotnet` command.

## Requirements Coverage

| Requirement                    | Test                                                       |
|--------------------------------|------------------------------------------------------------|
| DictionaryMark-System-Version  | DictionaryMark_VersionFlag_Provided_OutputsVersion         |
| DictionaryMark-System-Help     | DictionaryMark_HelpFlag_Provided_OutputsUsageInformation   |
| DictionaryMark-System-Validate | DictionaryMark_ValidateFlag_Provided_RunsValidation        |
| DictionaryMark-System-Silent   | DictionaryMark_SilentFlag_Provided_SuppressesOutput        |
| DictionaryMark-System-Log      | DictionaryMark_LogFlag_Provided_WritesOutputToFile         |
