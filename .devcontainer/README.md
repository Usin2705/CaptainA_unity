# Dev container

A [development container](https://containers.dev) with the tools to edit,
format and lint the C#, Markdown and Python files in this repository.

The Unity Editor does not run in the container. Open the project in Unity on
the host as usual, and use the container for editing code and running the
formatters.

## Open the container

1. Install [Podman](https://podman.io/docs/installation) (or
   [Docker](https://docs.docker.com/get-started/get-docker/)) and the VS Code
   [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
   extension.
2. With Podman, set `dev.containers.dockerPath` to `podman` in your VS Code
   settings.
3. Open the repository in VS Code.
4. Run **Dev Containers: Reopen in Container** from the Command Palette.

The first build takes a few minutes. Later starts reuse the built container.

## What is included

| Tool      | Purpose                                     | Source                             |
| --------- | ------------------------------------------- | ---------------------------------- |
| .NET SDK  | C# language support and the Unity extension | Base image `devcontainers/dotnet`  |
| Node.js   | Runtime for Prettier                        | `node` feature                     |
| Python    | Runtime for the Python scripts              | `python` feature                   |
| CSharpier | C# formatter                                | [`post-create.sh`](post-create.sh) |
| Prettier  | Markdown formatter                          | [`post-create.sh`](post-create.sh) |
| Ruff      | Python linter and formatter                 | `python` feature                   |

The container also installs the matching VS Code extensions, so the editor
uses the same formatters.

## Files

- [`devcontainer.json`](devcontainer.json): the container definition, with the
  base image, features and VS Code extensions.
- [`devcontainer-lock.json`](devcontainer-lock.json): the exact feature
  versions, so every build uses the same ones.
- [`post-create.sh`](post-create.sh): runs once after the container is created.
  It installs CSharpier and Prettier, prints the version of each tool and
  enables the [Git hooks](../.githooks/README.md).

To change the CSharpier or Prettier version, edit the version at the top of
`post-create.sh` and rebuild the container with **Dev Containers: Rebuild
Container**.

## Personal configuration

For settings that only apply to you, such as extra mounts, copy the
configuration into `.devcontainer/local/devcontainer.json` and edit it there.
Git ignores any path under `.devcontainer/` that contains `local`.

When the repository has more than one configuration, **Reopen in Container**
asks which one to use. Keep the personal copy in step with `devcontainer.json`
when the shared configuration changes.
