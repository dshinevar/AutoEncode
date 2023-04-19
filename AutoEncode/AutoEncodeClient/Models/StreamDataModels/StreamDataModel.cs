using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Interfaces;

namespace AutoEncodeClient.Models.StreamDataModels
{
    public class StreamDataModel :
        ModelBase,
        IUpdateable<StreamData>
    {
        public StreamDataModel() { }

        public void Update(StreamData data) => data.CopyProperties(this);

        #region Properties
        private int _streamIndex;
        public int StreamIndex
        {
            get => _streamIndex;
            set => SetAndNotify(_streamIndex, value, () => _streamIndex = value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetAndNotify(_title, value, () => _title = value);
        }
        #endregion Properties
    }
}
