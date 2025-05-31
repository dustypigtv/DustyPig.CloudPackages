using System;
using System.IO;
using System.Security.Cryptography;

namespace DustyPig.CloudPackages;

static class SHA256Helper
{
    public static string Compute(Stream stream)
    {
        using SHA256 sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(stream));
    }
}