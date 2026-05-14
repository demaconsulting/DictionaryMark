# Utilities Subsystem

The `Utilities` subsystem provides shared utility functions for the Template DotNet Tool.
It supplies reusable, independently testable helpers that are consumed by other subsystems.

## Overview

The `Utilities` subsystem contains general-purpose helpers that do not belong to any
specific feature subsystem. Its primary responsibility is safe file-path manipulation,
protecting callers from path-traversal vulnerabilities when constructing paths from
external inputs.

## Units

The `Utilities` subsystem contains the following software unit:

| Unit          | File                       | Responsibility                              |
|---------------|----------------------------|---------------------------------------------|
| `PathHelpers` | `Utilities/PathHelpers.cs` | Safe path combination and traversal checks. |

## Interfaces

The `Utilities` subsystem exposes the following outbound interface to the rest of the tool:

- **`PathHelpers.SafePathCombine`**: Combines two path segments, rejecting traversal sequences
  and absolute path overrides.

`SafePathCombine` throws `ArgumentException` when the combined path escapes the base
directory, and `ArgumentNullException` for null inputs. `NotSupportedException`
(unsupported path format) and `PathTooLongException` (path exceeds system limit) may
propagate from the underlying BCL path operations.

## Interactions

`PathHelpers` has no dependencies on other tool units or subsystems. It uses only .NET base
class library types (`Path`, `ArgumentNullException`).
