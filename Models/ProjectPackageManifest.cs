using System;
using System.Collections.Generic;

namespace NestLaserDesktop.Models;

public class ProjectPackageManifest
{
    public string PackageVersion { get; set; } = "1.0.0";
    public string CreatedWithVersion { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string ProjectFile { get; set; } = "project.nelp";
    public List<string> IncludedFiles { get; set; } = new();
}
