using AutoEncodeUtilities.Enums;
using System;

namespace AutoEncodeServer.Models.Data;

public class EncodingJobStatusChangedEventArgs : EventArgs
{
    public EncodingJobStatus Status { get; set; }
}
