using System;
using System.IO;
using System.Runtime.InteropServices;
using PortAudioSharp;
using StardewModdingAPI;

namespace ProximityVoiceChat.Audio;

/// <summary>
/// Resolves PortAudio's native library from the mod folder. SMAPI runs mods without a
/// runtime identifier, so the package's runtimes/&lt;rid&gt;/native layout is not probed.
/// </summary>
public static class NativeLibraryResolver
{
    private static bool _registered;
    private static bool _loggedLoadedPath;

    public static bool Register(IMonitor monitor, string modDirectory)
    {
        if (_registered)
            return true;

        try
        {
            NativeLibrary.SetDllImportResolver(typeof(PortAudio).Assembly, (libraryName, assembly, searchPath) =>
            {
                if (libraryName.IndexOf("portaudio", StringComparison.OrdinalIgnoreCase) < 0)
                    return IntPtr.Zero;

                string? rid = GetRuntimeIdentifier();
                if (rid is null)
                    return IntPtr.Zero;

                string fileName = GetFileName();
                string[] candidates =
                {
                    Path.Combine(modDirectory, "lib", rid, fileName),
                    Path.Combine(modDirectory, "runtimes", rid, "native", fileName),
                    Path.Combine(modDirectory, fileName)
                };

                foreach (string candidate in candidates)
                {
                    if (!File.Exists(candidate))
                        continue;

                    try
                    {
                        IntPtr handle = NativeLibrary.Load(candidate);
                        if (!_loggedLoadedPath)
                        {
                            _loggedLoadedPath = true;
                            monitor.Log($"Loaded native PortAudio library '{candidate}'.", LogLevel.Debug);
                        }

                        return handle;
                    }
                    catch (Exception ex)
                    {
                        monitor.Log($"Found native PortAudio library '{candidate}' but could not load it: {ex.Message}", LogLevel.Error);
                        return IntPtr.Zero;
                    }
                }

                monitor.Log(
                    $"Could not find native PortAudio for {rid}; expected '{fileName}' under '{Path.Combine(modDirectory, "lib", rid)}'. Voice chat will be disabled. " +
                    "Reinstall the mod without removing its lib folder.",
                    LogLevel.Error);
                return IntPtr.Zero;
            });

            _registered = true;
            return true;
        }
        catch (Exception ex)
        {
            monitor.Log($"Could not install the PortAudio native-library resolver: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    private static string? GetRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win-x64";

        string architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            _ => ""
        };

        if (architecture.Length == 0)
            return null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"osx-{architecture}";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"linux-{architecture}";

        return null;
    }

    private static string GetFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "portaudio.dll";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "libportaudio.dylib";

        return "libportaudio.so";
    }
}
