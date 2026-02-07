All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [0.1.3] - 2026-02-07
### Patch
Traverse up in the workdir to find the changesets root directory.
This allows to execute monover in subdirectories.
Fixed #1: allowed to create Changesets for projects that do not have the VersionPrefix tag and are therefore not publishable.

## [0.1.2] - 2025-03-19
### Patch
updated dependency MonoVer.Domain
## [0.1.1] - 2025-02-01
### Patch
Fixed: Delete changesets after they are applied

The File delete had been commented out for testing purposes
