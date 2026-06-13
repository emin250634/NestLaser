# Release Readiness Review — NestLaser Desktop

**Date:** 2026-06-13  
**Phase:** FAZ 8P — Installer, Packaging & Release Readiness  
**Audit Type:** Pre-release system audit

---

## 1. Release Pre-audit Summary

### Current State Assessment

| Category | Status | Notes |
|----------|--------|-------|
| Build System | ✅ Ready | dotnet build works with 0 errors |
| Test Suite | ✅ Ready | 28 tests passing |
| Version System | ❌ Missing | No centralized version info |
| Assembly Info | ❌ Missing | No Product, Company, Copyright metadata |
| Application Icon | ❌ Missing | No .ico files |
| About Dialog | ⚠️ Basic | Simple MessageBox, needs improvement |
| First Run Setup | ⚠️ Implicit | Folders created on-demand, not explicitly |
| Portable Build | ❌ Not Configured | No publish profile |
| Installer | ❌ Not Configured | No MSIX or Inno Setup |
| Data Safety | ✅ Ready | SafeJsonFileService with .bak backups |
| Crash Logging | ✅ Ready | AppLogger writes to error-log.txt |
| Backup System | ✅ Ready | ProjectBackupService with dated backups |

---

## 2. Release Pre-requisites

### Missing Items

| Item | Priority | Impact |
|------|----------|--------|
| Assembly metadata (Product, Company, Version) | High | Required for Windows properties |
| Application icon | High | Professional appearance |
| Centralized version system | High | Required for About and updates |
| Improved About dialog | Medium | User experience |
| First run explicit setup | Medium | Data integrity |
| Portable build configuration | Medium | Distribution option |
| Installer configuration | High | Primary distribution |
| Release checklist | High | Process documentation |

### Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| No installer | High | Document as manual install for RC1 |
| No icon | Medium | Use default Windows icon |
| Version not visible | Low | Add to About dialog |
| Data loss on update | Low | Backup system already in place |

---

## 3. File Dependencies

### Runtime Dependencies

| File | Location | Purpose |
|------|----------|---------|
| NestLaserDesktop.exe | bin/Debug/net8.0-windows/ | Main executable |
| NestLaserDesktop.dll | bin/Debug/net8.0-windows/ | Application assembly |
| AppLogger.cs | Services/ | Error logging to AppData |

### Data Dependencies (AppData)

| Folder | Path | Contents |
|--------|------|----------|
| Profiles | %APPDATA%/NestLaser/profiles/ | Material, machine, operation, cost JSON |
| Projects | %APPDATA%/NestLaser/projects/ | Recent project list |
| Backups | %APPDATA%/NestLaser/backups/ | Dated project backups |
| Logs | %APPDATA%/NestLaser/logs/ | error-log.txt |

### Project File Dependencies

- .NET 8.0-windows (WPF)
- System.Text.Json (built-in)
- No external NuGet packages required for core functionality

---

## 4. AppData Usage

### Current Structure

```
%APPDATA%/NestLaser/
├── profiles/
│   ├── materials.json
│   ├── machines.json
│   ├── operations.json
│   └── cost-settings.json
├── projects/
│   └── recent.json
├── backups/
│   └── 2026-06-13_*.nelp
├── logs/
│   └── error-log.txt
└── settings.json
```

### Observations

- All AppData folders created on-demand by services
- No explicit first-run setup
- Backup system supports dated retention (latest 10)
- Safe JSON saves use .tmp + .bak pattern
- No AppData cleanup on uninstall (installer not configured)

---

## 5. First Run Behavior

### Current (Implicit)

1. App starts → MainWindow loads
2. First file operation triggers folder creation
3. No explicit first-run message or setup
4. Material/machine profiles loaded from seed data if missing

### Recommended (FAZ 8P)

1. App starts → Check for first run flag
2. If first run:
   - Create all AppData folders explicitly
   - Log "First run setup completed" to logs
   - Set first-run flag
3. Continue to MainWindow

---

## 6. Installer Strategy Analysis

### Option A: MSIX

**Pros:**
- Modern Windows 10/11 native
- Auto-update support via Store
- Clean install/uninstall
- Sandboxed execution

**Cons:**
- Requires Windows SDK tools
- Code signing certificate needed for distribution
- More complex build pipeline
- Store submission required for auto-update

### Option B: Inno Setup

**Pros:**
- Simple script-based configuration
- No code signing required for internal use
- Supports portable and installed variants
- Well-documented, mature tool

**Cons:**
- No auto-update built-in
- Traditional Windows installer experience
- Manual uninstall entry

### Recommendation

**Inno Setup** for RC1:
- Simpler to implement quickly
- No code signing requirement
- Can create portable variant
- Sufficient for initial distribution

**MSIX** for v1.0+:
- If Store distribution desired
- If auto-update required
- Requires code signing investment

---

## 7. Portable Build Strategy

### Requirements

- Self-contained deployment (includes .NET runtime)
- Single folder distribution
- No installer required
- User data in same folder (or AppData if preferred)

### Configuration

- Publish profile: `publish/Profile/Portable.pubxml`
- Output: `dist/portable/`
- Runtime: win-x64
- Self-contained: true

---

## 8. Release Channel Definition

| Channel | Version Pattern | Purpose |
|---------|-----------------|---------|
| RC1 | 1.0.0-RC1 | First release candidate |
| RC2 | 1.0.0-RC2 | Bug fixes from RC1 |
| Release | 1.0.0 | First stable release |

### Version Components

- **ProductVersion**: 1.0.0-RC1 (user-facing)
- **AssemblyVersion**: 1.0.0.0 (runtime)
- **FileVersion**: 1.0.0.1 (build increment)
- **BuildVersion**: Auto-incremented by CI or manual

---

## 9. Crash Recovery Validation

### Current Status

| Component | Status | Details |
|-----------|--------|---------|
| error-log.txt | ✅ Ready | AppLogger writes to %APPDATA%/NestLaser/logs/ |
| Backup system | ✅ Ready | ProjectBackupService with dated backups |
| Recovery system | ✅ Ready | ProjectIntegrityService repairs corrupted projects |
| Undo stack | ✅ Ready | 50-level undo preserved in .nelp |

### Validation

- Unhandled exceptions logged via AppDomain.UnhandledException
- Dispatcher exceptions logged via DispatcherUnhandledException
- Task exceptions logged via TaskScheduler.UnobservedTaskException
- JSON save uses temp-file + replace + .bak pattern

**Conclusion**: Crash recovery is adequate for RC1.

---

## 10. Next Steps

1. Add assembly metadata to csproj
2. Create version constants class
3. Add application icon
4. Improve About dialog
5. Add first-run setup to App.xaml.cs
6. Create portable publish profile
7. Create Inno Setup script
8. Create build-release.ps1 script
9. Update documentation
10. Final validation

---

## 11. Release Checklist Preview

- [ ] dotnet restore succeeds
- [ ] dotnet build produces 0 warnings, 0 errors
- [ ] dotnet test passes all 28 tests
- [ ] Portable build creates self-contained folder
- [ ] Installer builds successfully (Inno Setup)
- [ ] First-run creates AppData folders
- [ ] About dialog shows version info
- [ ] Error logging writes to correct location
- [ ] Backup system creates dated backups
- [ ] Project save/load roundtrips correctly
