# Git hooks

Git hooks shared by the repository. The `pre-commit` hook runs
[`tools/format.sh`](../tools/format.sh) before each commit, so commits with
unformatted C#, Markdown or Python files are blocked.

## Install

The devcontainer installs the hooks when it is created, so there is nothing to
do there.

Outside the devcontainer, run this once from the repository root:

```bash
bash .githooks/setup.sh
```

The hook uses these tools from your `PATH`, and skips any that are not
installed:

- [CSharpier](https://csharpier.com) for C#
- [Prettier](https://prettier.io) for Markdown
- [Ruff](https://docs.astral.sh/ruff/) for Python

The versions the devcontainer installs are in
[`.devcontainer/post-create.sh`](../.devcontainer/post-create.sh).

## Use

By default, the hook only checks formatting. If the check fails, run
`tools/format.sh --fix`, stage the changes and commit again.

To format and re-stage the staged files as part of the commit:

```bash
FORMAT_HOOK=fix git commit
```

If a staged file also has unstaged changes, the hook does not fix anything and
only checks. Re-staging that file would also commit its unstaged changes.

To skip the hook for one commit:

```bash
git commit --no-verify
```

## Uninstall

```bash
git config --unset core.hooksPath
```
