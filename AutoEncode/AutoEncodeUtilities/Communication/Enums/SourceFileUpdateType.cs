namespace AutoEncodeUtilities.Communication.Enums;

/// <summary>Indicates the type of update to the source files.</summary>
public enum SourceFileUpdateType
{
    /// <summary>Default</summary>
    None = 0,

    /// <summary>Source file added.</summary>
    Add = 1,

    /// <summary>Source file removed.</summary>
    Remove = 2,

    /// <summary>Source file info updated.</summary>
    Update = 3
}
