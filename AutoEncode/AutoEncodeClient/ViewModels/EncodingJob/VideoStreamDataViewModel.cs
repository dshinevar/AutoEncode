using AutoEncodeClient.ViewModels.EncodingJob.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;

namespace AutoEncodeClient.ViewModels.EncodingJob;

public class VideoStreamDataViewModel :
    ViewModelBase,
    IVideoStreamDataViewModel
{
    #region Properties
    public short StreamIndex { get; set; }
    public string Title { get; set; }

    public HDRData HDRData { get; set; }
    public bool HasHDR => !HDRData?.HDRFlags.Equals(HDRFlags.NONE) ?? false;
    public bool HasDynamicHDR => HasHDR && (HDRData?.IsDynamic ?? false);
    public string CodecName { get; set; }
    public string PixelFormat { get; set; }

    private string _crop;
    public string Crop
    {
        get => _crop;
        set => SetAndNotify(_crop, value, () => _crop = value);
    }
    public string Resolution { get; set; }
    public int ResolutionInt { get; set; }
    public string ColorSpace { get; set; }
    public string ColorPrimaries { get; set; }
    public string ColorTransfer { get; set; }
    public string FrameRate { get; set; }
    public double CalculatedFrameRate { get; set; }

    private VideoScanType _scanType = VideoScanType.UNDETERMINED;
    public VideoScanType ScanType
    {
        get => _scanType;
        set => SetAndNotify(_scanType, value, () => _scanType = value);
    }
    public ChromaLocation? ChromaLocation { get; set; } = null;

    #endregion Properties

    public VideoStreamDataViewModel(VideoStreamData videoStreamData)
    {
        videoStreamData.CopyProperties(this);
    }

    public void Update(VideoStreamData videoStreamData)
    {
        videoStreamData.CopyProperties(this);
    }
}
