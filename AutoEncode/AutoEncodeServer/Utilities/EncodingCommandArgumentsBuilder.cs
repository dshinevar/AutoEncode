using AutoEncodeServer.Models.Interfaces;
using AutoEncodeServer.Utilities.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoEncodeServer.Utilities;

public class EncodingCommandArgumentsBuilder : IEncodingCommandArgumentsBuilder
{
    public ILogger Logger { get; set; }

    private const string format = "{0} ";         // Format should hopefully always add space to end of append

    public EncodingCommandArguments Build(IEncodingJobData encodingJobData)
        => encodingJobData.EncodingInstructions.DolbyVisionEncoding ?
            BuildDolbyVisionArguments(encodingJobData) :
            BuildStandardFFmpegArguments(encodingJobData);

    private EncodingCommandArguments BuildStandardFFmpegArguments(IEncodingJobData encodingJobData)
    {
        SourceStreamData streamData = encodingJobData.SourceStreamData;
        EncodingInstructions instructions = encodingJobData.EncodingInstructions;

        VideoStreamEncodingInstructions videoInstructions = instructions.VideoStreamEncodingInstructions;

        StringBuilder sbArguments = new();
        sbArguments.AppendFormat(format, $"-y -nostdin -analyzeduration 2147483647 -probesize 2147483647 -i \"{encodingJobData.SourceFullPath}\"");

        // Map Section
        sbArguments.AppendFormat(format, "-map 0:v:0");
        foreach (AudioStreamEncodingInstructions audioInstructions in instructions.AudioStreamEncodingInstructions)
        {
            sbArguments.AppendFormat(format, $"-map 0:a:{audioInstructions.SourceIndex}");
        }
        foreach (SubtitleStreamEncodingInstructions subtitleInstructions in instructions.SubtitleStreamEncodingInstructions)
        {
            sbArguments.AppendFormat(format, $"-map 0:s:{subtitleInstructions.SourceIndex}");
        }

        // Video Section
        string deinterlace = videoInstructions.Deinterlace is true ? BuildDeinterlaceArgument(streamData.VideoStream.ScanType) : string.Empty;
        string crop = videoInstructions.Crop is true ? $"crop={streamData.VideoStream.Crop}" : string.Empty;
        string videoFilter = string.Empty;

        if (!string.IsNullOrWhiteSpace(deinterlace) || !string.IsNullOrWhiteSpace(crop))
        {
            videoFilter = $"-vf \"{HelperMethods.JoinFilter(", ", crop, deinterlace)}\"";
        }

        sbArguments.AppendFormat(format, $"-pix_fmt {videoInstructions.PixelFormat}");
        if (videoInstructions.VideoEncoder.Equals(VideoEncoder.LIBX265))
        {
            HDRData hdr = streamData.VideoStream.HDRData;
            sbArguments.AppendFormat(format, "-c:v libx265").AppendFormat(format, "-preset slow").AppendFormat(format, $"-crf {videoInstructions.CRF}");
            if (!string.IsNullOrWhiteSpace(videoFilter)) sbArguments.AppendFormat(format, videoFilter);
            sbArguments.Append($"-x265-params \"bframes={videoInstructions.BFrames}:keyint=120:repeat-headers=1:")
                .Append($"{(string.IsNullOrWhiteSpace(streamData.VideoStream.ColorPrimaries) ? string.Empty : $"colorprim={streamData.VideoStream.ColorPrimaries}:")}")
                .Append($"{(string.IsNullOrWhiteSpace(streamData.VideoStream.ColorTransfer) ? string.Empty : $"transfer={streamData.VideoStream.ColorTransfer}:")}")
                .Append($"{(string.IsNullOrWhiteSpace(streamData.VideoStream.ColorSpace) ? string.Empty : $"colormatrix={streamData.VideoStream.ColorSpace}:")}")
                .Append($"{(streamData.VideoStream.ChromaLocation is null ? string.Empty : $"chromaloc={(int)streamData.VideoStream.ChromaLocation}")}");

            if (videoInstructions.HasHDR)
            {
                // HDR10 data; Always add
                sbArguments.Append($":master-display='G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})':max-cll={streamData.VideoStream.HDRData.MaxCLL}");

                if (videoInstructions.HDRFlags.HasFlag(HDRFlags.HDR10PLUS))
                {
                    videoInstructions.DynamicHDRMetadataFullPaths.TryGetValue(HDRFlags.HDR10PLUS, out string metadataPath);
                    if (!string.IsNullOrWhiteSpace(metadataPath))
                    {
                        sbArguments.Append($":dhdr10-info='{metadataPath}'");
                    }
                }

                if (videoInstructions.HDRFlags.HasFlag(HDRFlags.DOLBY_VISION))
                {
                    Logger.LogWarning($"File with DolbyVision not being built with DolbyVision [{encodingJobData.SourceFullPath}]", nameof(EncodingCommandArgumentsBuilder));
                }
            }
            sbArguments.AppendFormat(format, '"');
        }
        else if (videoInstructions.VideoEncoder.Equals(VideoEncoder.LIBX264))
        {
            sbArguments.AppendFormat(format, "-c:v libx264").AppendFormat(format, "-preset veryslow");
            if (!string.IsNullOrWhiteSpace(videoFilter)) sbArguments.AppendFormat(format, videoFilter);
            sbArguments.AppendFormat(format, $"-x264-params \"bframes=16:b-adapt=2:b-pyramid=normal:partitions=all\" -crf {videoInstructions.CRF}");
        }
        else
        {
            throw new NotImplementedException("Unknown VideoEncoder. Unable to build ffmpeg arguments.");
        }

        // Audio Section
        for (int i = 0; i < instructions.AudioStreamEncodingInstructions.Count; i++)
        {
            AudioStreamEncodingInstructions audioInstruction = instructions.AudioStreamEncodingInstructions[i];
            BuildAudioStreamArguments(sbArguments, i, audioInstruction);
        }

        // Subtitle Section
        for (int i = 0; i < instructions.SubtitleStreamEncodingInstructions.Count; i++)
        {
            SubtitleStreamEncodingInstructions subtitleInstruction = instructions.SubtitleStreamEncodingInstructions[i];
            BuildSubtitleStreamArguments(sbArguments, i, subtitleInstruction);
        }

        sbArguments.Append($"-max_muxing_queue_size 9999 -metadata title=\"{encodingJobData.Title}\" \"{encodingJobData.DestinationFullPath}\"");

        return new EncodingCommandArguments(false, sbArguments.ToString());
    }

