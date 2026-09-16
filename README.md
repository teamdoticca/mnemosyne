# Mnemosyne

Portable Markdown language intelligence for .NET.

Mnemosyne parses Markdown documents into structured semantics, classifies significance tags, resolves links against a caller-provided path index, and compares document snapshots for evolution candidates. It is the autonomous document engine used by Mnemon, but has no reference back to Mnemon.

## Install

```shell
dotnet add package Doticca.Mnemosyne
```

The package targets .NET 10 and is managed code only, so it is portable across supported Windows, Linux, and macOS runtimes. It has no native runtime assets.

## Boundary

The library accepts Markdown text, optional paths, path indexes, and document snapshots. It returns structured DTOs and diagnostics. It does not reference Mnemon, SQLite, Git, ASP.NET, or perform filesystem I/O.

## npm

The .NET assembly cannot be published as a directly usable npm package. An npm distribution would require a separate JavaScript or WebAssembly binding with a deliberate JavaScript API. This repository does not publish an npm package until that binding exists.

## Development

```shell
dotnet test Mnemosyne.sln
dotnet pack src/Mnemosyne/Mnemosyne.csproj --output artifacts/nuget
```