using AutoEncodeServer.Data;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Communication;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace AutoEncodeServer.Models;

// BUILD
public partial class EncodingJobModel :
    ModelBase,
    IEncodingJobModel
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex VideoScanRegex();

    public void Build(CancellationTokenSource cancellationTokenSource)
    {
        TaskCancellationTokenSource = cancellationTokenSource;
        Status = EncodingJobStatus.BUILDING;
        BuildingStatus = EncodingJobBuildingStatus.BUILDING;

        CancellationToken cancellationToken = cancellationTokenSource.Token;

        if (File.Exists(SourceFullPath) is false)
        {
            SetError(Logger.LogError($"Source file no longer found for {this}", nameof(EncodingJobModel), new { SourceFullPath }));
            return;
        }

        HelperMethods.DebugLog($"BUILD STARTED: {this}", nameof(EncodingJobModel));

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // STEP 1: Initial ffprobe
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.PROBING;
                ProbeData probeData = GetProbeData();

                if (probeData is not null)
                {
                    SourceStreamData = probeData.ToSourceStreamData();
                    Title = probeData.GetTitle();
                }
                else
                {
                    // Set error and end
                    SetError(Logger.LogError($"Failed to get probe data for {Filename}", nameof(EncodingJobModel), new { SourceFullPath }));
                    return;
                }
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error getting probe or source file data for {this}", nameof(EncodingJobModel), new { SourceFullPath, State.FFmpegDirectory }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 2: Get ScanType
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.SCAN_TYPE;
                VideoScanType scanType = GetVideoScan(cancellationToken);

                if (scanType.Equals(VideoScanType.UNDETERMINED))
                {
                    SetError(Logger.LogError($"Failed to determine VideoScanType for {this}.", nameof(EncodingJobModel), new { SourceFullPath, State.FFmpegDirectory }));
                    return;
                }
                else
                {
                    SetSourceScanType(scanType);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error determining VideoScanType for {this}", nameof(EncodingJobModel), new { SourceFullPath, State.FFmpegDirectory }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 3: Determine Crop
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.CROP;
                string crop = GetCrop(cancellationToken);

                if (string.IsNullOrWhiteSpace(crop))
                {
                    SetError(Logger.LogError($"Failed to determine crop for {this}", nameof(EncodingJobModel), new { SourceFullPath, State.FFmpegDirectory }));
                }
                else
                {
                    SetSourceCrop(crop);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error determining crop for {this}", nameof(EncodingJobModel), new { SourceFullPath, State.FFmpegDirectory }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // OPTIONAL STEP: Create HDR metadata file if needed
            try
            {
                if (SourceStreamData.VideoStream.HasDynamicHDR is true)
                {
                    BuildingStatus = EncodingJobBuildingStatus.DYNAMIC_HDR;
                    HDRData hdrData = SourceStreamData.VideoStream.HDRData;
                    if (hdrData.HDRFlags.HasFlag(HDRFlags.HDR10PLUS))
                    {
                        // If we aren't given a path, skip this step;  It will be treated as HDR10
                        if (string.IsNullOrWhiteSpace(State.HDR10PlusExtractorFullPath) is false)
                        {
                            string filePath = CreateHDRMetadataFile(HDRFlags.HDR10PLUS, cancellationToken);
                            if (string.IsNullOrWhiteSpace(filePath) is false)
                            {
                                AddSourceHDRMetadataFilePath(HDRFlags.HDR10PLUS, filePath);
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"No HDR10+ Metadata Extractor given. Will not use HDR10+ for {Name}.", nameof(EncodingJobModel));
                        }
                    }

                    if (hdrData.HDRFlags.HasFlag(HDRFlags.DOLBY_VISION) && State.DolbyVisionEncodingEnabled is true)
                    {
                        // If we aren't given a path, skip this step;  It will be treated as HDR10
                        if (!string.IsNullOrWhiteSpace(State.DolbyVisionExtractorFullPath))
                        {
                            string filePath = CreateHDRMetadataFile(HDRFlags.DOLBY_VISION, cancellationToken);
                            if (string.IsNullOrWhiteSpace(filePath) is false)
                            {
                                AddSourceHDRMetadataFilePath(HDRFlags.DOLBY_VISION, filePath);
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"No DolbyVision Metadata Extractor given. Will not use DolbyVision for {Name}.", nameof(EncodingJobModel));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error creating HDR metadata file for {this}", nameof(EncodingJobModel),
                    new { Id, Name, DynamicHDR = SourceStreamData.VideoStream.HasDynamicHDR, State.DolbyVisionEncodingEnabled, State.DolbyVisionExtractorFullPath, State.HDR10PlusExtractorFullPath }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 4: Decide Encoding Options
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.INSTRUCTIONS;
                DetermineEncodingInstructions();

                if (EncodingInstructions is null)
                    throw new Exception("EncodingInstructions null.");
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error building encoding instructions for {this}", nameof(EncodingJobModel), new { Id, Name }), ex);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 5: Create FFMPEG command
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.COMMAND;

                EncodingCommandArguments = EncodingCommandArgumentsBuilder.Build(this);

                if (EncodingCommandArguments is null)
                {
                    throw new Exception("Failed to build encoding command arguments.");
                }

                if (EncodingCommandArguments.IsDolbyVision is true)
                {
                    if (EncodingCommandArguments.CommandArguments.Any(string.IsNullOrWhiteSpace))
                    {
                        throw new Exception("Empty dolby vision encoding command argument string returned.");
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(EncodingCommandArguments.CommandArguments[0]))
                    {
                        throw new Exception("Empty encoding command argument string returned.");
                    }
                }
            }
            catch (Exception ex)
            {
                SetError(Logger.LogException(ex, $"Error building FFmpeg command for {this}", nameof(EncodingJobModel), new { Id, Name }), ex);
                return;
            }

        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning($"Build was cancelled for {this} - Build Step: {BuildingStatus.GetDescription()}", nameof(EncodingJobModel));
            return;
        }
        catch (Exception ex)
        {
            SetError(Logger.LogException(ex, $"Error building encoding job for {this}", nameof(EncodingJobModel), new { Id, Name, BuildingStatus }), ex);
            return;
        }

        Status = EncodingJobStatus.BUILT;
        BuildingStatus = EncodingJobBuildingStatus.BUILT;
        Logger.LogInfo($"Successfully built {this} encoding job.", nameof(EncodingJobModel));

        #region Local Functions
        ProbeData GetProbeData()
        {
            string ffprobeArgs = $"-v quiet -read_intervals \"%+#2\" -print_format json -show_format -show_streams -show_entries frame \"{SourceFullPath}\"";

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(State.FFmpegDirectory, "ffprobe"),
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

            return null;
        }

        // Returns string of crop in this format: "XXXX:YYYY:AA:BB"
        string GetCrop(CancellationToken cancellationToken)
        {
            string ffmpegArgs = $"-i \"{SourceFullPath}\" -vf cropdetect -f null -";
            Process cropProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                cropProcess?.Kill(true);
            });

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(State.FFmpegDirectory, "ffmpeg"),
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            string latestCropLine = string.Empty;

            using (cropProcess = new())
            {
                cropProcess.StartInfo = startInfo;
                cropProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false && e.Data.Contains("crop=")) latestCropLine = e.Data;
                };
                cropProcess.Start();
                cropProcess.BeginErrorReadLine();
                cropProcess.WaitForExit();
            }

            tokenRegistration.Unregister();

            cancellationToken.ThrowIfCancellationRequested();

            return latestCropLine.TrimEnd(Environment.NewLine.ToCharArray())[latestCropLine.IndexOf("crop=")..].Remove(0, 5);
        }

        VideoScanType GetVideoScan(CancellationToken cancellationToken)
        {
            string ffmpegArgs = $"-filter:v idet -frames:v 10000 -an -f rawvideo -y {Lookups.NullLocation} -i \"{SourceFullPath}\"";
            Process scanTypeProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                scanTypeProcess?.Kill(true);
            });

            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(State.FFmpegDirectory, "ffmpeg"),
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            StringBuilder sbScan = new();

            using (scanTypeProcess = new())
            {
                scanTypeProcess.StartInfo = startInfo;
                scanTypeProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false && e.Data.Contains("frame detection")) sbScan.AppendLine(e.Data);
                };

                scanTypeProcess.Start();
                scanTypeProcess.BeginErrorReadLine();
                scanTypeProcess.WaitForExit();
            }

            tokenRegistration.Unregister();

            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<string> frameDetections = sbScan.ToString().TrimEnd(Environment.NewLine.ToCharArray()).Split(Environment.NewLine);

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

            return (VideoScanType)Array.IndexOf(frame_totals, frame_totals.Max());
        }

        // Creates the Dynamic HDR Metadata file (.json or .bin) for ffmpeg to ingest when encoding
        string CreateHDRMetadataFile(HDRFlags hdrFlag, CancellationToken cancellationToken)
        {
            string metadataOutputFile = $"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(SourceFullPath).Replace('\'', ' ')}{(hdrFlag.Equals(HDRFlags.HDR10PLUS) ? ".json" : ".RPU.bin")}";
            Process hdrMetadataProcess = null;
            CancellationTokenRegistration tokenRegistration = cancellationToken.Register(() =>
            {
                hdrMetadataProcess?.Kill(true);
            });

            string ffmpegArgs;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string extractorArgs = hdrFlag.Equals(HDRFlags.HDR10PLUS) ? $"'{State.HDR10PlusExtractorFullPath}' extract -o '{metadataOutputFile}' - " :
                                                                            $"'{State.DolbyVisionExtractorFullPath}' extract-rpu - -o '{metadataOutputFile}'";

                ffmpegArgs = $"-c \"{Path.Combine(State.FFmpegDirectory, "ffmpeg")} -nostdin -i '{SourceFullPath.Replace("'", "'\\''")}' -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
            }
            else
            {
                string extractorArgs = hdrFlag.Equals(HDRFlags.HDR10PLUS) ? $"\"{State.HDR10PlusExtractorFullPath}\" extract -o \"{metadataOutputFile}\" - " :
                                                                            $"\"{State.DolbyVisionExtractorFullPath}\" extract-rpu - -o \"{metadataOutputFile}\"";

                ffmpegArgs = $"/C \"\"{Path.Combine(State.FFmpegDirectory, "ffmpeg")}\" -i \"{SourceFullPath}\" -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {extractorArgs}\"";
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

            tokenRegistration.Unregister();

            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(metadataOutputFile))
            {
                FileInfo metadataFileInfo = new(metadataOutputFile);

                if (metadataFileInfo.Length > 0)
                {
                    return metadataOutputFile;
                }
                else
                {
                    throw new Exception("HDR Metadata file was created but is empty.");
                }
            }
            else
            {
                throw new Exception("HDR Metadata file was not created/does not exist.");
            }
        }

        void DetermineEncodingInstructions()
        {
            SourceStreamData streamData = SourceStreamData;
            EncodingInstructions instructions = new();

            VideoStreamEncodingInstructions videoStreamEncodingInstructions = new()
            {
                VideoEncoder = streamData.VideoStream.ResoultionInt >= Lookups.MinX265ResolutionInt ? VideoEncoder.LIBX265 : VideoEncoder.LIBX264,
                BFrames = 8,
                Deinterlace = !streamData.VideoStream.ScanType.Equals(VideoScanType.PROGRESSIVE),
                Crop = true
            };

            if (streamData.VideoStream.HasDynamicHDR)
            {
                if (State.DolbyVisionEncodingEnabled is true)
                {
                    instructions.DolbyVisionEncoding = true;
                }

                HDRData hdrData = streamData.VideoStream.HDRData;
                videoStreamEncodingInstructions.HDRFlags |= HDRFlags.HDR10;
                videoStreamEncodingInstructions.DynamicHDRMetadataFullPaths = [];
                // Go through each possible metadata entry
                foreach (KeyValuePair<HDRFlags, string> path in hdrData.DynamicMetadataFullPaths)
                {
                    // If added, shouldn't be null/empty but double check anyways
                    if (!string.IsNullOrWhiteSpace(path.Value))
                    {
                        // Set flag AND add path
                        videoStreamEncodingInstructions.HDRFlags |= path.Key;
                        videoStreamEncodingInstructions.DynamicHDRMetadataFullPaths.Add(path.Key, path.Value);

                        if (path.Key.Equals(HDRFlags.DOLBY_VISION))
                        {
                            instructions.EncodedVideoFullPath = DestinationFullPath.Replace(Path.GetExtension(DestinationFullPath), ".hevc").Replace('\'', ' ');
                            instructions.EncodedAudioSubsFullPath = DestinationFullPath.Replace(Path.GetExtension(DestinationFullPath), ".as.mkv").Replace('\'', ' ');
                        }

                    }
                }
            }
            else if (streamData.VideoStream.HasHDR)
            {
                videoStreamEncodingInstructions.HDRFlags = HDRFlags.HDR10;
            }
            else
            {
                videoStreamEncodingInstructions.HDRFlags = HDRFlags.NONE;
            }

            videoStreamEncodingInstructions.PixelFormat = videoStreamEncodingInstructions.VideoEncoder.Equals(VideoEncoder.LIBX265) ? "yuv420p10le" : "yuv420p";
            videoStreamEncodingInstructions.CRF = videoStreamEncodingInstructions.VideoEncoder.Equals(VideoEncoder.LIBX265) ? 20 : 16;
            instructions.VideoStreamEncodingInstructions = videoStreamEncodingInstructions;

            List<AudioStreamEncodingInstructions> audioInstructions = [];

            IEnumerable<IGrouping<string, AudioStreamData>> streamsByLanguage = streamData.AudioStreams.GroupBy(x => x.Language);
            foreach (IGrouping<string, AudioStreamData> audioData in streamsByLanguage)
            {
                AudioStreamData bestQualityAudioStream = audioData.Where(x => x.Commentary is false).MaxBy(x => Lookups.AudioCodecPriority.IndexOf(x.CodecName.ToLower()));
                IEnumerable<AudioStreamData> commentaryAudioStreams = audioData.Where(x => x.Commentary is true);

                if (bestQualityAudioStream.CodecName.Equals("ac3", StringComparison.OrdinalIgnoreCase) && bestQualityAudioStream.Channels < 2)
                {
                    // If ac3 and mono, go ahead and convert to AAC
                    audioInstructions.Add(new()
                    {
                        SourceIndex = bestQualityAudioStream.AudioIndex,
                        AudioCodec = AudioCodec.AAC,
                        Language = bestQualityAudioStream.Language,
                        Title = bestQualityAudioStream.Title
                    });
                }
                else
                {
                    audioInstructions.Add(new()
                    {
                        SourceIndex = bestQualityAudioStream.AudioIndex,
                        AudioCodec = AudioCodec.COPY,
                        Language = bestQualityAudioStream.Language,
                        Title = bestQualityAudioStream.Title
                    });

                    audioInstructions.Add(new()
                    {
                        SourceIndex = bestQualityAudioStream.AudioIndex,
                        AudioCodec = AudioCodec.AAC,
                        Language = bestQualityAudioStream.Language,
                        Title = bestQualityAudioStream.Title
                    });
                }

                foreach (AudioStreamData commentaryStream in commentaryAudioStreams)
                {
                    // Just copy all commentary streams
                    audioInstructions.Add(new()
                    {
                        SourceIndex = commentaryStream.AudioIndex,
                        AudioCodec = AudioCodec.COPY,
                        Language = commentaryStream.Language,
                        Title = commentaryStream.Title,
                        Commentary = true
                    });
                }
            }

            instructions.AudioStreamEncodingInstructions = [.. audioInstructions.OrderBy(x => x.Commentary) // Put commentaries at the end
            .ThenBy(x => x.Language.Equals(Lookups.PrimaryLanguage, StringComparison.OrdinalIgnoreCase)) // Put non-primary languages first
            .ThenBy(x => x.Language) // Not sure if needed? Make sure languages are together
            .ThenByDescending(x => x.AudioCodec.Equals(AudioCodec.COPY))];

            List<SubtitleStreamEncodingInstructions> subtitleInstructions = [];
            if (streamData.SubtitleStreams?.Any() ?? false)
            {
                foreach (SubtitleStreamData stream in streamData.SubtitleStreams)
                {
                    subtitleInstructions.Add(new()
                    {
                        SourceIndex = stream.SubtitleIndex,
                        Forced = stream.Forced,
                        Title = stream.Title,
                        Commentary = stream.Commentary,
                        HearingImpaired = stream.HearingImpaired,
                    });
                }
            }

            instructions.SubtitleStreamEncodingInstructions = subtitleInstructions.OrderBy(x => x.Forced).ToList();

            EncodingInstructions = instructions;
        }
        #endregion Local Functions
    }
}
