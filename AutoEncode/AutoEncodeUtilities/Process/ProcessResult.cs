namespace AutoEncodeUtilities.Process;

/// <summary>Indicates the status of a <see cref="ProcessResult"/></summary>
public enum ProcessResultStatus
{
    Success,
    Warning,
    Failure
}

/// <summary>Very simple result object to indicate the result of a process and return a message. </summary>
/// <param name="Status">Result status.</param>
/// <param name="Message">Message from process indicating more info.</param>
public record ProcessResult(ProcessResultStatus Status, string Message);

/// <summary> Very simple result object to indicate the result of a process, a message, and data payload.</summary>
/// <typeparam name="T">Data payload type</typeparam>
/// <param name="Data">Data to return</param>
/// <param name="Status">Result status.</param>
/// <param name="Message">Message from process indicating more info.</param>
public record ProcessResult<T>(T Data, ProcessResultStatus Status, string Message) :
    ProcessResult(Status, Message);
