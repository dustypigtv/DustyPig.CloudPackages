using System;
using System.Collections.Generic;
using System.IO;

namespace DustyPig.CloudPackages;

static class Packager
{
    public static void CreatePackage(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory, string name, Version version)
    {
        if (!sourceDirectory.Exists)
            throw new DirectoryNotFoundException("Source directory does not exist");

        if (version == null || version == new Version(0, 0))
        {
            //throw new ArgumentException("Version cannot be null or 0.0", nameof(version));
            var dt = DateTime.UtcNow;
            version = new Version(dt.Year, dt.Month, dt.Day, dt.Hour * 60 + dt.Minute);
            Console.WriteLine($"Warning: Version is null or 0.0. Using {version}");
        }


        if (outputDirectory.Exists)
            outputDirectory.Delete(true);

        var versionDirectory = new DirectoryInfo(Path.Combine(outputDirectory.FullName, $"v{version}"));
        versionDirectory.Create();

        var packageFiles = new List<PackageFile>();
        CreatePackageFiles(sourceDirectory, versionDirectory, packageFiles);
       
        var package = new Package
        {
            Name = name,
            Version = version.ToString(),
            Files = packageFiles
        };

        package.Save(new FileInfo(Path.Combine(outputDirectory.FullName, "package.json")));
    }


    static void CreatePackageFiles(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory, List<PackageFile> files, string relativePath = "")
    {
        foreach (var dir in sourceDirectory.EnumerateDirectories())
            CreatePackageFiles(dir, outputDirectory.CreateSubdirectory(dir.Name), files, $"{relativePath}/{dir.Name}");

        foreach (var file in sourceDirectory.EnumerateFiles())
        {
            Console.WriteLine($"Packaging {file.FullName}");
            using var fs = file.OpenRead();
            files.Add(new PackageFile
            {
                FileSize = file.Length,
                RelativePath = $"{relativePath}/{file.Name}".Trim('/'),
                SHA256 = SHA256Helper.Compute(fs)
            });

            var destFile = new FileInfo(Path.Combine(outputDirectory.FullName, file.Name + ".dpcp"));
            destFile.Directory.Create();
            file.CopyTo(destFile.FullName, true);
        }
    }
}
