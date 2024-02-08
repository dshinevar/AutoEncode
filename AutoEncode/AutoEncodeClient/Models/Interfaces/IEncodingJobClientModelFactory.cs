using AutoEncodeUtilities.Interfaces;

namespace AutoEncodeClient.Models.Interfaces
{
    public interface IEncodingJobClientModelFactory
    {
        IEncodingJobClientModel Create(IEncodingJobData encodingJobData);

        void Release(IEncodingJobClientModel model);
    }
}
