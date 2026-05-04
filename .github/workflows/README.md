# CI Workflow

This document explains the purpose of the CI (Continuous Integration) workflow in this repository. The workflow is defined in [`ci.yml`](https://github.com/Usin2705/CaptainA_unity/blob/feature/advanced-panel/.github/workflows/ci.yml) in this directory.

The CI automatically checks code quality whenever changes are made. On Ubuntu, to format the code to pass the linter checks, install `CSharpier` with `dotnet tool install csharpier`, and run the formatter with `dotnet csharpier format Assets/Scripts`. If you want to just check the code quality, run `dotnet csharpier check Assets/Scripts`.

`CSharpier` requires `.NET` to be installed. To check if `dotnet` is installed, run `dotnet --info` or `dotnet --list-sdks`, and install it with `sudo apt-get update` and `sudo apt-get install -y dotnet-sdk-10.0` if needed.

### **Triggers:**
- Runs on every push to all other branches except `main`.
- Runs on every pull request targeting `main`.

### **Steps:**
- **Checkout code:** Retrieves the latest code from the repository.
- **Setup .NET Core:** Sets up a `.NET CLI` environment.
- **Install and run CSharpier:** Installs `CSharpier` and checks code style in the `Assets/Scripts` directory.
