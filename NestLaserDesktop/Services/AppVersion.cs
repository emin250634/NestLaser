using System.Reflection;

namespace NestLaserDesktop.Services;

public static class AppVersion
{
    public static string ProductName => "NestLaser Desktop";
    public static string Company => "NestLaser";
    public static string Description => "Profesyonel lazer kesim nesting workspace";

    public static string ProductVersion => Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "1.0.0-RC1";

    public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

    public static string FileVersion => Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "1.0.0.0";

    public static string AssemblyVersion => Version;

    public static string ReleaseChannel
    {
        get
        {
            var pv = ProductVersion;
            if (pv.Contains("-RC"))
                return "Release Candidate";
            if (pv.Contains("-Beta"))
                return "Beta";
            if (pv.Contains("-Alpha"))
                return "Alpha";
            return "Stable";
        }
    }

    public static string Copyright => Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? $"Copyright © {DateTime.Now.Year} NestLaser";

    public static string GetAboutText() => $@"{ProductName}
{Description}

Sürüm: {ProductVersion}
Build: {FileVersion}
Yayın Kanalı: {ReleaseChannel}

{Copyright}

Destek: https://nestlaser.com/destek";
}
