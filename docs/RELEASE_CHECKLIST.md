# Release Checklist — NestLaser Desktop

**Version:** 1.0.0-RC1  
**Phase:** FAZ 8P — Installer, Packaging & Release Readiness

---

## Pre-Release Validation

### Build & Test

- [ ] `dotnet restore NestLaserDesktop.sln` succeeds with 0 errors
- [ ] `dotnet build NestLaserDesktop.sln -c Release` produces 0 warnings, 0 errors
- [ ] `dotnet test NestLaserDesktop.sln` all tests pass (28/28)
- [ ] Assembly metadata shows correct Product, Company, Version
- [ ] About dialog displays version information correctly

### Code Quality

- [ ] No TODO comments remaining
- [ ] No hardcoded paths (except AppData)
- [ ] No secrets or keys in code
- [ ] Error handling in place for all file operations

---

## Core Functionality

### Import & Export

- [ ] DXF import works (unitless, mm, inch)
- [ ] DXF export generates valid file
- [ ] Layer visible/reference rules preserved
- [ ] Plate border export option works
- [ ] export-report.txt generated

### Project System

- [ ] New project creates clean state
- [ ] Save project creates .nelp file
- [ ] Open project restores all state
- [ ] Recent projects list updates
- [ ] Undo/Redo works (50 levels)
- [ ] Profile snapshot persists in .nelp

### Nesting

- [ ] Free Rectangle nesting runs
- [ ] Polygon Collision nesting runs
- [ ] Irregular plate nesting runs
- [ ] Benchmark completes
- [ ] NestResult displays correctly

### Cost & PDF

- [ ] Cost calculation completes
- [ ] PerSheet/PerSquareMeter/PerKg works
- [ ] Quotation PDF generates
- [ ] Production PDF generates
- [ ] PDF contains %PDF header
- [ ] Company logo appears in PDF

### Profiles

- [ ] Material profiles save/load
- [ ] Machine profiles save/load
- [ ] Operation settings save/load
- [ ] Cost settings save/load
- [ ] Missing profiles show warning

---

## Data Safety

### Backup System

- [ ] Project save creates .bak backup
- [ ] Dated backups in backups folder
- [ ] Latest 10 backups retained
- [ ] .bak loads when primary corrupted

### Safe JSON

- [ ] .tmp file created during save
- [ ] Atomic replace succeeds
- [ ] JSON parse errors handled
- [ ] Fallback to .bak works

### Crash Recovery

- [ ] error-log.txt created in logs folder
- [ ] UnhandledException logged
- [ ] DispatcherUnhandledException logged
- [ ] TaskScheduler exceptions logged

---

## Packaging

### Portable Build

- [x] `dotnet publish -c Release -p:PublishProfile=Properties/PublishProfiles/Portable.pubxml` succeeds
- [x] dist/portable/ folder contains all files
- [x] Application launches from portable folder (self-contained)
- [x] AppData created on first run
- [x] Runtime files present: coreclr.dll, hostfxr.dll, hostpolicy.dll

### Installer (Inno Setup)

- [ ] Inno Setup script exists at scripts/setup.iss
- [ ] Installer builds successfully
- [ ] Install creates Start Menu shortcut
- [ ] Install creates Desktop shortcut
- [ ] Uninstaller removes application
- [ ] User data preserved after reinstall

---

## First Run Experience

- [ ] First run creates AppData folders
- [ ] First run flag file created
- [ ] Default material profiles loaded
- [ ] Default machine profiles loaded
- [ ] No errors on fresh start

---

## Release Artifacts

### Required Files

- [ ] NestLaserDesktop.exe (main executable)
- [ ] NestLaserDesktop.dll (application assembly)
- [ ] Portable distribution folder (self-contained)
- [ ] Installer setup.exe (if using Inno Setup)

### Documentation

- [ ] RELEASE_READINESS_REVIEW.md exists
- [ ] RELEASE_CHECKLIST.md exists
- [ ] Updated PROJECT_MEMORY.md
- [ ] Updated DEVELOPMENT_LOG.md
- [ ] Updated ROADMAP.md
- [ ] Updated TEST_NOTES.md

---

## Post-Release

- [ ] Tag release in git (v1.0.0-RC1)
- [ ] Create GitHub release with artifacts
- [ ] Update download links
- [ ] Announce to users

---

## Quick Validation Commands

```powershell
# Build & Test
dotnet restore NestLaserDesktop.sln
dotnet build NestLaserDesktop.sln -c Release
dotnet test NestLaserDesktop.sln

# Portable publish (script)
.\scripts\build-release.ps1 -Clean -Portable

# Portable publish (direct)
dotnet publish NestLaserDesktop.csproj -c Release -r win-x64 -p:PublishProfile=Properties/PublishProfiles\Portable.pubxml
```

---

## Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Lead Developer | | | |
| QA | | | |
| Product Owner | | | |

---

**Checklist Version:** 1.0.0-RC1  
**Last Updated:** 2026-06-13
