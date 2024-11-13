using AutoEncodeUtilities.Enums;
using System;

namespace AutoEncodeUtilities.Communication.Data;

public class EncodingJobStatusUpdateData
{
    public EncodingJobStatus Status { get; set; }

    public EncodingJobBuildingStatus BuildingStatus { get; set; }

    public bool IsProcessing { get; set; }

    public bool HasError { get; set; }

    public string ErrorMessage { get; set; }

    public DateTime? ErrorTime { get; set; }

    public bool ToBePaused { get; set; }

    public bool Paused { get; set; }

    public bool Canceled { get; set; }

    public bool CanCancel { get; set; }

    public bool Complete { get; set; }
}
