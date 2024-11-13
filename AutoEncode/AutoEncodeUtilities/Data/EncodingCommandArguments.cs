namespace AutoEncodeUtilities.Data;

/// <summary>
/// Encoding Command Arguments holder class. CommandArguments will have a length of 3 if DolbyVision, 1 otherwise.<br/>
/// BASIC ENCODE:<br/>
/// [0] = Encoding Arguments<br/>
/// DOLBY VISION:<br/>
/// [0] = Video Encoding Arguments<br/>
/// [1] = Audio/Subs Arguments<br/>
/// [2] = Merge Arguments<br/>
/// </summary>
public class EncodingCommandArguments
{
    public EncodingCommandArguments(bool isDolbyVision, params string[] commandArguments)
    {
        IsDolbyVision = isDolbyVision;
        CommandArguments = new string[(isDolbyVision ? 3 : 1)];

        for (int i = 0; i < commandArguments.Length; i++)
        {
            if (CommandArguments.Length > i)
            {
                CommandArguments[i] = commandArguments[i];
            }
        }
    }

    public bool IsDolbyVision { get; set; }

    /// <summary>
    /// BASIC ENCODE:<br/>
    /// [0] = Encoding Arguments<br/>
    /// DOLBY VISION:<br/>
    /// [0] = Video Encoding Arguments<br/>
    /// [1] = Audio/Subs Arguments<br/>
    /// [2] = Merge Arguments<br/>
    /// </summary>
    public string[] CommandArguments { get; } 
}
