using AutoEncodeServer.Data;
using AutoEncodeServer.Utilities.Data;
using AutoEncodeServer.Utilities.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Communication;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Stream = AutoEncodeServer.Data.Stream;

namespace AutoEncodeServer.Utilities;

/// <summary>Processor that is used to determine info about a source file.</summary>
public partial class SourceFileProcessor : ISourceFileProcessor
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex VideoScanRegex();

    public ILogger Logger { get; set; }

    public IProcessExecutor ProcessExecutor { get; set; }

    #region Probe
    public ProcessResult<SourceFileProbeResultData> Probe(string sourceFileFullPath)
    {
        // FFprobe
        (ProbeData probeData, string message) = FFProbe(sourceFileFullPath);

        // If null, error occurred
        if (probeData is null)
        {
            Logger.LogError(["Error occurred while probing source file.", message], nameof(SourceFileProcessor), new { sourceFileFullPath });
            return new ProcessResult<SourceFileProbeResultData>(null, ProcessResultStatus.Failure, message);
        }

        int numberOfFrames = 0;
        VideoStreamData videoStreamData = null;
        DolbyVisionInfo dolbyVisionInfo = null;
        List<AudioStreamData> audioStreams = null;
        List<SubtitleStreamData> subtitleStreams = null;

        try
        {
            // Sift through all the streams
            short audioIndex = 0;
            short subIndex = 0;
            foreach (Stream stream in probeData.Streams)
            {
                switch (stream.CodecType)
                {
                    case CodecType.Video:
                    {
                        // Only support 1 video stream -- if somehow multiple, just move on
                        if (videoStreamData is not null)
                        {
                            Logger.LogWarning($"Detected multiple video streams in {sourceFileFullPath}");
                            break;
                        }

                        videoStreamData = new()
                        {
                            StreamIndex = stream.Index,
                            Resolution = $"{stream.Width}x{stream.Height}",
                            ResoultionInt = stream.Width * stream.Height,
                            CodecName = stream.CodecName,
                            CodecLongName = stream.CodecLongName,
                            Title = string.IsNullOrWhiteSpace(stream.Tags.Title) ? "Video" : stream.Tags.Title,
                            ChromaLocation = stream.ChromaLocation,
                            PixelFormat = stream.PixelFormat,
                            ColorPrimaries = stream.ColorPrimaries,
                            ColorSpace = stream.ColorSpace,
                            ColorTransfer = stream.ColorTransfer,
                        };

                        string frameRateString = string.IsNullOrWhiteSpace(stream.RFrameRate) ? (stream.AverageFrameRate ?? string.Empty) : stream.RFrameRate;
                        videoStreamData.FrameRate = frameRateString;

                        if (string.IsNullOrWhiteSpace(frameRateString) is false)
                        {
                            string[] frameRateStrings = frameRateString.Split("/");
                            if (double.TryParse(frameRateStrings[0], out double frameRateNumerator) && double.TryParse(frameRateStrings[1], out double frameRateDenominator))
                            {
                                double frameRate = frameRateNumerator / frameRateDenominator;
                                numberOfFrames = (int)(frameRate * probeData.Format.DurationInSeconds);
                                videoStreamData.CalculatedFrameRate = Math.Round(frameRate, 3);
                            }
                        }

                        // Video streams can have side data for dolby vision -- grab that info and set it aside
                        if ((stream.SideData?.Count ?? -1) > 0)
                        {
                            SideData doviConfigurationRecord = stream.SideData.FirstOrDefault(sd => sd.SideDataType.Equals("DOVI configuration record", StringComparison.OrdinalIgnoreCase));
                            if (doviConfigurationRecord is not null)
                            {
                                dolbyVisionInfo = new()
                                {
                                    Version = $"{doviConfigurationRecord.DolbyVisionVersionMajor}.{doviConfigurationRecord.DolbyVisionVersionMinor}",
                                    Profile = $"{doviConfigurationRecord.DolbyVisionProfile}.{doviConfigurationRecord.DolbyVisionLevel}",
                                    RPUPresent = doviConfigurationRecord.RPUPresent,
                                    ELPresent = doviConfigurationRecord.ELPresent,
                                    BLPresent = doviConfigurationRecord.BLPresent,
                                };
                            }
                        }

                        break;
                    }
                    case CodecType.Audio:
                    {
                        AudioStreamData audioStream = new()
                        {
                            StreamIndex = stream.Index,
                            AudioIndex = audioIndex,
                            Channels = stream.Channels,
                            Language = stream.Tags.Language,
                            CodecName = stream.CodecName,
                            CodecLongName = stream.CodecLongName,
                            Profile = stream.Profile ?? string.Empty,
                            Title = stream.Tags.Title,
                            HasDolbyAtmos = stream.Profile?.Contains("Atmos", StringComparison.OrdinalIgnoreCase) ?? false,
                            Commentary = stream.Disposition.Commentary || stream.Tags.Title.Contains("Commentary", StringComparison.OrdinalIgnoreCase)
                        };

                        if (string.IsNullOrWhiteSpace(stream.ChannelLayout) is false)
                            audioStream.ChannelLayout = stream.ChannelLayout;
                        else
                            audioStream.ChannelLayout = ConvertNumberOfChannelsToLayout(stream.Channels);

                        audioStreams ??= [];
                        audioStreams.Add(audioStream);
                        audioIndex++;
                        break;
                    }
                    case CodecType.Subtitle:
                    {
                        SubtitleStreamData subtitleStream = new()
                        {
                            StreamIndex = stream.Index,
                            SubtitleIndex = subIndex,
                            CodecName = stream.CodecName,
                            CodecLongName = stream.CodecLongName,
                            Language = stream.Tags.Language,
                            Forced = stream.Disposition.Forced,
                            Commentary = stream.Disposition.Forced || (stream.Tags.Title?.Contains("Commentary", StringComparison.OrdinalIgnoreCase) ?? false),
                            HearingImpaired = stream.Disposition.HearingImpaired || (stream.Tags.Title?.Contains("SDH", StringComparison.OrdinalIgnoreCase) ?? false),
                            Title = stream.Tags.Title ?? string.Empty
                        };

                        subtitleStreams ??= [];
                        subtitleStreams.Add(subtitleStream);
                        subIndex++;

                        break;
                    }
                    default:
                        break;
                }
            }

            // Validate the stream data
            // Error: No video, no audio, (No subtitles is ok)
            if (videoStreamData is null)
                throw new Exception("No video stream found.");
            if ((audioStreams?.Count ?? 0) < 1)
                throw new Exception("No audio streams found.");

            // Handle Frame data -- usually for HDR
            // Attempt to grab the first one -- can have multiple video ones but tend to have duplicate info for what we care about
            // Audio frames usually aren't useful so ignoring for now
            Frame videoFrame = probeData.Frames?
                                        .FirstOrDefault(f => f.MediaType.Equals("video", StringComparison.OrdinalIgnoreCase));

            if (videoFrame is not null)
            {
                // Fallback data checks -- if somehow we still don't have data for these, grab from frame data
                if (string.IsNullOrWhiteSpace(videoStreamData.PixelFormat))
                    videoStreamData.PixelFormat = videoFrame.PixelFormat;
                if (string.IsNullOrWhiteSpace(videoStreamData.ColorPrimaries))
                    videoStreamData.ColorPrimaries = videoFrame.ColorPrimaries;
                if (string.IsNullOrWhiteSpace(videoStreamData.ColorSpace))
                    videoStreamData.ColorSpace = videoFrame.ColorSpace;
                if (string.IsNullOrWhiteSpace(videoStreamData.ColorTransfer))
                    videoStreamData.ColorTransfer = videoFrame.ColorTransfer;

                videoStreamData.ChromaLocation ??= videoFrame.ChromaLocation;

                // Usually should have some kind of side data
                if ((videoFrame?.SideData?.Count ?? -1) > 0)
                {
                    SideData masteringDisplayMetadata = videoFrame.SideData
                                                                    .SingleOrDefault(sd => sd.SideDataType.Equals("Mastering display metadata", StringComparison.OrdinalIgnoreCase));

                    if (masteringDisplayMetadata is not null)
                    {
                        SideData contentLightLevelMetadata = videoFrame.SideData
                                                                        .SingleOrDefault(sd => sd.SideDataType.Equals("Content light level metadata", StringComparison.OrdinalIgnoreCase));

                        HDRData hdrData = new()
                        {
                            HDRFlags = HDRFlags.HDR10,
                            Blue_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.BlueX) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.BlueX.Split("/")[0],
                            Blue_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.BlueY) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.BlueY.Split("/")[0],
                            Green_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.GreenX) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.GreenX.Split("/")[0],
                            Green_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.GreenY) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.GreenY.Split("/")[0],
                            Red_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.RedX) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.RedX.Split("/")[0],
                            Red_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.RedY) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.RedY.Split("/")[0],
                            WhitePoint_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.WhitePointX) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.WhitePointX.Split("/")[0],
                            WhitePoint_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.WhitePointY) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.WhitePointY.Split("/")[0],
                            MaxLuminance = string.IsNullOrWhiteSpace(masteringDisplayMetadata.MaxLuminance) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.MaxLuminance.Split("/")[0],
                            MinLuminance = string.IsNullOrWhiteSpace(masteringDisplayMetadata.MinLuminance) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.MinLuminance.Split("/")[0],
                            MaxCLL = $"{contentLightLevelMetadata?.MaxContent ?? 0},{contentLightLevelMetadata?.MaxAverage ?? 0}"
                        };

                        // Check for HDR10+ and DolbyVision
                        if (videoFrame.SideData.Any(sd => sd.SideDataType.Contains("Dolby Vision", StringComparison.OrdinalIgnoreCase)))
                        {
                            hdrData.HDRFlags |= HDRFlags.DOLBY_VISION;
                            // If we found DV info previously, tack it on here
                            if (dolbyVisionInfo is not null)
                                hdrData.DolbyVisionInfo = dolbyVisionInfo;
                        }

                        if (videoFrame.SideData.Any(sd => sd.SideDataType.Contains("HDR Dynamic Metadata", StringComparison.OrdinalIgnoreCase) ||
                                                            sd.SideDataType.Contains("HDR10+", StringComparison.OrdinalIgnoreCase)))
                            hdrData.HDRFlags |= HDRFlags.HDR10PLUS;

                        if (hdrData.IsDynamic)
                            hdrData.DynamicMetadataFullPaths = [];

                        videoStreamData.HDRData = hdrData;
                    }
                }
            }

            int durationInSeconds = Convert.ToInt32(probeData.Format.DurationInSeconds);
            string sourceFileTitle = probeData.Format.Tags?.Title;

            SourceStreamData sourceStreamData = new(durationInSeconds, numberOfFrames, videoStreamData, audioStreams, subtitleStreams);
            return new ProcessResult<SourceFileProbeResultData>(new SourceFileProbeResultData()
            {
                TitleOfSourceFile = sourceFileTitle,
                SourceStreamData = sourceStreamData,
            }, ProcessResultStatus.Success, "Successfully probed source file.");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Exception occurred while processing probe data.", details: new { sourceFileFullPath });
            return new ProcessResult<SourceFileProbeResultData>(null, ProcessResultStatus.Failure, "Error occurred while processing probe data.");
        }
    }

    /// <summary>FFprobe call and deserialization </summary>
    /// <param name="sourceFileFullPath">source file to be probed</param>
    /// <returns>ProbeData</returns>
    private (ProbeData ProbeData, string Message) FFProbe(string sourceFileFullPath)
    {
        ProbeData probeData = null;
        string message = null;
        try
        {
            string ffprobeArgs = $"-v quiet -read_intervals \"%+#2\" -print_format json -show_format -show_streams -show_entries frame \"{sourceFileFullPath}\"";

            ProcessResult<string> ffprobeResult = ProcessExecutor.Execute(new()
            {
                FileName = Path.Combine(State.Ffmpeg.FfprobeDirectory, Lookups.FFprobeExecutable),
                Arguments = ffprobeArgs,
                ReturnStandardOutput = true,
            });

            if (ffprobeResult.Status == ProcessResultStatus.Success)
            {
                string stringProbeOutput = ffprobeResult.Data.Trim();

                if (stringProbeOutput.IsValidJson() is true)
                {
                    probeData = JsonSerializer.Deserialize<ProbeData>(stringProbeOutput, CommunicationConstants.SerializerOptions);
                }
                else
                {
                    message = "ffprobe returned invalid json.";
                }
            }
            else
                message = "Error occurred while probing source file.";
        }
        catch (JsonException jsonEx)
        {
            message = "JsonException thrown when deserializing ffprobe output.";
            Logger.LogException(jsonEx, message, details: new { sourceFileFullPath });
        }
        catch (Exception ex)
        {
            message = "Exception thrown while attempting to probe source file.";
            Logger.LogException(ex, message, details: new { sourceFileFullPath, State.Ffmpeg.FfprobeDirectory });
        }

        return (probeData, message ?? "ffprobe success");
    }

    private static string ConvertNumberOfChannelsToLayout(short channels)
        => channels switch
        {
            0 => throw new Exception("Can't have 0 audio channels."),
            1 => "Mono",
            2 => "Stereo",
            _ => $"{channels}-channels"
        };

    #endregion Probe


    #region ScanType
    public async Task<ProcessResult<VideoScanType>> DetermineVideoScanTypeAsync(string sourceFileFullPath, CancellationToken cancellationToken)
    {
        string ffmpegArgs = $"-filter:v idet -frames:v 10000 -an -f rawvideo -y {Lookups.NullLocation} -i \"{sourceFileFullPath}\"";

        ProcessResult<string> scanTypeProcessResult = await ProcessExecutor.ExecuteAsync(new()
        {
            FileName = Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable),
            Arguments = ffmpegArgs,
            ReturnStandardError = true,
            AdditionalOutputCheck = str => str.Contains("frame detection")
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (scanTypeProcessResult.Status == ProcessResultStatus.Failure)
        {
            string msg = "Error occurred while determining video scan type.";
            Logger.LogError(msg, nameof(SourceFileProcessor), new { sourceFileFullPath });
            return new ProcessResult<VideoScanType>(VideoScanType.UNDETERMINED, ProcessResultStatus.Failure, msg);
        }

        VideoScanType determinedScanType;
        try
        {
            IEnumerable<string> frameDetections = scanTypeProcessResult.Data.TrimEnd(Environment.NewLine.ToCharArray()).Split(Environment.NewLine);

            List<(int tff, int bff, int prog, int undet)> scan = [];
            foreach (string frame in frameDetections)
            {
                MatchCollection matches = VideoScanRegex().Matches(frame.Remove(0, 34));
                scan.Add(new(Convert.ToInt32(matches[0].Value), Convert.ToInt32(matches[1].Value), Convert.ToInt32(matches[2].Value), Convert.ToInt32(matches[3].Value)));
            }

            int[] frame_totals = new int[4];

            foreach ((int tff, int bff, int prog, int undet) in scan)
            {
                // Should always be the order of: TFF, BFF, PROG
                frame_totals[(int)VideoScanType.INTERLACED_TFF] += tff;
                frame_totals[(int)VideoScanType.INTERLACED_BFF] += bff;
                frame_totals[(int)VideoScanType.PROGRESSIVE] += prog;
                frame_totals[(int)VideoScanType.UNDETERMINED] += undet;
            }

            determinedScanType = (VideoScanType)Array.IndexOf(frame_totals, frame_totals.Max());
        }
        catch (Exception ex)
        {
            string msg = "Exception occurred while determining video scan type.";
            Logger.LogException(ex, msg, nameof(SourceFileProcessor), new { sourceFileFullPath });
            return new ProcessResult<VideoScanType>(VideoScanType.UNDETERMINED, ProcessResultStatus.Failure, msg);
        }

        if (determinedScanType == VideoScanType.UNDETERMINED)
            return new ProcessResult<VideoScanType>(determinedScanType, ProcessResultStatus.Failure, "Unable to determine video scan type");
        else
            return new ProcessResult<VideoScanType>(determinedScanType, ProcessResultStatus.Success, "Successfully determined video scan type.");
    }
    #endregion ScanType


    #region Crop
    public async Task<ProcessResult<string>> DetermineCropAsync(string sourceFileFullPath, CancellationToken cancellationToken)
    {
        string ffmpegArgs = $"-i \"{sourceFileFullPath}\" -vf cropdetect -f null -";

        ProcessResult<string> cropResult = await ProcessExecutor.ExecuteAsync(new()
        {
            FileName = Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable),
            Arguments = ffmpegArgs,
            ReturnStandardError = true,
            AdditionalOutputCheck = str => str.Contains("crop="),
            TakeLastOutputLine = true,
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (cropResult.Status == ProcessResultStatus.Failure)
        {
            string msg = "Error occurred while determining video crop.";
            Logger.LogError(msg, nameof(SourceFileProcessor), new { sourceFileFullPath });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
        }

        string crop;
        try
        {
            string lastCropLine = cropResult.Data.TrimEnd(Environment.NewLine.ToCharArray());
            const string cropText = "crop=";
            crop = lastCropLine[(lastCropLine.IndexOf(cropText) + cropText.Length)..];
        }
        catch (Exception ex)
        {
            string msg = "Exception occurred while determining video crop.";
            Logger.LogException(ex, msg, nameof(SourceFileProcessor), new { sourceFileFullPath });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, msg);
        }

        return new ProcessResult<string>(crop, ProcessResultStatus.Success, "Successfully Determined Crop");
    }
    #endregion Crop
}
