using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DustyPig.CloudPackages;

class Package
{
    static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.General) { WriteIndented = true };

    public string Name { get; set; }

    public string Version { get; set; }

    public List<PackageFile> Files { get; set; }

    public void Save(FileInfo file)
    {
        file.Directory.Create();
        File.WriteAllText(file.FullName, JsonSerializer.Serialize(this, options));
    }
}