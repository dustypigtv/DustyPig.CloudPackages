using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.CloudPackages;

public static class Manager
{
    static readonly Lazy<HttpClient> _defaultClient = new();



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
    /// <param name="cloudUri">https path to package.json</param>
    /// <param name="installDirectory"><see cref="DirectoryInfo"/> target where the package will be installed</param>
    /// <param name="progress">Optional <see cref="IProgress{InstallProgress}"/> to track install progress</param>
    /// <param name="deleteNonPackageFiles">Optionally delete all non-package files in the <paramref name="installDirectory"/>. Default is false/></param>
    public static Task Install(Uri cloudUri, DirectoryInfo installDirectory, IProgress<InstallProgress> progress = null, bool deleteNonPackageFiles = false, CancellationToken cancellationToken = default) =>
        Installer.Install(_defaultClient.Value, cloudUri, installDirectory, progress, deleteNonPackageFiles, cancellationToken);


    /// <summary>
    /// Adds package files from the cloud to the local install using the supplied <see cref="HttpClient"/>
    /// </summary>
    /// <param name="client"><see cref="HttpClient"/> to use for downloading files. Usefull if you need ot configure credentials</param>
    /// <param name="cloudUri">https path to package.json</param>
    /// <param name="installDirectory"><see cref="DirectoryInfo"/> target where the package will be installed</param>
    /// <param name="progress">Optional <see cref="IProgress{InstallProgress}"/> to track install progress</param>
    /// <param name="deleteNonPackageFiles">Optionally delete all non-package files in the <paramref name="installDirectory"/>. Default is false/></param>
    public static Task Install(HttpClient client, Uri cloudUri, DirectoryInfo installDirectory, IProgress<InstallProgress> progress = null, bool deleteNonPackageFiles = false, CancellationToken cancellationToken = default) =>
        Installer.Install(client, cloudUri, installDirectory, progress, deleteNonPackageFiles, cancellationToken);



    /// <summary>
    /// Uninstalls all files specified in the package. This leaves non-package files in place
    /// </summary>
    /// <param name="cloudUri">https path to package.json</param>
    /// <param name="installDirectory">The <see cref="DirectoryInfo"/> where the package is installed</param>
    /// <param name="progress">Optional <see cref="IProgress{InstallProgress}"/> to track uninstall progress</param>
    /// <param name="cancellationToken"></param>
    public static Task UnInstall(Uri cloudUri, DirectoryInfo installDirectory, IProgress<InstallProgress> progress = null, CancellationToken cancellationToken = default) =>
        Installer.UnInstall(_defaultClient.Value, cloudUri, installDirectory, progress, cancellationToken);



    /// <summary>
    /// Uninstalls all files specified in the package using the supplied <see cref="HttpClient"/>. This leaves non-package files in place
    /// </summary>
    /// <param name="client"><see cref="HttpClient"/> to use for downloading files. Usefull if you need ot configure credentials</param>
    /// <param name="cloudUri">https path to package.json</param>
    /// <param name="installDirectory">The <see cref="DirectoryInfo"/> where the package is installed</param>
    /// <param name="progress">Optional <see cref="IProgress{InstallProgress}"/> to track uninstall progress</param>
    /// <param name="cancellationToken"></param>
    public static Task UnInstall(HttpClient client, Uri cloudUri, DirectoryInfo installDirectory, IProgress<InstallProgress> progress = null, CancellationToken cancellationToken = default) =>
        Installer.UnInstall(client, cloudUri, installDirectory, progress, cancellationToken);
}
