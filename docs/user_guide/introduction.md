# Introduction

This guide describes how to install, configure, and use DictionaryMark.

## Purpose

DictionaryMark is a .NET command-line tool that reads YAML flat-dictionary files and generates
formatted Markdown output as either bullet lists or tables. It supports glob patterns for processing
multiple input files, detects conflicting definitions across files, and integrates easily into
documentation pipelines.

DictionaryMark includes built-in self-validation to support use in regulated environments where tool
qualification evidence is required. The project itself follows the Continuous Compliance methodology,
ensuring that requirements traceability, quality analysis, and audit documentation are generated
automatically on every CI run.

The workflow below shows how DictionaryMark is used: author YAML dictionary files, run the
`dictionarymark` command from a shell or CI pipeline, and use the generated Markdown bullet list or
table in larger documents, a documentation pipeline, or as a CI gate.

![DictionaryMark workflow: author YAML dictionary files, run dictionarymark with --input, --format and header
options, get a Markdown bullet list or table, then embed it in documents, a docs pipeline or a CI
gate](workflow.svg)

## Scope

This guide covers:

- Prerequisites and installation on Windows, Linux, and macOS
- A complete CLI options reference
- Usage examples for common scenarios, including glob patterns, custom headers, and sort order
- Self-validation for regulated environments

This guide assumes that the .NET SDK 10.0 or 11.0 is installed on the target system.

## References

- [DictionaryMark releases](https://github.com/demaconsulting/DictionaryMark/releases)
- [.NET SDK](https://dotnet.microsoft.com/download)
