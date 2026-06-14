using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class MaterialProfileService
{
    private static readonly string _baseFolder;
    private static readonly string _materialsFile;
    private static readonly string _machinesFile;
    private static readonly string _settingsFile;

    static MaterialProfileService()
    {
        _baseFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NestLaser", "profiles");
        _materialsFile = Path.Combine(_baseFolder, "materials.json");
        _machinesFile = Path.Combine(_baseFolder, "machines.json");
        _settingsFile = Path.Combine(_baseFolder, "operation-settings.json");
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    // --- Materials ---

    public static List<MaterialProfile> LoadMaterials()
    {
        if (!File.Exists(_materialsFile))
        {
            var seeds = CreateSeedMaterials();
            SaveMaterials(seeds);
            return seeds;
        }
        try
        {
            return SafeJsonFileService.Load<List<MaterialProfile>>(_materialsFile, _jsonOptions) ?? new();
        }
        catch (Exception ex) { AppLogger.LogError(ex, "Material profiles load failed"); return CreateSeedMaterials(); }
    }

    public static void SaveMaterials(List<MaterialProfile> materials)
    {
        EnsureDirectory();
        SafeJsonFileService.Save(_materialsFile, materials, _jsonOptions);
    }

    // --- Machines ---

    public static List<MachineProfile> LoadMachines()
    {
        if (!File.Exists(_machinesFile))
        {
            var seeds = CreateSeedMachines();
            SaveMachines(seeds);
            return seeds;
        }
        try
        {
            return SafeJsonFileService.Load<List<MachineProfile>>(_machinesFile, _jsonOptions) ?? new();
        }
        catch (Exception ex) { AppLogger.LogError(ex, "Machine profiles load failed"); return CreateSeedMachines(); }
    }

    public static void SaveMachines(List<MachineProfile> machines)
    {
        EnsureDirectory();
        SafeJsonFileService.Save(_machinesFile, machines, _jsonOptions);
    }

    // --- Operation Settings ---

    public static List<MaterialOperationSetting> LoadSettings()
    {
        if (!File.Exists(_settingsFile))
        {
            var seeds = CreateSeedSettings();
            SaveSettings(seeds);
            return seeds;
        }
        try
        {
            return SafeJsonFileService.Load<List<MaterialOperationSetting>>(_settingsFile, _jsonOptions) ?? new();
        }
        catch (Exception ex) { AppLogger.LogError(ex, "Material operation settings load failed"); return CreateSeedSettings(); }
    }

    public static void SaveSettings(List<MaterialOperationSetting> settings)
    {
        EnsureDirectory();
        SafeJsonFileService.Save(_settingsFile, settings, _jsonOptions);
    }

    // --- Lookup ---

    public static MaterialOperationSetting? FindSetting(
        List<MaterialOperationSetting> settings,
        string materialId,
        string machineId,
        OperationType opType)
    {
        return settings.FirstOrDefault(s =>
            s.MaterialId == materialId &&
            s.MachineId == machineId &&
            s.OperationType == opType);
    }

    // --- Seed Data ---

    private static List<MaterialProfile> CreateSeedMaterials()
    {
        int id = 0;
        string N() => $"seed_{++id}";

        return new List<MaterialProfile>
        {
            new() { Id = N(), Name = "MDF", Category = "Ahşap", ThicknessMm = 3, IsDefault = true },
            new() { Id = N(), Name = "MDF", Category = "Ahşap", ThicknessMm = 5 },
            new() { Id = N(), Name = "MDF", Category = "Ahşap", ThicknessMm = 8 },
            new() { Id = N(), Name = "Pleksi", Category = "Akrilik", ThicknessMm = 2, IsDefault = true },
            new() { Id = N(), Name = "Pleksi", Category = "Akrilik", ThicknessMm = 3 },
            new() { Id = N(), Name = "Pleksi", Category = "Akrilik", ThicknessMm = 5 },
            new() { Id = N(), Name = "Kontraplak", Category = "Ahşap", ThicknessMm = 3 },
            new() { Id = N(), Name = "Kontraplak", Category = "Ahşap", ThicknessMm = 6 },
            new() { Id = N(), Name = "Kontraplak", Category = "Ahşap", ThicknessMm = 10 },
            new() { Id = N(), Name = "Paslanmaz", Category = "Metal", ThicknessMm = 0.5 },
            new() { Id = N(), Name = "Paslanmaz", Category = "Metal", ThicknessMm = 1 },
            new() { Id = N(), Name = "Galvaniz", Category = "Metal", ThicknessMm = 0.5 },
            new() { Id = N(), Name = "Galvaniz", Category = "Metal", ThicknessMm = 1 },
            new() { Id = N(), Name = "Deri", Category = "Organik", ThicknessMm = 2 },
            new() { Id = N(), Name = "Deri", Category = "Organik", ThicknessMm = 3 },
            new() { Id = N(), Name = "Karton", Category = "Kâğıt", ThicknessMm = 1 },
            new() { Id = N(), Name = "Karton", Category = "Kâğıt", ThicknessMm = 2 },
            new() { Id = N(), Name = "Kumaş", Category = "Tekstil", ThicknessMm = 1 },
        };
    }

    private static List<MachineProfile> CreateSeedMachines()
    {
        int id = 0;
        string N() => $"seed_{++id}";

        return new List<MachineProfile>
        {
            new() { Id = N(), Name = "Ruida 100W", Manufacturer = "Ruida", Model = "RDLC-100", LaserType = "CO2", WorkingAreaX = 900, WorkingAreaY = 600 },
            new() { Id = N(), Name = "Ruida 130W", Manufacturer = "Ruida", Model = "RDLC-130", LaserType = "CO2", WorkingAreaX = 1300, WorkingAreaY = 900 },
            new() { Id = N(), Name = "Ruida 150W", Manufacturer = "Ruida", Model = "RDLC-150", LaserType = "CO2", WorkingAreaX = 1600, WorkingAreaY = 1000 },
            new() { Id = N(), Name = "CO2 Generic 80W", Manufacturer = "Generic", Model = "CO2-80", LaserType = "CO2", WorkingAreaX = 600, WorkingAreaY = 400 },
            new() { Id = N(), Name = "Fiber 20W", Manufacturer = "Generic", Model = "FIBER-20", LaserType = "Fiber", WorkingAreaX = 300, WorkingAreaY = 300 },
            new() { Id = N(), Name = "Fiber 30W", Manufacturer = "Generic", Model = "FIBER-30", LaserType = "Fiber", WorkingAreaX = 400, WorkingAreaY = 400 },
            new() { Id = N(), Name = "Fiber 50W", Manufacturer = "Generic", Model = "FIBER-50", LaserType = "Fiber", WorkingAreaX = 500, WorkingAreaY = 500 },
        };
    }

    private static List<MaterialOperationSetting> CreateSeedSettings()
    {
        int id = 0;
        string N() => $"seed_{++id}";

        var settings = new List<MaterialOperationSetting>();

        // --- MDF 3mm ---
        string mdf3Id = "seed_1";
        string mdf5Id = "seed_2";
        string mdf8Id = "seed_3";
        string plexi2Id = "seed_4";
        string plexi3Id = "seed_5";
        string plexi5Id = "seed_6";

        string ruida100Id = "seed_1";
        string ruida130Id = "seed_2";
        string ruida150Id = "seed_3";
        string co2GenericId = "seed_4";

        // MDF 3mm - Ruida 100W
        settings.Add(new() { Id = N(), MaterialId = mdf3Id, MachineId = ruida100Id, OperationType = OperationType.CutOuter, Power = 75, Speed = 25, PassCount = 1, Frequency = 20000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = mdf3Id, MachineId = ruida100Id, OperationType = OperationType.CutInner, Power = 65, Speed = 20, PassCount = 1, Frequency = 20000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = mdf3Id, MachineId = ruida100Id, OperationType = OperationType.Engrave, Power = 30, Speed = 200, PassCount = 1, Frequency = 25000, AirAssist = false });
        settings.Add(new() { Id = N(), MaterialId = mdf3Id, MachineId = ruida100Id, OperationType = OperationType.Mark, Power = 20, Speed = 300, PassCount = 1, Frequency = 30000, AirAssist = false });

        // MDF 3mm - Ruida 130W
        settings.Add(new() { Id = N(), MaterialId = mdf3Id, MachineId = ruida130Id, OperationType = OperationType.CutOuter, Power = 70, Speed = 30, PassCount = 1, Frequency = 20000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = mdf3Id, MachineId = ruida130Id, OperationType = OperationType.CutInner, Power = 60, Speed = 25, PassCount = 1, Frequency = 20000, AirAssist = true });

        // MDF 3mm - Ruida 150W
        settings.Add(new() { Id = N(), MaterialId = mdf3Id, MachineId = ruida150Id, OperationType = OperationType.CutOuter, Power = 60, Speed = 35, PassCount = 1, Frequency = 20000, AirAssist = true });

        // MDF 5mm - Ruida 100W
        settings.Add(new() { Id = N(), MaterialId = mdf5Id, MachineId = ruida100Id, OperationType = OperationType.CutOuter, Power = 85, Speed = 15, PassCount = 1, Frequency = 20000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = mdf5Id, MachineId = ruida100Id, OperationType = OperationType.CutInner, Power = 75, Speed = 12, PassCount = 1, Frequency = 20000, AirAssist = true });

        // MDF 5mm - Ruida 130W
        settings.Add(new() { Id = N(), MaterialId = mdf5Id, MachineId = ruida130Id, OperationType = OperationType.CutOuter, Power = 80, Speed = 20, PassCount = 1, Frequency = 20000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = mdf5Id, MachineId = ruida130Id, OperationType = OperationType.CutInner, Power = 70, Speed = 16, PassCount = 1, Frequency = 20000, AirAssist = true });

        // MDF 5mm - Ruida 150W
        settings.Add(new() { Id = N(), MaterialId = mdf5Id, MachineId = ruida150Id, OperationType = OperationType.CutOuter, Power = 70, Speed = 25, PassCount = 1, Frequency = 20000, AirAssist = true });

        // MDF 8mm - Ruida 100W
        settings.Add(new() { Id = N(), MaterialId = mdf8Id, MachineId = ruida100Id, OperationType = OperationType.CutOuter, Power = 95, Speed = 8, PassCount = 2, Frequency = 20000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = mdf8Id, MachineId = ruida100Id, OperationType = OperationType.CutInner, Power = 90, Speed = 6, PassCount = 2, Frequency = 20000, AirAssist = true });

        // MDF 8mm - Ruida 130W
        settings.Add(new() { Id = N(), MaterialId = mdf8Id, MachineId = ruida130Id, OperationType = OperationType.CutOuter, Power = 90, Speed = 12, PassCount = 1, Frequency = 20000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = mdf8Id, MachineId = ruida130Id, OperationType = OperationType.CutInner, Power = 85, Speed = 10, PassCount = 1, Frequency = 20000, AirAssist = true });

        // MDF 8mm - Ruida 150W
        settings.Add(new() { Id = N(), MaterialId = mdf8Id, MachineId = ruida150Id, OperationType = OperationType.CutOuter, Power = 80, Speed = 16, PassCount = 1, Frequency = 20000, AirAssist = true });

        // Pleksi 2mm - Ruida 100W
        settings.Add(new() { Id = N(), MaterialId = plexi2Id, MachineId = ruida100Id, OperationType = OperationType.CutOuter, Power = 55, Speed = 30, PassCount = 1, Frequency = 5000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = plexi2Id, MachineId = ruida100Id, OperationType = OperationType.Engrave, Power = 25, Speed = 250, PassCount = 1, Frequency = 10000, AirAssist = false });

        // Pleksi 3mm - Ruida 100W
        settings.Add(new() { Id = N(), MaterialId = plexi3Id, MachineId = ruida100Id, OperationType = OperationType.CutOuter, Power = 65, Speed = 20, PassCount = 1, Frequency = 5000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = plexi3Id, MachineId = ruida100Id, OperationType = OperationType.CutInner, Power = 55, Speed = 16, PassCount = 1, Frequency = 5000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = plexi3Id, MachineId = ruida100Id, OperationType = OperationType.Engrave, Power = 30, Speed = 200, PassCount = 1, Frequency = 10000, AirAssist = false });

        // Pleksi 3mm - Ruida 130W
        settings.Add(new() { Id = N(), MaterialId = plexi3Id, MachineId = ruida130Id, OperationType = OperationType.CutOuter, Power = 60, Speed = 25, PassCount = 1, Frequency = 5000, AirAssist = true });

        // Pleksi 5mm - Ruida 100W
        settings.Add(new() { Id = N(), MaterialId = plexi5Id, MachineId = ruida100Id, OperationType = OperationType.CutOuter, Power = 80, Speed = 12, PassCount = 1, Frequency = 5000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = plexi5Id, MachineId = ruida100Id, OperationType = OperationType.CutInner, Power = 70, Speed = 10, PassCount = 1, Frequency = 5000, AirAssist = true });

        // Pleksi 5mm - Ruida 130W
        settings.Add(new() { Id = N(), MaterialId = plexi5Id, MachineId = ruida130Id, OperationType = OperationType.CutOuter, Power = 75, Speed = 16, PassCount = 1, Frequency = 5000, AirAssist = true });
        settings.Add(new() { Id = N(), MaterialId = plexi5Id, MachineId = ruida130Id, OperationType = OperationType.CutInner, Power = 65, Speed = 13, PassCount = 1, Frequency = 5000, AirAssist = true });

        // Generic CO2 80W defaults for MDF 3mm
        settings.Add(new() { Id = N(), MaterialId = mdf3Id, MachineId = co2GenericId, OperationType = OperationType.CutOuter, Power = 90, Speed = 15, PassCount = 1, Frequency = 20000, AirAssist = true });

        return settings;
    }

    private static void EnsureDirectory()
    {
        if (!Directory.Exists(_baseFolder))
            Directory.CreateDirectory(_baseFolder);
    }
}
