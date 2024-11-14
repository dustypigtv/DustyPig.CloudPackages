using System.IO;

namespace DustyPig.CloudPackages;

class PackageFile
{
    public string RelativePath { get; set; }

    public long FileSize { get; set; }

    public string SHA256 { get; set; }

    public bool Installed(DirectoryInfo root)
    {
        var file = new FileInfo(Path.Combine(root.FullName, RelativePath));
        if (!file.Exists)
            return false;

        if (file.Length != FileSize)
            return false;

        using var fs = file.OpenRead();
        var sha256 = SHA256Helper.Compute(fs);
        return sha256 == SHA256;
    }

    public static PackageFile Create(DirectoryInfo root, FileInfo file)
    {
        using var fs = file.OpenRead();
        var sha256 = SHA256Helper.Compute(fs);
        return new PackageFile
        {
            RelativePath = file.FullName[root.FullName.TrimEnd(Path.DirectorySeparatorChar).Length..].Replace(Path.DirectorySeparatorChar, '/').Trim('/'),
            FileSize = file.Length,
            SHA256 = sha256
        };
    }
}
