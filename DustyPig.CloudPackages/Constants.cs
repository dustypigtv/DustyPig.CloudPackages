namespace DustyPig.CloudPackages;

static class Constants
{
    public const string PACKAGE_FILE_EXT = ".dpcp";

    //4096 is STILL the file stream default buffer size in .net9 in 2025
    //System.IO.File::DefaultBufferSize (internal constant)
    public const int FILE_BUFFER_SIZE = 4096;
}
