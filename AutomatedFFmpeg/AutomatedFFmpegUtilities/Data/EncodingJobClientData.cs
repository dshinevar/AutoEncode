using AutomatedFFmpegUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedFFmpegUtilities.Data
{
    public class EncodingJobClientData
    {
        public string Name { get; set; }
        public EncodingJobStatus Status { get; set; }
        public bool Paused { get; set; }

        public EncodingJobClientData() { }

        public EncodingJobClientData(EncodingJob encodingJob)
        {
            Name = encodingJob.Name;
            Status = encodingJob.Status;
            Paused = encodingJob.Paused;
        }
    }
}
