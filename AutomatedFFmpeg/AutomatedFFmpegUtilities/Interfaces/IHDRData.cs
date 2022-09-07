using AutomatedFFmpegUtilities.Enums;
using System.Collections.Generic;

namespace AutomatedFFmpegUtilities.Interfaces
{
    public interface IHDRData
    {
        HDRFlags HDRFlags { get; }
        string Red_X { get; }
        string Red_Y { get; }
        string Green_X { get; }
        string Green_Y { get; }
        string Blue_X { get; }
        string Blue_Y { get; }
        string WhitePoint_X { get; }
        string WhitePoint_Y { get; }
        string MinLuminance { get; }
        string MaxLuminance { get; }
        string MaxCLL { get; }
    }

    public interface IDynamicHDRData : IHDRData
    {
        Dictionary<HDRFlags, string> MetadataFullPaths { get; }
    }
}
