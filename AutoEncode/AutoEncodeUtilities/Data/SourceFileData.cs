using AutoEncodeUtilities.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeUtilities.Data
{
    public class SourceFileData :
        ISourceFileData,
        IUpdateable<SourceFileData>,
        IEquatable<SourceFileData>
    {
        #region Properties
        public Guid Guid { get; set; }
        public string FileName => Path.GetFileName(FullPath);
        public string FullPath { get; set; }
        public string DestinationFullPath { get; set; }
        public bool Encoded { get; set; }
        #endregion Properties

        public SourceFileData() { }

        public SourceFileData(ISourceFileData sourceFileData)
        {
            Guid = Guid.NewGuid();
            sourceFileData.CopyProperties(this);
        }

        #region Methods
        public bool Equals(SourceFileData data) => Guid.Equals(data.Guid);
        public override bool Equals(object obj)
        {
            if (obj is SourceFileData sourceFileData)
            {
                return Equals(sourceFileData);
            }

            return false;
        }
        public override int GetHashCode() => Guid.GetHashCode();
        public void Update(SourceFileData data) => data.CopyProperties(this);
        public static int CompareByFileName(SourceFileData data1, SourceFileData data2) => string.Compare(data1.FileName, data2.FileName);
        #endregion Methods
    }
}
