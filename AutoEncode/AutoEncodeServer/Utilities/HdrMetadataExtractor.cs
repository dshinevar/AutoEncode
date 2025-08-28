using AutoEncodeServer.Utilities.Interfaces;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Process;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Utilities;

public class HdrMetadataExtractor : IHdrMetadataExtractor
{
    public ILogger Logger { get; set; }

    public IProcessExecutor ProcessExecutor { get; set; }

    public async Task<ProcessResult<string>> Extract(string sourceFileFullPath, HDRFlags hdrFlag, CancellationToken cancellationToken)
    {
        switch (hdrFlag)
        {
            case HDRFlags.HDR10PLUS:
            {
                return await ExtractHdr10PlusMetadata(sourceFileFullPath, cancellationToken);
            }
            case HDRFlags.DOLBY_VISION:
            {
                return await ExtractDolbyVisionMetadata(sourceFileFullPath, cancellationToken);
            }
            default:
            {
                string msg = "Unsupported HDR Type for extraction.";
                Logger.LogError(msg, details: new { HDRFlag = hdrFlag });
                return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
            }
        }
    }

    private async Task<ProcessResult<string>> ExtractHdr10PlusMetadata(string sourceFileFullPath, CancellationToken cancellationToken)
    {
        string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(sourceFileFullPath).Replace('\'', ' ')}.json";
        string hdr10PlusToolProcessFileName = string.IsNullOrWhiteSpace(State.Hdr10Plus.Hdr10PlusToolFullPath) ? Lookups.HDR10PlusToolExecutable
                                                                                                        : State.Hdr10Plus.Hdr10PlusToolFullPath;
        string processFileName;
        string processArgs;

        // If source file is an .mkv file, we can do a simpler approach with hdr10plus_tool
        bool isMkvFile = sourceFileFullPath.EndsWith("mkv");
        if (isMkvFile)
        {
            processFileName = hdr10PlusToolProcessFileName;
            processArgs = $"extract \"{sourceFileFullPath}\" -o \"{metadataOutputFile}\"";
        }
        else
        {
            if (State.IsLinuxEnvironment)
            {
                processFileName = "/bin/bash";

                string extractorArgs = $"'{hdr10PlusToolProcessFileName}' extract -o '{metadataOutputFile}' - ";
                processArgs = $"-c \"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)} -nostdin -i '{sourceFileFullPath.Replace("'", "'\\''")}' -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }
            else
            {
                processFileName = "cmd";

                string extractorArgs = $"\"{hdr10PlusToolProcessFileName}\" extract -o \"{metadataOutputFile}\" - ";
                processArgs = $"/C \"\"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)}\" -i \"{sourceFileFullPath}\" -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }
        }

        ProcessResult result = await ProcessExecutor.ExecuteAsync(new()
        {
            FileName = processFileName,
            Arguments = processArgs,
        }, cancellationToken);

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
                Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, hdr10PlusToolProcessFileName, State.Hdr10Plus.Hdr10PlusToolFullPath, metadataOutputFile, HDR10PlusProcessArgs = processArgs });
                return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
            }
        }
        else
        {
            string msg = "HDR10+ Metadata file was not created/does not exist.";
            Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, hdr10PlusToolProcessFileName, State.Hdr10Plus.Hdr10PlusToolFullPath, metadataOutputFile, HDR10PlusProcessArgs = processArgs });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
        }
    }

    private async Task<ProcessResult<string>> ExtractDolbyVisionMetadata(string sourceFileFullPath, CancellationToken cancellationToken)
    {
        string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(sourceFileFullPath).Replace('\'', ' ')}.RPU.bin";
        string doviToolProcessFileName = string.IsNullOrWhiteSpace(State.DolbyVision.DoviToolFullPath) ? Lookups.DoviToolExecutable
                                                                                                : State.DolbyVision.DoviToolFullPath;
        string processFileName;
        string processArgs;

        // If source file is an .mkv file, we can do a simpler approach with dovi_tool
        // TODO: Investigate ffmpeg -dolby_vision 1 argument
        bool isMkvFile = sourceFileFullPath.EndsWith("mkv");
        if (isMkvFile)
        {
            processFileName = doviToolProcessFileName;
            processArgs = $"extract-rpu \"{sourceFileFullPath}\" -o \"{metadataOutputFile}\"";
        }
        else
        {
            if (State.IsLinuxEnvironment)
            {
                processFileName = "/bin/bash";

                string extractorArgs = $"'{doviToolProcessFileName}' extract-rpu -o '{metadataOutputFile}' - ";
                processArgs = $"-c \"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)} -nostdin -i '{sourceFileFullPath.Replace("'", "'\\''")}' -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }
            else
            {
                processFileName = "cmd";

                string extractorArgs = $"\"{doviToolProcessFileName}\" extract-rpu -o \"{metadataOutputFile}\" - ";
                processArgs = $"/C \"\"{Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable)}\" -i \"{sourceFileFullPath}\" -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }
        }

        ProcessResult result = await ProcessExecutor.ExecuteAsync(new()
        {
            FileName = processFileName,
            Arguments = processArgs,
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (result.Status == ProcessResultStatus.Failure)
        {
            string msg = "DolbyVision metadata extraction process failed.";
            Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, doviToolProcessFileName, State.DolbyVision.DoviToolFullPath, metadataOutputFile, DoviToolArguments = processArgs });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
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
                Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, doviToolProcessFileName, State.DolbyVision.DoviToolFullPath, metadataOutputFile, DoviToolArguments = processArgs });
                return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
            }
        }
        else
        {
            string msg = "DolbyVision Metadata file was not created/does not exist.";
            Logger.LogError(msg, nameof(HdrMetadataExtractor), new { sourceFileFullPath, doviToolProcessFileName, State.DolbyVision.DoviToolFullPath, metadataOutputFile, DoviToolArguments = processArgs });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
        }
    }
}