    private EncodingCommandArguments BuildDolbyVisionArguments(IEncodingJobData encodingJobData)
    {
        string sourceFullPath = encodingJobData.SourceFullPath;
        string encodedVideoFullPath = encodingJobData.EncodingInstructions.EncodedVideoFullPath;
        SourceStreamData streamData = encodingJobData.SourceStreamData;

        string videoEncodingCommandArguments;
        string audioSubEncodingCommandArguments;
        string mergeCommandArguments;

        // Video extraction/encoding
        StringBuilder sbVideo = new();
        string ffmpegFormatted;
        string sourceFormatted;
        string x265Formatted;
        string outputFormatted;
        string dolbyVisionPathFormatted;
        string masterDisplayFormatted;
        string maxCLLFormatted;

        VideoStreamEncodingInstructions videoInstructions = encodingJobData.EncodingInstructions.VideoStreamEncodingInstructions;
        HDRData hdr = streamData.VideoStream.HDRData;
        videoInstructions.DynamicHDRMetadataFullPaths.TryGetValue(HDRFlags.DOLBY_VISION, out string dolbyVisionMetadataPath);

        if (State.IsLinuxEnvironment)
        {
            ffmpegFormatted = $"'{Path.Combine(State.Ffmpeg.FfmpegDirectory, "ffmpeg")}'";
            sourceFormatted = $"'{sourceFullPath.Replace("'", "'\\''")}'";
            x265Formatted = $"'{State.DolbyVision.X265FullPath}'";
            outputFormatted = $"'{encodedVideoFullPath}'";
            masterDisplayFormatted = $"'G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})'";
            maxCLLFormatted = $"'{streamData.VideoStream.HDRData.MaxCLL}'";
            dolbyVisionPathFormatted = $"'{dolbyVisionMetadataPath}'";
        }
        else
        {
            ffmpegFormatted = $"\"{Path.Combine(State.Ffmpeg.FfmpegDirectory, "ffmpeg")}\"";
            sourceFormatted = $"\"{sourceFullPath}\"";
            x265Formatted = $"\"{State.DolbyVision.X265FullPath}\"";
            outputFormatted = $"\"{encodedVideoFullPath}\"";
            masterDisplayFormatted = $"\"G({hdr.Green_X},{hdr.Green_Y})B({hdr.Blue_X},{hdr.Blue_Y})R({hdr.Red_X},{hdr.Red_Y})WP({hdr.WhitePoint_X},{hdr.WhitePoint_Y})L({hdr.MaxLuminance},{hdr.MinLuminance})\"";
            maxCLLFormatted = $"\"{streamData.VideoStream.HDRData.MaxCLL}\"";
            dolbyVisionPathFormatted = $"\"{dolbyVisionMetadataPath}\"";
        }

        sbVideo.AppendFormat(format, $"{(State.IsLinuxEnvironment ? "-c" : "/C")}")
            .AppendFormat(format, $"\"{ffmpegFormatted} -y -hide_banner -loglevel error -nostdin -i {sourceFormatted}");

        if (videoInstructions.Crop is true) sbVideo.AppendFormat(format, $"-vf crop={streamData.VideoStream.Crop}");

        sbVideo.AppendFormat(format, $"-an -sn -f yuv4mpegpipe -strict -1 -pix_fmt {videoInstructions.PixelFormat} - |")
            .AppendFormat(format, $"{x265Formatted} - --input-depth 10 --output-depth 10 --y4m --preset slow --crf {videoInstructions.CRF} --bframes {videoInstructions.BFrames}")
            .AppendFormat(format, $"--repeat-headers --keyint 120")
            .AppendFormat(format, $"--master-display {masterDisplayFormatted}")
            .AppendFormat(format, $"--max-cll {maxCLLFormatted} --colormatrix {streamData.VideoStream.ColorSpace} --colorprim {streamData.VideoStream.ColorPrimaries} --transfer {streamData.VideoStream.ColorTransfer}")
            .AppendFormat(format, $"--dolby-vision-rpu {dolbyVisionPathFormatted} --dolby-vision-profile 8.1 --vbv-bufsize 120000 --vbv-maxrate 120000");

        if (videoInstructions.HDRFlags.HasFlag(HDRFlags.HDR10PLUS))
        {
            videoInstructions.DynamicHDRMetadataFullPaths.TryGetValue(HDRFlags.HDR10PLUS, out string hdr10PlusMetadataPath);
            if (!string.IsNullOrWhiteSpace(hdr10PlusMetadataPath))
            {
                sbVideo.AppendFormat(format, $"--dhdr10-info '{hdr10PlusMetadataPath}'");
            }
        }

        sbVideo.Append($"{outputFormatted}\"");
        videoEncodingCommandArguments = sbVideo.ToString();

        // Audio/Sub extraction/encoding
        StringBuilder sbAudioSubs = new();
        sbAudioSubs.AppendFormat(format, $"-y -nostdin -analyzeduration 2147483647 -probesize 2147483647 -i \"{sourceFullPath}\" -vn");
        foreach (AudioStreamEncodingInstructions audioInstructions in encodingJobData.EncodingInstructions.AudioStreamEncodingInstructions)
        {
            sbAudioSubs.AppendFormat(format, $"-map 0:a:{audioInstructions.SourceIndex}");
        }
        foreach (SubtitleStreamEncodingInstructions subtitleInstructions in encodingJobData.EncodingInstructions.SubtitleStreamEncodingInstructions)
        {
            sbAudioSubs.AppendFormat(format, $"-map 0:s:{subtitleInstructions.SourceIndex}");
        }

        for (int i = 0; i < encodingJobData.EncodingInstructions.AudioStreamEncodingInstructions.Count; i++)
        {
            AudioStreamEncodingInstructions audioInstruction = encodingJobData.EncodingInstructions.AudioStreamEncodingInstructions[i];
            BuildAudioStreamArguments(sbAudioSubs, i, audioInstruction);
        }

        for (int i = 0; i < encodingJobData.EncodingInstructions.SubtitleStreamEncodingInstructions.Count; i++)
        {
            SubtitleStreamEncodingInstructions subtitleInstruction = encodingJobData.EncodingInstructions.SubtitleStreamEncodingInstructions[i];
            BuildSubtitleStreamArguments(sbAudioSubs, i, subtitleInstruction);
        }

        sbAudioSubs.AppendFormat(format, $"-max_muxing_queue_size 9999 \"{encodingJobData.EncodingInstructions.EncodedAudioSubsFullPath}\"");
        audioSubEncodingCommandArguments = sbAudioSubs.ToString();

        // Merging
        StringBuilder sbMerge = new();
        sbMerge.AppendFormat(format, $"-o \"{encodingJobData.DestinationFullPath}\" --compression -1:none \"{encodedVideoFullPath}\" --compression -1:none \"{encodingJobData.EncodingInstructions.EncodedAudioSubsFullPath}\"")
                .Append($"--title \"{encodingJobData.Title}\"");
        mergeCommandArguments = sbMerge.ToString();

        return new EncodingCommandArguments(true, videoEncodingCommandArguments, audioSubEncodingCommandArguments, mergeCommandArguments);
    }

