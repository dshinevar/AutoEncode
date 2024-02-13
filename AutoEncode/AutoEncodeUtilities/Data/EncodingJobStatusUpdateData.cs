using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;

namespace AutoEncodeUtilities.Data
{
    public class EncodingJobStatusUpdateData : IClientUpdateData
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
}
