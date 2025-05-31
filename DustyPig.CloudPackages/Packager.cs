using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            DateTime dt = DateTime.UtcNow;
            version = new Version(dt.Year, dt.Month, dt.Day, dt.Hour * 60 + dt.Minute);
            Debug.Print($"Warning: Version is null or 0.0. Using {version}");
        }


        if (outputDirectory.Exists)
            outputDirectory.Delete(true);

        DirectoryInfo versionDirectory = new (Path.Combine(outputDirectory.FullName, $"v{version}"));
        versionDirectory.Create();

        List<PackageFile> packageFiles = [];
        CreatePackageFiles(sourceDirectory, versionDirectory, packageFiles);

        Package package = new()
        {
            Name = name,
            Version = version.ToString(),
            Files = packageFiles
        };

        package.Save(new FileInfo(Path.Combine(outputDirectory.FullName, "package.json")));
    }


    static void CreatePackageFiles(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory, List<PackageFile> files, string relativePath = "")
    {
        foreach (DirectoryInfo dir in sourceDirectory.EnumerateDirectories())
            CreatePackageFiles(dir, outputDirectory.CreateSubdirectory(dir.Name), files, $"{relativePath}/{dir.Name}");

        foreach (FileInfo file in sourceDirectory.EnumerateFiles())
        {
            Debug.Print($"Packaging {file.FullName}");
            using FileStream fs = file.OpenRead();
            files.Add(new PackageFile
            {
                FileSize = file.Length,
                RelativePath = $"{relativePath}/{file.Name}".Trim('/'),
                SHA256 = SHA256Helper.Compute(fs)
            });

            FileInfo destFile = new (Path.Combine(outputDirectory.FullName, file.Name + Constants.PACKAGE_FILE_EXT));
            destFile.Directory.Create();
            file.CopyTo(destFile.FullName, true);
        }
    }
}
