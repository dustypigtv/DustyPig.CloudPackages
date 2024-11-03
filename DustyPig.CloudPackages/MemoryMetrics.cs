using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DustyPig.CloudPackages;

class MemoryMetrics
{
    public double Total { get; private set; }

    public double Used { get; private set; }

    public double Free { get; private set; }



    public static MemoryMetrics Get() => IsUnix() ? GetUnixMetrics() : GetWindowsMetrics();




    static bool IsUnix() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    static MemoryMetrics GetWindowsMetrics()
    {
        var output = "";
        var info = new ProcessStartInfo
        {
            FileName = "wmic",
            Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value",
            RedirectStandardOutput = true
        };

        using (var process = Process.Start(info))
        {
            output = process.StandardOutput.ReadToEnd();
        }

        var lines = output.Trim().Split("\n");
        var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
        var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

        //wmic gives memory in Kb, multiply by 1024 to get bytes
        var metrics = new MemoryMetrics
        {
            Total = double.Parse(totalMemoryParts[1]) * 1024,
            Free = double.Parse(freeMemoryParts[1]) * 1024
        };
        metrics.Used = metrics.Total - metrics.Free;

        return metrics;
    }

    static MemoryMetrics GetUnixMetrics()
    {
        var output = "";
        var info = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = "-c \"free -b\"",
            RedirectStandardOutput = true
        };

        using (var process = Process.Start(info))
        {
            output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
        }

        var lines = output.Split("\n");
        var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

        var metrics = new MemoryMetrics
        {
            Total = double.Parse(memory[1]),
            Used = double.Parse(memory[2]),
            Free = double.Parse(memory[3])
        };

        return metrics;
    }

}