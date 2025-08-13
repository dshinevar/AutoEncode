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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Stream = AutoEncodeServer.Data.Stream;

namespace AutoEncodeServer.Utilities;

/// <summary>Probes a given source file utilizing ffprobe and produces <see cref="SourceStreamData"/></summary>
public class SourceFileProbingProcessor : ISourceFileProbingProcessor
{
    public ILogger Logger { get; set; }

    public ProcessResult<SourceFileProbeResultData> Probe(string sourceFileFullPath)
    {
        ProbeData probeData = FFProbe(sourceFileFullPath);
        if (probeData is null)
        {
            return new ProcessResult<SourceFileProbeResultData>(null, ProcessResultStatus.Failure, "Error occurred while probing source file.");
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
    private ProbeData FFProbe(string sourceFileFullPath)
    {
        try
        {
            string ffprobeArgs = $"-v quiet -read_intervals \"%+#2\" -print_format json -show_format -show_streams -show_entries frame \"{sourceFileFullPath}\"";

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(State.Ffmpeg.FfprobeDirectory, Lookups.FFprobeExecutable),
                Arguments = ffprobeArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            StringBuilder sbFfprobeOutput = new();

            using (Process ffprobeProcess = new())
            {
                ffprobeProcess.StartInfo = startInfo;
                ffprobeProcess.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null) sbFfprobeOutput.AppendLine(e.Data);
                };
                ffprobeProcess.Start();
                ffprobeProcess.BeginOutputReadLine();
                ffprobeProcess.WaitForExit();
            }

            string stringProbeOutput = sbFfprobeOutput.ToString().Trim();

            if (stringProbeOutput.IsValidJson() is true)
            {
                return JsonSerializer.Deserialize<ProbeData>(stringProbeOutput, CommunicationConstants.SerializerOptions);
            }
        }
        catch (JsonException jsonEx)
        {
            Logger.LogException(jsonEx, "JsonException thrown when deserializing ffprobe output.", details: new { sourceFileFullPath });
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Exception thrown while attempting to probe source file.", details: new { sourceFileFullPath });
        }

        return null;
    }

    private static string ConvertNumberOfChannelsToLayout(short channels)
        => channels switch
        {
            0 => throw new Exception("Can't have 0 audio channels."),
            1 => "Mono",
            2 => "Stereo",
            _ => $"{channels}-channels"
        };
}
