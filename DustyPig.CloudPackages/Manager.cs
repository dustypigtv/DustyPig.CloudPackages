using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.CloudPackages;

public static class Manager
{
    /// <summary>
    /// Creates a cloud package
    /// </summary>
    /// <param name="sourceDirectory">Directory to build the package from</param>
    /// <param name="outputDirectory">Output directory for package files</param>
    /// <param name="name">Optional name of package included in the manifest</param>
    /// <param name="version">Optioanl version number. If ommited, it is built from the current UTC time</param>
    public static void CreatePackage(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory, string name = null, Version version = null) =>
        Packager.CreatePackage(sourceDirectory, outputDirectory, name, version);


    /// <summary>
    /// Adds package files from the cloud to the local install
    /// </summary>
    public static Task InstallAsync(Uri cloudUri, DirectoryInfo root, IProgress<InstallProgress> progress = null, bool deleteNonPackageFiles = false, CancellationToken cancellationToken = default) =>
        Installer.InstallAsync(cloudUri, root, progress, deleteNonPackageFiles, cancellationToken);
}
