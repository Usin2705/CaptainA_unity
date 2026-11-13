# CI Workflow

This document explains the purpose of the CI (Continuous Integration) workflow
in this repository. The workflow is defined in [`ci.yml`](/.github/workflows/ci.yml) in this
directory.

The CI checks that the C# code in `Assets/Scripts` is formatted with
[CSharpier](https://csharpier.com). The CSharpier version is pinned in
[`.config/dotnet-tools.json`](/.config/dotnet-tools.json), so CI and your
machine use the same one.

To run the same check locally, from the repository root:

```bash
dotnet tool restore
dotnet csharpier check Assets/Scripts
```

To format the code so that it passes, run

```bash
dotnet csharpier format Assets/Scripts
```

CSharpier requires the .NET SDK. To check if it is installed, run
`dotnet --list-sdks`. On Ubuntu, install it with

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

The [dev container](/.devcontainer/README.md) already has the .NET SDK
and CSharpier installed, so you can skip this setup there.

### **Triggers:**

- Runs on every push to all other branches except `main`.
- Runs on every pull request targeting `main`.

### **Steps:**

- **Checkout repository:** Downloads and checks out only `Assets/Scripts`,
  `.config` and the files at the repository root, so the large Unity assets are
  never downloaded.
- **Set up .NET:** Sets up a `.NET CLI` environment.
- **Restore .NET tools:** Installs the pinned version with `dotnet tool restore`.
- **Run format check:** Checks code style in the `Assets/Scripts` directory.
