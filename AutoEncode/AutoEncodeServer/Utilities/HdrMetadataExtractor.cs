using AutoEncodeServer.Utilities.Interfaces;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Process;
using System;
using System.Diagnostics;
using System.IO;
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
        string processArgs;

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
                processArgs = State.IsLinuxEnvironment ? $"extract '{sourceFileFullPath}' -o '{metadataOutputFile}'"
                                                        : $"extract \"{sourceFileFullPath}\" -o \"{metadataOutputFile}\"";

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = hdr10PlusToolProcessFileName,
                    Arguments = processArgs,
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
                if (State.IsLinuxEnvironment)
                {
                    string extractorArgs = $"'{hdr10PlusToolProcessFileName}' extract -o '{metadataOutputFile}' - ";

                    processArgs = $"-c \"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)} -nostdin -i '{sourceFileFullPath.Replace("'", "'\\''")}' -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
                }
                else
                {
                    string extractorArgs = $"\"{hdr10PlusToolProcessFileName}\" extract -o \"{metadataOutputFile}\" - ";

                    processArgs = $"/C \"\"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)}\" -i \"{sourceFileFullPath}\" -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
                }

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = State.IsLinuxEnvironment ? "/bin/bash" : "cmd",
                    Arguments = processArgs,
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
                string msg = "HDR10+ Metadata file was created but is empty.";
                Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, State.Hdr10Plus.Hdr10PlusToolFullPath, metadataOutputFile, HDR10PlusProcessArgs = processArgs });
                return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
            }
        }
        else
        {
            string msg = "HDR10+ Metadata file was not created/does not exist.";
            Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, State.Hdr10Plus.Hdr10PlusToolFullPath, metadataOutputFile, HDR10PlusProcessArgs = processArgs });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
        }
    }

    private ProcessResult<string> ExtractDolbyVisionMetadata(string sourceFileFullPath, CancellationToken cancellationToken)
    {
        string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(sourceFileFullPath).Replace('\'', ' ')}.RPU.bin";
        string doviToolProcessFileName = string.IsNullOrWhiteSpace(State.DolbyVision.DoviToolFullPath) ? Lookups.DoviToolExecutable
                                                                                                : State.DolbyVision.DoviToolFullPath;
        string processArgs;

        Process hdrMetadataProcess = null;
        int? exitCode = null;
        bool processStarted = false;
        CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
        {
            hdrMetadataProcess?.Kill(true);
        });

        try
        {
            // If source file is an .mkv file, we can do a simpler approach with dovi_tool
            // TODO: Investigate ffmpeg -dolby_vision 1 argument
            bool isMkvFile = sourceFileFullPath.EndsWith("mkv");
            if (isMkvFile)
            {
                processArgs = State.IsLinuxEnvironment ? $"extract-rpu '{sourceFileFullPath}' -o '{metadataOutputFile}'"
                                                                                : $"extract-rpu \"{sourceFileFullPath}\" -o \"{metadataOutputFile}\"";

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = State.IsLinuxEnvironment ? "/bin/bash" : "cmd",
                    Arguments = $"{doviToolProcessFileName} {processArgs}",
                    UseShellExecute = false
                };

                using (hdrMetadataProcess = new())
                {
                    hdrMetadataProcess.StartInfo = startInfo;
                    hdrMetadataProcess.EnableRaisingEvents = true;
                    hdrMetadataProcess.Exited += (sender, e) =>
                    {
                        if (sender is Process proc)
                            exitCode = proc.ExitCode;
                    };
                    processStarted = hdrMetadataProcess.Start();
                    hdrMetadataProcess.WaitForExit();
                }
            }
            else
            {
                if (State.IsLinuxEnvironment)
                {
                    string extractorArgs = $"'{doviToolProcessFileName}' extract-rpu -o '{metadataOutputFile}' - ";

                    processArgs = $"-c \"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)} -nostdin -i '{sourceFileFullPath.Replace("'", "'\\''")}' -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
                }
                else
                {
                    string extractorArgs = $"\"{doviToolProcessFileName}\" extract-rpu -o \"{metadataOutputFile}\" - ";

                    processArgs = $"/C \"\"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)}\" -i \"{sourceFileFullPath}\" -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
                }

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = State.IsLinuxEnvironment ? "/bin/bash" : "cmd",
                    Arguments = processArgs,
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
                string msg = "DolbyVision Metadata file was created but is empty.";
                Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, doviToolProcessFileName, State.DolbyVision.DoviToolFullPath, metadataOutputFile, DoviToolArguments = processArgs, ProcessExitCode = exitCode });
                return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
            }
        }
        else
        {
            string msg = "DolbyVision Metadata file was not created/does not exist.";
            Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, doviToolProcessFileName, State.DolbyVision.DoviToolFullPath, metadataOutputFile, DoviToolArguments = processArgs, ProcessExitCode = exitCode, ProcessStarted = processStarted });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
        }
    }
}
