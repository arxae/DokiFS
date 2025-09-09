using System.Diagnostics;
using DokiFS.Exceptions;

namespace DokiFS.Internal;

/// <summary>
/// OS Specific utilities
/// </summary>
public static class OSUtils
{
    static bool lsofExists;
    static bool lsofChecked;
    static string lsofPath;
    static readonly List<string> lsofPaths = [
        "/usr/bin/lsof",        // Most common location on Linux and macOS
        "/usr/sbin/lsof",       // Alternative location (Mac)
        "/bin/lsof"            // Alternative location
    ];

    /// <summary>
    /// Adds a custom path to the list of locations to check for lsof.
    /// </summary>
    /// <param name="path"></param>
    public static void AddLsofPath(string path)
    {
        if (lsofPaths.Contains(path) == false)
        {
            lsofPaths.Insert(0, path);
            lsofChecked = false;
        }
    }

    /// <summary>
    /// Removes a custom path from the list of locations to check for lsof.
    /// </summary>
    /// <param name="path"></param>
    public static void RemoveLsofPath(string path)
    {
        lsofPaths.Remove(path);
        lsofChecked = false;
    }

    static bool CheckLsof()
    {
        if (lsofChecked) return lsofExists;

        foreach (string path in lsofPaths)
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = path,
                    Arguments = "-v",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process proc = Process.Start(psi);
                if (proc == null) return false;

                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    lsofPath = path;
                    lsofExists = true;
                    break;
                }
            }
            catch (Exception)
            {
                lsofExists = false;
            }
        }

        lsofChecked = true;

        return lsofExists;
    }

    internal static bool UnixFileInUse(string physicalPath)
    {
        if (CheckLsof() == false)
        {
            throw new LsofNotFoundException();
        }

        ProcessStartInfo psi = new()
        {
            FileName = lsofPath,
            Arguments = $"-w -- \"{physicalPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process proc = Process.Start(psi);

            if (proc == null)
            {
                return false;
            }

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            // lsof exit codes:
            // 0: All files found and listed.
            // 1: Some files could not be processed (e.g., permission errors, or file not open by any process).
            // >1: A fatal error occurred.
            // If lsof exits with 1 AND output is empty (or just header), file is likely not open by others.
            // If lsof exits with 0 AND output (beyond header) shows our file, it's in use.

            if (proc.ExitCode > 1)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                // No output means no processes are using the file
                return false;
            }

            string[] lines = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1) // Header + data lines
            {
                for (int i = 1; i < lines.Length; i++) // Skip header
                {
                    if (lines[i].Contains(physicalPath))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception)
        {
            // Other unexpected errors running lsof, assume file is in use for safety
            return false;
        }
    }
}
