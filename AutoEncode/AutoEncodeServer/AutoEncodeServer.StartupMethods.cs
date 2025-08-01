using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AutoEncodeServer;

// STARTUP HELPER METHODS
internal partial class AutoEncodeServer
{
    /// <summary>Gets FFmpeg version/Checks to make sure FFmpeg is accessible </summary>
    /// <param name="ffmpegDirectory">FFmpeg directory from config</param>
    /// <returns>List of strings from version output (for logging)</returns>
    static List<string> CheckForFfmpeg(string ffmpegDirectory)
    {
        List<string> ffmpegVersionLines = [];
        string commandFileName = Lookups.FFmpegExecutable;

        // If provided an ffmpeg directory, check to see if file is there
        // If it is, try to use it
        if (string.IsNullOrWhiteSpace(ffmpegDirectory) is false)
        {
            string ffmpegFullPath = Path.Combine(ffmpegDirectory, commandFileName);
            if (File.Exists(ffmpegFullPath))
                commandFileName = ffmpegFullPath;
        }

        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = commandFileName,
            Arguments = "-version",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        using (Process ffprobeProcess = new())
        {
            ffprobeProcess.StartInfo = startInfo;
            ffprobeProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) ffmpegVersionLines.Add(e.Data);
            };
            ffprobeProcess.Start();
            ffprobeProcess.BeginOutputReadLine();
            ffprobeProcess.WaitForExit();
        }

        return ffmpegVersionLines;
    }

    /// <summary>Gets ffprobe version/Checks to make sure ffprobe is accessible </summary>
    /// <param name="ffprobeDirectory">ffprobe directory location</param>
    /// <returns>List of strings from version output (for logging)</returns>
    static List<string> CheckForFfprobe(string ffprobeDirectory)
    {
        List<string> ffprobeVersionLines = [];
        string commandFileName = Lookups.FFprobeExecutable;

        // If provided an ffprobe directory, check to see if file is there
        // If it is, try to use it
        if (string.IsNullOrWhiteSpace(ffprobeDirectory) is false)
        {
            string ffprobeFullPath = Path.Combine(ffprobeDirectory, commandFileName);
            if (File.Exists(ffprobeFullPath))
                commandFileName = ffprobeFullPath;
        }

        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = commandFileName,
            Arguments = "-version",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        using (Process ffprobeProcess = new())
        {
            ffprobeProcess.StartInfo = startInfo;
            ffprobeProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) ffprobeVersionLines.Add(e.Data);
            };
            ffprobeProcess.Start();
            ffprobeProcess.BeginOutputReadLine();
            ffprobeProcess.WaitForExit();
        }

        return ffprobeVersionLines;
    }

    /// <summary>Checks to see if hdr10plus_tool is installed and returns the version. </summary>
    /// <param name="hdr10PlusToolFullPath">The full path to hdr10plus_tool</param>
    /// <returns>Version string of hdr10plus_tool</returns>
    static string CheckForHdr10PlusTool(string hdr10PlusToolFullPath)
    {
        string hdr10PlusVersion = null;
        string commandFileName = "hdr10plus_tool";

        // If provided a hdr10plus_tool directory, check to see if file is there
        // If it is, try to use it
        if (string.IsNullOrWhiteSpace(hdr10PlusToolFullPath) is false
            && File.Exists(hdr10PlusToolFullPath))
        {
            commandFileName = hdr10PlusToolFullPath;
        }

        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = commandFileName,
            Arguments = "-V",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        using (Process hdr10PlusToolCheckProcess = new())
        {
            hdr10PlusToolCheckProcess.StartInfo = startInfo;
            hdr10PlusToolCheckProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) hdr10PlusVersion = e.Data.Trim();   // Only expecting one line
            };
            hdr10PlusToolCheckProcess.Start();
            hdr10PlusToolCheckProcess.BeginOutputReadLine();
            hdr10PlusToolCheckProcess.WaitForExit();
        }

        return hdr10PlusVersion;
    }

    /// <summary>Checks to see if dovi_tool is installed and returns the version. </summary>
    /// <param name="doviToolFullPath">The full path to dovi_tool</param>
    /// <returns>Version string of dovi_tool</returns>
    static string CheckForDoviTool(string doviToolFullPath)
    {
        string doviToolVersion = null;
        string commandFileName = "dovi_tool";

        // If provided a dovi_tool path, check to see if file is there
        // If it is, try to use it
        if (string.IsNullOrWhiteSpace(doviToolFullPath) is false
            && File.Exists(doviToolFullPath))
        {
            commandFileName = doviToolFullPath;
        }

        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = commandFileName,
            Arguments = "-V",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        using (Process doviToolCheckProcess = new())
        {
            doviToolCheckProcess.StartInfo = startInfo;
            doviToolCheckProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) doviToolVersion = e.Data.Trim();   // Only expecting one line
            };
            doviToolCheckProcess.Start();
            doviToolCheckProcess.BeginOutputReadLine();
            doviToolCheckProcess.WaitForExit();
        }

        return doviToolVersion;
    }

    static List<string> CheckForX265(string x265FullPath)
    {
        List<string> x265Version = [];
        string commandFileName = "x265";

        // If provided a x265 path, check to see if file is there
        // If it is, try to use it
        if (string.IsNullOrWhiteSpace(x265FullPath) is false
            && File.Exists(x265FullPath))
        {
            commandFileName = x265FullPath;
        }

        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = commandFileName,
            Arguments = "--version",
            UseShellExecute = false,
            RedirectStandardError = true
        };

        using (Process x265Process = new())
        {
            x265Process.StartInfo = startInfo;
            x265Process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) x265Version.Add(e.Data.Replace("x265 [info]: ", string.Empty));
            };
            x265Process.Start();
            x265Process.BeginErrorReadLine();
            x265Process.WaitForExit();
        }

        return x265Version;
    }

    /// <summary>Gets mkvmerge version </summary>
    /// <returns>mkvmerge version string</returns>
    static string CheckForMkvMerge(string mkvMergeFullPath)
    {
        string mkvMergeVersion = string.Empty;
        string commandFileName = "mkvmerge";

        // If provided a mkvmerge path, check to see if file is there
        // If it is, try to use it
        if (string.IsNullOrWhiteSpace(mkvMergeFullPath) is false
            && File.Exists(mkvMergeFullPath))
        {
            commandFileName = mkvMergeFullPath;
        }

        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = commandFileName,
            Arguments = "--version",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        using (Process mkvMergeProcess = new())
        {
            mkvMergeProcess.StartInfo = startInfo;
            mkvMergeProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) mkvMergeVersion = e.Data; // Only expecting one line
            };
            mkvMergeProcess.Start();
            mkvMergeProcess.BeginOutputReadLine();
            mkvMergeProcess.WaitForExit();
        }

        return mkvMergeVersion;
    }
}
