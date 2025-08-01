using AutoEncodeServer.Utilities.Interfaces;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Process;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoEncodeServer.Utilities;

public class HdrMetadataExtractor : IHdrMetadataExtractor
{
    public ILogger Logger { get; set; }

    public ProcessResult<string> Extract(string sourceFileFullPath, HDRFlags hdrFlag, CancellationToken cancellationToken)
    {
        switch (hdrFlag)
        {
            case HDRFlags.HDR10PLUS:
            {
                return ExtractHdr10PlusMetadata(sourceFileFullPath, cancellationToken);
            }
            case HDRFlags.DOLBY_VISION:
            {
                return ExtractDolbyVisionMetadata(sourceFileFullPath, cancellationToken);
            }
            default:
            {
                string msg = "Unsupported HDR Type for extraction.";
                Logger.LogError(msg, details: new { HDRFlag = hdrFlag });
                return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
            }
        }
    }

    private ProcessResult<string> ExtractHdr10PlusMetadata(string sourceFileFullPath, CancellationToken cancellationToken)
    {
        string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(sourceFileFullPath).Replace('\'', ' ')}.json";

        Process hdrMetadataProcess = null;
        CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
        {
            hdrMetadataProcess?.Kill(true);
        });

        try
        {
            string hdr10PlusToolProcessFileName = string.IsNullOrWhiteSpace(State.Hdr10Plus.Hdr10PlusToolFullPath) ? Lookups.HDR10PlusToolExecutable
                                                                                                                    : State.Hdr10Plus.Hdr10PlusToolFullPath;

            // If source file is an .mkv file, we can do a simpler approach with hdr10plus_tool
            bool isMkvFile = sourceFileFullPath.EndsWith("mkv");
            if (isMkvFile)
            {
                string args = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? $"extract '{sourceFileFullPath}' -o '{metadataOutputFile}'"
                                                                                : $"extract \"{sourceFileFullPath}\" -o \"{metadataOutputFile}\"";

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = hdr10PlusToolProcessFileName,
                    Arguments = args,
                    UseShellExecute = false
                };

                using (hdrMetadataProcess = new())
                {
                    hdrMetadataProcess.StartInfo = startInfo;
                    hdrMetadataProcess.Start();
                    hdrMetadataProcess.WaitForExit();
                }
            }
            else
            {
                string ffmpegArgs;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    string extractorArgs = $"'{hdr10PlusToolProcessFileName}' extract -o '{metadataOutputFile}' - ";

                    ffmpegArgs = $"-c \"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)} -nostdin -i '{sourceFileFullPath.Replace("'", "'\\''")}' -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
                }
                else
                {
                    string extractorArgs = $"\"{hdr10PlusToolProcessFileName}\" extract -o \"{metadataOutputFile}\" - ";

                    ffmpegArgs = $"/C \"\"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)}\" -i \"{sourceFileFullPath}\" -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
                }

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/bin/bash" : "cmd",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false
                };

                using (hdrMetadataProcess = new())
                {
                    hdrMetadataProcess.StartInfo = startInfo;
                    hdrMetadataProcess.Start();
                    hdrMetadataProcess.WaitForExit();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error creating HDR10+ metadata file for {sourceFileFullPath}", details: new { State.Hdr10Plus.Hdr10PlusToolFullPath });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, "Exception occurred while extracting HDR10+ metadata.");
        }
        finally
        {
            tokenRegistration.Unregister();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (File.Exists(metadataOutputFile))
        {
            FileInfo metadataFileInfo = new(metadataOutputFile);

            if (metadataFileInfo.Length > 0)
            {
                return new ProcessResult<string>(metadataOutputFile, ProcessResultStatus.Success, "Successfully extracted HDR10+ metadata.");
            }
            else
            {
                return new ProcessResult<string>(null, ProcessResultStatus.Failure, "HDR10+ Metadata file was created but is empty.");
            }
        }
        else
        {
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, "HDR10+ Metadata file was not created/does not exist.");
        }
    }

    private ProcessResult<string> ExtractDolbyVisionMetadata(string sourceFileFullPath, CancellationToken cancellationToken)
    {
        string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(sourceFileFullPath).Replace('\'', ' ')}.RPU.bin";

        Process hdrMetadataProcess = null;
        CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
        {
            hdrMetadataProcess?.Kill(true);
        });

        try
        {
            string doviToolProcessFileName = string.IsNullOrWhiteSpace(State.DolbyVision.DoviToolFullPath) ? Lookups.DoviToolExecutable
                                                                                                            : State.DolbyVision.DoviToolFullPath;

            // If source file is an .mkv file, we can do a simpler approach with dovi_tool
            // TODO: Investigate ffmpeg -dolby_vision 1 argument
            bool isMkvFile = sourceFileFullPath.EndsWith("mkv");
            if (isMkvFile)
            {
                string args = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? $"extract-rpu '{sourceFileFullPath}' -o '{metadataOutputFile}'"
                                                                                : $"extract-rpu \"{sourceFileFullPath}\" -o \"{metadataOutputFile}\"";

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = doviToolProcessFileName,
                    Arguments = args,
                    UseShellExecute = false
                };

                using (hdrMetadataProcess = new())
                {
                    hdrMetadataProcess.StartInfo = startInfo;
                    hdrMetadataProcess.Start();
                    hdrMetadataProcess.WaitForExit();
                }
            }
            else
            {
                string ffmpegArgs;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    string extractorArgs = $"'{doviToolProcessFileName}' extract-rpu -o '{metadataOutputFile}' - ";

                    ffmpegArgs = $"-c \"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)} -nostdin -i '{sourceFileFullPath.Replace("'", "'\\''")}' -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
                }
                else
                {
                    string extractorArgs = $"\"{doviToolProcessFileName}\" extract-rpu -o \"{metadataOutputFile}\" - ";

                    ffmpegArgs = $"/C \"\"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)}\" -i \"{sourceFileFullPath}\" -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
                }

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/bin/bash" : "cmd",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false
                };

                using (hdrMetadataProcess = new())
                {
                    hdrMetadataProcess.StartInfo = startInfo;
                    hdrMetadataProcess.Start();
                    hdrMetadataProcess.WaitForExit();
                }
            }

        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error creating DolbyVision metadata file for {sourceFileFullPath}", details: new { State.DolbyVision.DoviToolFullPath });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, "Exception occurred while extracting DolbyVision metadata.");
        }
        finally
        {
            tokenRegistration.Unregister();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (File.Exists(metadataOutputFile))
        {
            FileInfo metadataFileInfo = new(metadataOutputFile);

            if (metadataFileInfo.Length > 0)
            {
                return new ProcessResult<string>(metadataOutputFile, ProcessResultStatus.Success, "Successfully extracted DolbyVision metadata.");
            }
            else
            {
                return new ProcessResult<string>(null, ProcessResultStatus.Failure, "DolbyVision Metadata file was created but is empty.");
            }
        }
        else
        {
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, "DolbyVision Metadata file was not created/does not exist.");
        }
    }
}
