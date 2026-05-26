# Installation

## Prerequisites

DictionaryMark requires the .NET SDK 8.0, 9.0, or 10.0. It runs on Windows, Linux, and macOS.

Download the .NET SDK from <https://dotnet.microsoft.com/download>.

## Global Installation

Install DictionaryMark as a global .NET tool for system-wide use:

```bash
dotnet tool install --global DemaConsulting.DictionaryMark
```

Verify the installation:

```bash
dictionarymark --version
```

## Local Installation

Install DictionaryMark as a local tool within a project (recommended for team projects):

```bash
dotnet new tool-manifest  # if you don't have a tool manifest already
dotnet tool install DemaConsulting.DictionaryMark
```

Run the local tool:

```bash
dotnet dictionarymark --version
```

## Updating

To update to the latest version:

```bash
# Global installation
dotnet tool update --global DemaConsulting.DictionaryMark

# Local installation
dotnet tool update DemaConsulting.DictionaryMark
```

## Compatibility

| Component | Version | Status    |
| :-------- | :------ | :-------- |
| .NET SDK  | 8.0     | Supported |
| .NET SDK  | 9.0     | Supported |
| .NET SDK  | 10.0    | Supported |
| OS        | Windows | Supported |
| OS        | Linux   | Supported |
| OS        | macOS   | Supported |
