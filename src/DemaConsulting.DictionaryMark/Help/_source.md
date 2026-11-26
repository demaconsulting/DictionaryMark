---
name: dictionarymark
schema: 2
title: DictionaryMark
summary: Built-in help of DictionaryMark, which turns YAML dictionary files into Markdown lists and tables.
---
{{tool}} reads YAML dictionary files of approved or deprecated terms and writes them as a Markdown bullet list or
table, for a glossary in the documentation.

Every operation is an option. The only command is `{{tool}} help`, which must be the first argument and takes topic
names, such as `{{tool}} help command-line`.
