
using System.Runtime.InteropServices;

namespace Shiron.Utils;

/// <summary>Utility functions for platform detection.</summary>
public static class PlatformUtils {
    /// <summary>Gets a string key representing the current platform.</summary>
    public static string GetPlatformKey() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "osx";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return "freebsd";
        throw new PlatformNotSupportedException("Unable to determine platform!");
    }

    /// <summary>Gets a string key representing the current architecture.</summary>
    public static string GetArchitectureKey() {
        return RuntimeInformation.OSArchitecture.ToString().ToLower();
    }
}
