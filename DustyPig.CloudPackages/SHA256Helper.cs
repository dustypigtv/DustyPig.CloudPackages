using System;
using System.IO;

namespace DustyPig.CloudPackages;

class SHA256Helper
{
    public static string Compute(Stream stream)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "");
    }
}