# MonoVer

MonoVer is a version management tool designed for mono-repositories and single-project repositories. It automates versioning, changelog updates, and publishing workflows, making it easier to manage and track changes across multiple projects.

## Features

- **Automatic Versioning**: Collects changesets and applies version bumps based on semantic rules.
- **Changelog Management**: Helps track changes efficiently.
- **Monorepo Support**: Handles multi-package repositories efficiently.
- **Preview & Release Workflow**: Supports feature branch previews and stable releases.
- **NuGet Integration**: Publishes packages to NuGet if needed.

## Installation

MonoVer is distributed as a .NET tool. To install it, run:

```sh
 dotnet tool install --global monover
```

Or, if using a local tool manifest:

```sh
 dotnet new tool-manifest
 dotnet tool install monover
```

## Usage

Run `monover help` to see available commands.

### Initialize

```sh
monover init
```

Initializes the repository for MonoVer.

### Create a Changeset

```sh
monover new
```

Creates a new changeset entry.

### Apply Changesets and Publish

```sh
monover publish
```

Applies all open changesets and publishes the new versions.

### Display Version Information

```sh
monover version
```

Displays the current MonoVer version.

## Contributing

Contributions are welcome! Please submit issues and pull requests via [GitHub](https://github.com/hoolie/monover).

## License

MonoVer is licensed under the MIT License. See [LICENSE](LICENSE) for details.

