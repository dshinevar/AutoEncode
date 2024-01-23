using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Interfaces;
using System.IO;

namespace AutoEncodeServer.Data
{
    /// <summary>Most base source file data -- used to create <see cref="SourceFileData"/> which has a GUID </summary>
    internal class SourceFile : ISourceFileData
    {
        public string FileName => Path.GetFileName(FullPath);
        public string FullPath { get; set; }
        public string DestinationFullPath { get; set; }
        public bool Encoded { get; set; }
    }
}
