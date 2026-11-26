---
title: Getting started
summary: Install the tool and turn a YAML dictionary file into a Markdown glossary.
aliases:
  - start
  - quickstart
  - examples
  - install
tags:
  - tutorial
order: 1
see-also:
  - dictionary-files
  - output-formats
  - configuration
examples:
  - name: install-local
    kind: snippet
    language: shell
    save-as: install-local.sh
    description: Add the tool to the tool manifest of a repository.
    content: |
      dotnet new tool-manifest
      dotnet tool install --local DemaConsulting.DictionaryMark
  - name: run-in-ci
    kind: snippet
    language: shell
    save-as: run-in-ci.sh
    description: Restore the manifest tools in CI, then run the tool.
    content: |
      dotnet tool restore
      dotnet {{tool}} --input glossary.yaml --format table --output docs/glossary.md
  - name: version
    kind: command
    language: shell
    description: Print the version and exit.
    content: '{{tool}} --version'
  - name: first
    kind: source
    language: yaml
    save-as: glossary.yaml
    description: A dictionary file with three terms.
    content: |
      API: Application Programming Interface
      CLI: Command-Line Interface
      SDK: Software Development Kit
  - name: print
    kind: command
    language: shell
    description: Print the terms as a Markdown bullet list.
    content: '{{tool}} --input glossary.yaml'
    expect: '- **API**: Application Programming Interface'
  - name: write
    kind: command
    language: shell
    description: Write the terms as a table to a Markdown file.
    content: '{{tool}} --input glossary.yaml --format table --section Glossary --output glossary.md'
  - name: topic
    kind: command
    language: shell
    description: Print a help topic, here the dictionary files.
    content: '{{tool}} help dictionary-files'
    expect: 'The root of the file is a YAML mapping'
---
{{tool}} {{version}} is a .NET tool.
It reads YAML dictionary files of terms and writes them as a Markdown
bullet list or table, ready to include in a document.

1. **Install it.** The tool needs the .NET 10 SDK or later. In CI, set it
   up first when the runner has an older .NET, for example with
   `actions/setup-dotnet` and `dotnet-version: 10.x`.
   For a repository or CI, install it as a local tool. Run the commands
   that [[#install-local]] prints once, without
   `dotnet new tool-manifest` when `.config/dotnet-tools.json` exists, and
   commit that file. In CI, run `dotnet tool restore` first, then run the
   tool as `dotnet {{tool}}`, as in the commands that
   [[#run-in-ci]] prints. For one machine, install it globally and
   run it as `{{tool}}`:

   ```bash
   dotnet tool install -g DemaConsulting.DictionaryMark
   ```

2. **Check it.** The command that [[#version]] prints shows the version.
   `{{tool}} --validate` runs the built-in self-tests; see
   [[self-validation]].
3. **Write a dictionary.** Save the file that [[#first]] prints
   as `glossary.yaml`. Each line is a term and its definition;
   see [[dictionary-files]].
4. **Print it.** The command that [[#print]] prints writes the
   terms as a bullet list to the console, after the banner of the tool.
5. **Write it.** The command that [[#write]] prints writes a
   table with the heading `# Glossary` to `glossary.md`. The file holds
   only the Markdown, without the banner; see [[output-formats]].

To generate several tables in one run, keep the options and the table
jobs in a configuration file; see [[configuration]].

The tool reads only the files that the options or the configuration file
name, and writes only the outputs, the `--log` file, and the `--results`
file; `--validate` also uses a temporary folder. Without any option it
prints the banner and a hint to use `--input`, and exits 0.

Print any help topic with `{{tool}} help <topic>`,
as in the command that [[#topic]] prints.
Add `--search <words>` to find a topic, and `--json` to get any help
answer as JSON.
