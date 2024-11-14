using System.ComponentModel;

namespace AutoEncodeClient.ViewModels.Interfaces;

public interface IEncodingJobQueueViewModel : IViewModel
{
    ICollectionView EncodingJobsView { get; }

    void Initialize();

    void Shutdown();
}