    private static void BuildAudioStreamArguments(StringBuilder sbAudio, int index, AudioStreamEncodingInstructions audioInstructions)
    {
        if (audioInstructions.AudioCodec.Equals(AudioCodec.UNKNOWN))
        {
            throw new Exception("AudioCodec not set (Unknown). Unable to build ffmpeg arguments");
        }
        else if (audioInstructions.AudioCodec.Equals(AudioCodec.COPY))
        {
            sbAudio.AppendFormat(format, $"-c:a:{index} copy");
        }
        else
        {
            sbAudio.AppendFormat(format, $"-c:a:{index} {audioInstructions.AudioCodec.GetDescription()}")
                    .AppendFormat(format, $"-ac:a:{index} 2 -b:a:{index} 192k -filter:a:{index} \"aresample=matrix_encoding=dplii\"")
                    .AppendFormat(format, $"-metadata:s:a:{index} title=\"Stereo ({audioInstructions.AudioCodec.GetDescription()})\"")
                    .AppendFormat(format, $"-metadata:s:a:{index} language=\"{audioInstructions.Language}\"");
        }
    }

    private static void BuildSubtitleStreamArguments(StringBuilder sbSubtitle, int index, SubtitleStreamEncodingInstructions subtitleInstructions)
    {
        sbSubtitle.AppendFormat(format, $"-c:s:{index} copy");
    }

    private static string BuildDeinterlaceArgument(VideoScanType scanType)
    {
        if (State.Ffmpeg.NnediEnabled is true)
        {
            string nnediArg = $"nnedi=weights='{Path.Combine(State.Ffmpeg.NnediDirectory, "nnedi3_weights.bin")}'";
            if (State.IsWindowsEnvironment)
                return nnediArg.Replace("\\", $"\\\\").Replace(":", "\\:");
            else
                return nnediArg.Replace("/", "//");
        }
        else
        {
            return $"yadif=1:{(int)scanType}:0";
        }
    }
}
