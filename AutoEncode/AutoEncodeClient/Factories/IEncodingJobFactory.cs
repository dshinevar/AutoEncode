using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels.EncodingJob.Interfaces;
using AutoEncodeUtilities.Data;

namespace AutoEncodeClient.Factories;

public interface IEncodingJobClientModelFactory
{
    #region EncodingJobClientModel
    IEncodingJobClientModel Create(EncodingJobData encodingJobData);

    void Release(IEncodingJobClientModel model);
    #endregion EncodingJobClientModel

    #region EncodingJobViewModel
    IEncodingJobViewModel Create(IEncodingJobClientModel encodingJobClientModel);

    void Release(IEncodingJobViewModel viewModel);
    #endregion EncodingJobViewModel
}
