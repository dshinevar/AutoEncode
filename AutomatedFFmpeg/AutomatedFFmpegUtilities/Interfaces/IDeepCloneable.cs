using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedFFmpegUtilities.Interfaces
{
    public interface IDeepCloneable<T>
    {
        T DeepClone();
    }
}
