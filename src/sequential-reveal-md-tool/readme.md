# Sequential Reveal Markdown Tool

[![NuGet](https://img.shields.io/nuget/v/sequential-reveal-md-tool.svg)](https://www.nuget.org/packages/sequential-reveal-md-tool/)
[![Build](https://github.com/your-repo/vscode-polyglotnotebook-presentation-tools/actions/workflows/dotnet.yml/badge.svg)](https://github.com/your-repo/vscode-polyglotnotebook-presentation-tools/actions)

A tool for sequentially revealing Markdown content in Polyglot Notebooks, designed for use in presentations similar to the way you would animate text appearing in powerpoint or google slides. This library allows you to progressively display sections of Markdown content, making it ideal for step-by-step explanations or interactive presentations.

---

## Features

- **Sequential Markdown Rendering**: Reveal Markdown content section by section, each time you run the C# cell containing the render method, an additional line|section|paragraph|bullet etc will be appended to the cells output.
- **Customisable styles**: Supports styles so you can adjust the size of the markdown typography, either by providing a string, a file path or a cell Tag.

---

## Usage

1. Add the nuget package, link above
2. Add a Markdown cell containing the markdown that you would like to sequentially reveal.
3. Add a C# cell which calls the Render method, add a cell tag to this cell and pass it into the method.
4. Optionally define some styles to override the markdown output font sizes.

See the [example notebook file here](..\..\examples\sequential-reveal.ipynb)

