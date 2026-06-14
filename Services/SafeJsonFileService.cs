using System;
using System.IO;
using System.Text.Json;

namespace NestLaserDesktop.Services;

public static class SafeJsonFileService
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true
    };

    public static void Save<T>(string path, T value, JsonSerializerOptions? options = null)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        string tempPath = path + ".tmp";
        string backupPath = path + ".bak";
        string json = JsonSerializer.Serialize(value, options ?? DefaultOptions);

        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, path);
            File.Copy(path, backupPath, overwrite: true);
        }
    }

    public static T? Load<T>(string path, JsonSerializerOptions? options = null)
    {
        try
        {
            if (!File.Exists(path))
                return default;

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"JSON load failed: {path}");
            string backupPath = path + ".bak";
            if (!File.Exists(backupPath))
                return default;

            try
            {
                string backupJson = File.ReadAllText(backupPath);
                return JsonSerializer.Deserialize<T>(backupJson, options ?? DefaultOptions);
            }
            catch (Exception backupEx)
            {
                AppLogger.LogError(backupEx, $"JSON backup load failed: {backupPath}");
                return default;
            }
        }
    }
}
