---
title: File access
summary: Confine the files that a configuration file may read and write.
aliases:
  - security
  - allow-root
  - roots
  - no-symlinks
  - trust
tags:
  - reference
order: 7
see-also:
  - configuration
  - troubleshooting
examples:
  - name: confined
    kind: source
    language: yaml
    save-as: dictionarymark.yaml
    description: A configuration whose table stays in its folder.
    content: |
      tables:
        - input: terms.yaml
          output: out/terms.md
  - name: untrusted
    kind: source
    language: yaml
    save-as: untrusted.yaml
    description: A configuration that reads a file outside its folder.
    content: |
      tables:
        - input: ../outside.yaml
          output: out.md
  - name: allowed
    kind: command
    language: shell
    description: Allow only the current folder. The table is written.
    content: '{{tool}} --config dictionarymark.yaml --allow-root .'
  - name: denied
    kind: command
    language: shell
    description: The input outside the folder is refused. Exits 1.
    content: '{{tool}} --config untrusted.yaml --allow-root .'
    exit: 1
    expect: 'file-access-denied'
  - name: bad-mode
    kind: command
    language: shell
    description: An unknown mode is an error. Exits 1.
    content: '{{tool}} --config dictionarymark.yaml --file-access root'
    exit: 1
    expect: 'is not a value of --file-access'
  - name: any-with-root
    kind: command
    language: shell
    description: The any mode does not take --allow-root. Exits 1.
    content: '{{tool}} --config dictionarymark.yaml --allow-root . --file-access any'
    exit: 1
    expect: 'cannot be used together'
---
The command line is trusted, and the content of a configuration file is
not, because it may come from a pull request or a shared repository.
The file-access mode judges every input, including every match of a
glob pattern, and every output of the tables of a configuration file.
It also judges a relative `--input` or `--output` that the `root` key
of the file moves, when no `--root` is given. `--config`, `--log`,
`--results`, absolute command-line paths, and every command-line path
when `--root` is given are not judged.

| Mode | The tables may read and write |
| --- | --- |
| `any` (default) | Every regular local file. |
| `roots` | Only files inside the allowed folders. |
| `no-symlinks` | No file reached through a symbolic link or junction. |
| `roots,no-symlinks` | Both rules, in either order. |

**Options.** Choose the mode with `--file-access <mode>`, in any letter
case; when it is repeated, the last value wins. Name an allowed folder
with `--allow-root <dir>`, which may be repeated, implies `roots`, and
must exist; a relative folder is taken from the current folder. With
`roots` and no `--allow-root`, the current folder and the folder of the
configuration file are allowed. `--allow-root` with `any` or
`no-symlinks` is an error. These mistakes are reported before any input
is read and are always printed.

**Denied files.** A denied input or output fails its table with
`file-access-denied` and writes nothing; the other tables still run, and
the exit code is 1. A file without a wildcard is judged before it is
looked up, so the command that [[#denied]] prints refuses it
whether or not it exists.

**Always refused.** In every mode, including `any`, the tables may not
use network paths, device paths, alternate data streams, control
characters, or anything but a regular file, and an input may not be
larger than 16 MiB.

**`no-symlinks`** trusts the folder of the configuration file and the
folders above it, and refuses a link below it.

**Environment variables.** Every mode except `any` refuses `${VAR}` in
the `root`, `input`, and `output` of the file, so that the file cannot
read the environment of the process.

**Known limits.** A hard link is not detected, the check of an output
is not safe against a link swapped in before the write, and a glob
pattern can reveal the names of files outside the allowed folders,
though never their content.
