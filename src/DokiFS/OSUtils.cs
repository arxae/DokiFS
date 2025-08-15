using System.Diagnostics;
using DokiFS.Exceptions;

namespace DokiFS;

internal static class OSUtils
{
    static bool lsofExists;
    static bool lsofChecked;

    static bool CheckLsof()
    {
        if (lsofChecked) return lsofExists;

        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "lsof",
                Arguments = "-v",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process proc = Process.Start(psi);
            if (proc == null) return false;

            proc.WaitForExit();
            lsofExists = proc.ExitCode == 0;
        }
        catch (Exception)
        {
            lsofExists = false;
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
            FileName = "lsof",
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
