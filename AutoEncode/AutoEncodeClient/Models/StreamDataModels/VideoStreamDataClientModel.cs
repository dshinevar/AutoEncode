using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;

namespace AutoEncodeClient.Models.StreamDataModels;

public class VideoStreamDataClientModel :
    StreamDataModel,
    IUpdateable<VideoStreamData>
{
    public VideoStreamDataClientModel(VideoStreamData data)
    {
        data.CopyProperties(this);
    }

    #region Properties
    private HDRData _hdrData = null;
    public HDRData HDRData
    {
        get => _hdrData;
        set => SetAndNotify(_hdrData, value, () =>
        {
            if (value is not null)
            {
                if (_hdrData is null) _hdrData = value;
                else _hdrData.Update(value);
            }
        });
    }

    private bool _hasHDR;
    public bool HasHDR
    {
        get => _hasHDR;
        set => SetAndNotify(_hasHDR, value, () => _hasHDR = value);
    }

    private bool _hasDynamicHDR;
    public bool HasDynamicHDR
    {
        get => _hasDynamicHDR;
        set => SetAndNotify(_hasDynamicHDR, value, () => _hasDynamicHDR = value);
    }

    private string _codecName;
    public string CodecName
    {
        get => _codecName;
        set => SetAndNotify(_codecName, value, () => _codecName = value);
    }

    private string _pixelFormat;
    public string PixelFormat
    {
        get => _pixelFormat;
        set => SetAndNotify(_pixelFormat, value, () => _pixelFormat = value);
    }

    private string _crop;
    public string Crop
    {
        get => _crop;
        set => SetAndNotify(_crop, value, () => _crop = value);
    }

    private string _resolution;
    public string Resolution
    {
        get => _resolution;
        set => SetAndNotify(_resolution, value, () => _resolution = value);
    }

    private int _resolutionInt;
    public int ResoultionInt
    {
        get => _resolutionInt;
        set => SetAndNotify(_resolutionInt, value, () => _resolutionInt = value);
    }

    private string _colorSpace;
    public string ColorSpace
    {
        get => _colorSpace;
        set => SetAndNotify(_colorSpace, value, () => _colorSpace = value);
    }

    private string _colorPrimaries;
    public string ColorPrimaries
    {
        get => _colorPrimaries;
        set => SetAndNotify(_colorPrimaries, value, () => _colorPrimaries = value);
    }

    private string _colorTransfer;
    public string ColorTransfer
    {
        get => _colorTransfer;
        set => SetAndNotify(_colorTransfer, value, () => _colorTransfer = value);
    }

    private string _framerate;
    public string FrameRate
    {
        get => _framerate;
        set => SetAndNotify(_framerate, value, () => _framerate = value);
    }

    private double _calculatedFrameRate;
    public double CalculatedFrameRate
    {
        get => _calculatedFrameRate;
        set => SetAndNotify(_calculatedFrameRate, value, () => _calculatedFrameRate = value);
    }

    private bool _animated = false;
    public bool Animated
    {
        get => _animated;
        set => SetAndNotify(_animated, value, () => _animated = value);
    }

    private VideoScanType _scanType = VideoScanType.UNDETERMINED;
    public VideoScanType ScanType
    {
        get => _scanType;
        set => SetAndNotify(_scanType, value, () => _scanType = value);
    }
    private ChromaLocation? _chromaLocation = null;
    public ChromaLocation? ChromaLocation
    {
        get => _chromaLocation;
        set => SetAndNotify(_chromaLocation, value, () => _chromaLocation = value);
    }

    #endregion Properties

    public void Update(VideoStreamData data) => base.Update(data);
}
