using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Interfaces
{
    public interface IHDRData
    {
        HDRType HDRType { get; }
        string Red_X { get; set; }
        string Red_Y { get; set; }
        string Green_X { get; set; }
        string Green_Y { get; set; }
        string Blue_X { get; set; }
        string Blue_Y { get; set; }
        string WhitePoint_X { get; set; }
        string WhitePoint_Y { get; set; }
        string MinLuminance { get; set; }
        string MaxLuminance { get; set; }
        string MaxCLL { get; set; }
    }

    public interface IDynamicHDRData : IHDRData
    {
        string MetadataFullPath { get; set; }
    }
}
