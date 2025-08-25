using AutoEncodeServer.Models.Interfaces;
using AutoEncodeServer.Utilities.Data;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            string msg = $"Source file no longer found for {this}";
            SetError(msg);
            Logger.LogError(msg, nameof(EncodingJobModel), new { SourceFullPath });
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

                ProcessResult<SourceFileProbeResultData> probeResult = SourceFileProbingProcessor.Probe(SourceFullPath);

                if (probeResult.Status.Equals(ProcessResultStatus.Success))
                {
                    Title = probeResult.Data.TitleOfSourceFile;
                    SourceStreamData = probeResult.Data.SourceStreamData;
                }
                else
                {
                    // Set error and end
                    SetError(probeResult.Message);
                    return;
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error getting probe or source file data for {this}";
                SetError(msg, ex);
                Logger.LogException(ex, msg, nameof(EncodingJobModel), new { SourceFullPath, State.Ffmpeg.FfprobeDirectory });
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
                    string msg = $"Failed to determine VideoScanType for {this}.";
                    SetError(msg);
                    Logger.LogError(msg, nameof(EncodingJobModel), new { SourceFullPath, State.Ffmpeg.FfmpegDirectory });
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
                string msg = $"Error determining VideoScanType for {this}";
                SetError(msg, ex);
                Logger.LogException(ex, msg, nameof(EncodingJobModel), new { SourceFullPath, State.Ffmpeg.FfmpegDirectory });
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // STEP 3: Determine Crop
            // TODO: REMOVE COMMENTED OUT CROP
            /*
            try
            {
                BuildingStatus = EncodingJobBuildingStatus.CROP;
                string crop = GetCrop(cancellationToken);

                if (string.IsNullOrWhiteSpace(crop))
                {
                    string msg = $"Failed to determine crop for {this}";
                    SetError(msg);
                    Logger.LogError(msg, nameof(EncodingJobModel), new { SourceFullPath, State.Ffmpeg.FfmpegDirectory });
                    return;
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
                string msg = $"Error determining crop for {this}";
                SetError(msg, ex);
                Logger.LogException(ex, msg, nameof(EncodingJobModel), new { SourceFullPath, State.Ffmpeg.FfmpegDirectory });
                return;
            }
            */

            cancellationToken.ThrowIfCancellationRequested();

            // OPTIONAL STEP: Create HDR metadata file if needed
            try
            {
                if (SourceStreamData.VideoStream.HasDynamicHDR is true)
                {
                    BuildingStatus = EncodingJobBuildingStatus.DYNAMIC_HDR;
                    HDRData hdrData = SourceStreamData.VideoStream.HDRData;
                    if (State.Hdr10Plus.Enabled && hdrData.HDRFlags.HasFlag(HDRFlags.HDR10PLUS))
                    {
                        ProcessResult<string> extractResult = HdrMetadataExtractor.Extract(SourceFullPath, HDRFlags.HDR10PLUS, cancellationToken);
                        if (extractResult.Status == ProcessResultStatus.Success)
                        {
                            AddSourceHDRMetadataFilePath(HDRFlags.HDR10PLUS, extractResult.Data);
                        }
                        else
                        {
                            string errorMsg = $"Error creating HDR metadata file for {this}";
                            SetError(errorMsg, extractResult.Message);
                            Logger.LogError(errorMsg, nameof(EncodingJobModel), new { SourceFullPath, State.Hdr10Plus.Hdr10PlusToolFullPath });
                            return;
                        }
                    }

                    if (State.DolbyVision.Enabled && hdrData.HDRFlags.HasFlag(HDRFlags.DOLBY_VISION))
                    {
                        ProcessResult<string> extractResult = HdrMetadataExtractor.Extract(SourceFullPath, HDRFlags.DOLBY_VISION, cancellationToken);
                        if (extractResult.Status == ProcessResultStatus.Success)
                        {
                            AddSourceHDRMetadataFilePath(HDRFlags.DOLBY_VISION, extractResult.Data);
                        }
                        else
                        {
                            string errorMsg = $"Error creating HDR metadata file for {this}";
                            SetError(errorMsg, extractResult.Message);
                            Logger.LogError(errorMsg, nameof(EncodingJobModel), new { SourceFullPath, State.DolbyVision.DoviToolFullPath });
                            return;
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
                string msg = $"Error creating HDR metadata file for {this}";
                SetError(msg, ex);
                Logger.LogException(ex, msg, nameof(EncodingJobModel),
                    new { Id, Name, DynamicHDR = SourceStreamData.VideoStream.HasDynamicHDR, DVEnabled = State.DolbyVision.Enabled, State.DolbyVision.DoviToolFullPath, State.Hdr10Plus.Hdr10PlusToolFullPath });
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
                string msg = $"Error building encoding instructions for {this}";
                SetError(msg, ex);
                Logger.LogException(ex, msg, nameof(EncodingJobModel), new { Id, Name });
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
                string msg = $"Error building FFmpeg command for {this}";
                SetError(msg, ex);
                Logger.LogException(ex, msg, nameof(EncodingJobModel), new { Id, Name });
                return;
            }

        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning($"Build was cancelled for {this} (Build Step: {BuildingStatus.GetDescription()})", nameof(EncodingJobModel));
            return;
        }
        catch (Exception ex)
        {
            string msg = $"Error building encoding job for {this}";
            SetError(msg, ex);
            Logger.LogException(ex, msg, nameof(EncodingJobModel), new { Id, Name, BuildingStatus });
            return;
        }

        Status = EncodingJobStatus.BUILT;
        BuildingStatus = EncodingJobBuildingStatus.BUILT;
        Logger.LogInfo($"Successfully built {this} encoding job.", nameof(EncodingJobModel));

        #region Local Functions

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
                FileName = Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable),
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
                FileName = Path.Combine(State.Ffmpeg.FfmpegDirectory, Lookups.FFmpegExecutable),
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
                if (State.DolbyVision.Enabled is true)
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
                AudioStreamData bestQualityAudioStream = audioData.Where(ad => ad.Commentary is false).MaxBy(ad => Lookups.AudioCodecPriority.IndexOf(ad.CodecName.ToLower()));
                IEnumerable<AudioStreamData> commentaryAudioStreams = audioData.Where(ad => ad.Commentary is true);

                if (bestQualityAudioStream.CodecName.Equals("ac3", StringComparison.OrdinalIgnoreCase) ||
                    bestQualityAudioStream.CodecName.Equals("aac", StringComparison.OrdinalIgnoreCase))
                {
                    // If ac3 and mono, go ahead and convert to AAC
                    if (bestQualityAudioStream.Channels < 2)
                    {
                        audioInstructions.Add(new()
                        {
                            SourceIndex = bestQualityAudioStream.AudioIndex,
                            AudioCodec = AudioCodec.AAC,
                            Language = bestQualityAudioStream.Language,
                            Title = bestQualityAudioStream.Title
                        });
                    }
                    // Otherwise just copy -- low enough quality not worth having multiple audio
                    else
                    {
                        audioInstructions.Add(new()
                        {
                            SourceIndex = bestQualityAudioStream.AudioIndex,
                            AudioCodec = AudioCodec.COPY,
                            Language = bestQualityAudioStream.Language,
                            Title = bestQualityAudioStream.Title
                        });
                    }
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

            instructions.SubtitleStreamEncodingInstructions = [.. subtitleInstructions.OrderBy(x => x.Forced)];

            EncodingInstructions = instructions;
        }
        #endregion Local Functions
    }
}
