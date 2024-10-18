namespace AutoEncodeUtilities.Config;

public class GlobalJobSettings
{
    public int MaxNumberOfJobsInQueue { get; set; } = 20;
    public int HoursCompletedUntilRemoval { get; set; } = 1;
    public int HoursErroredUntilRemoval { get; set; } = 2;
    public bool DolbyVisionEncodingEnabled { get; set; } = true;
}
