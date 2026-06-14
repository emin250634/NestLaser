using System;
using System.IO;

namespace NestLaserDesktop.Tests;

internal static class TestPaths
{
    public static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "samples", "dxf")))
                    return dir.FullName;

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Repository root could not be located from test output directory.");
        }
    }

    public static string Fixture(string fileName) => Path.Combine(RepoRoot, "samples", "dxf", fileName);
}
