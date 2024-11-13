using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;

namespace AutoEncodeClient.ViewModels.EncodingJob.Interfaces;

public interface IVideoStreamDataViewModel :
    IViewModel,
    IUpdateable<VideoStreamData>
{
    short StreamIndex { get; }
    string Title { get; }
    HDRData HDRData { get; }
    bool HasHDR { get; }
    bool HasDynamicHDR { get; }
    string CodecName { get; }
    string PixelFormat { get; }
    string Crop { get; }
    string Resolution { get; }
    int ResolutionInt { get; }
    string ColorSpace { get; }
    string ColorPrimaries { get; }
    string ColorTransfer { get; }
    string FrameRate { get; }
    double CalculatedFrameRate { get; }
    VideoScanType ScanType { get; }
    ChromaLocation? ChromaLocation { get; }
}
