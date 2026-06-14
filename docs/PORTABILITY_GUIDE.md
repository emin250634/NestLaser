# NestLaser Portability Guide

## Purpose

Phase 8N makes `.nelp` projects usable across different computers, Windows user accounts, and future NestLaser versions with minimum data loss.

## Project Versioning

Every saved project now carries:

- `ProjectVersion`
- `CreatedWithVersion`
- `LastSavedWithVersion`

Current version is `1.0.0`. These fields allow later project migrations without guessing the source schema.

## Migration

`ProjectMigrationService` upgrades loaded projects to the current project format. The 1.0.0 migration currently normalizes missing core objects, version fields, profile snapshots, PDF settings, company profile, and cost settings.

Future migrations should be added as explicit version steps before setting `LastSavedWithVersion`.

## Embedded Snapshots

Portable projects include snapshots for:

- selected material profile
- selected machine profile
- material/machine operation settings
- cost settings
- company profile
- PDF report settings

This allows a project to open even if the destination computer has empty or different AppData profile databases.

## Missing Profile Recovery

When a project opens and the selected material or machine profile cannot be found locally:

1. The project snapshot is restored into the in-memory profile list.
2. If no snapshot exists, a temporary recovered profile is created.
3. A status warning is shown instead of crashing or silently losing the reference.

Temporary profiles should be replaced with real production profiles before final manufacturing.

## Integrity Checks

`ProjectIntegrityService` validates and repairs:

- missing or duplicate part IDs
- missing or duplicate layer IDs
- missing or duplicate operation IDs
- invalid part layer references
- invalid operation layer references
- selected material/machine IDs recoverable from snapshots

Recovery details are stored in `ProjectRecoveryReport`.

## Backup System

Before overwriting an existing project, NestLaser creates a dated project backup:

`Backups/Project_yyyy_MM_dd_HHmm.nelp`

Only the latest 10 backups per project are kept.

The existing safe JSON `.bak` file remains in use for low-level write recovery.

## Recovery Mode

If a project JSON file is corrupt, NestLaser tries to load the `.bak` file and reports:

- corrupted sections
- recovered sections
- lost sections
- integrity warnings
- migration notes

Silent data loss is not acceptable; every recovery path should add a report entry.

## Project Packages

`.nelpkg` is a zip-based project package for moving a project as one file.

Package contents:

- `project.nelp`
- `material-snapshot.json`
- `machine-snapshot.json`
- `operation-settings-snapshot.json`
- `cost-settings-snapshot.json`
- optional `report.pdf`
- optional `export-report.txt`
- `manifest.json`

Importing a package runs the same migration, snapshot recovery, and integrity checks as normal project loading.

## Regression Tests

Portability tests cover:

- migration/version fields
- profile snapshot roundtrip
- corrupt project recovery via `.bak`
- package export/import
- ID and layer reference repair
- latest 10 project backup retention

Run:

```powershell
dotnet test
```
