using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AutoEncodeServer;

internal partial class AutoEncodeServer
{
    /// <summary>Gets FFmpeg version/Checks to make sure FFmpeg is accessible </summary>
    /// <param name="ffmpegDirectory">FFmpeg directory from config</param>
    /// <returns>List of strings from version output (for logging)</returns>
    static List<string> GetFFmpegVersion(string ffmpegDirectory)
    {
        try
        {
            List<string> ffmpegVersionLines = [];

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(ffmpegDirectory, "ffmpeg"),
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
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>Gets mkvmerge version </summary>
    /// <returns>mkvmerge version string</returns>
    static string GetMKVMergeVersion(string mkvMergeFullPath)
    {
        try
        {
            string mkvMergeVersion = string.Empty;

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = string.IsNullOrWhiteSpace(mkvMergeFullPath) ? "mkvmerge" : mkvMergeFullPath,
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
        catch (Exception)
        {
            throw;
        }
    }

    static List<string> Getx265Version(string x265FullPath)
    {
        try
        {
            List<string> x265Version = [];

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = x265FullPath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using (Process x265Process = new())
            {
                x265Process.StartInfo = startInfo;
                x265Process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data)) x265Version.Add(e.Data.Replace("x265 [info]: ", string.Empty)); // Only expecting one line
                };
                x265Process.Start();
                x265Process.BeginErrorReadLine();
                x265Process.WaitForExit();
            }

            return x265Version;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
