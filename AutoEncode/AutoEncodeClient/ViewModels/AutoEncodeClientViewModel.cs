using AutoEncodeClient.ViewModels.Interfaces;

namespace AutoEncodeClient.ViewModels;

public class AutoEncodeClientViewModel :
    ViewModelBase,
    IAutoEncodeClientViewModel
{
    #region Dependencies
    public ISourceFilesViewModel SourceFilesViewModel { get; set; }

    public IEncodingJobQueueViewModel EncodingJobQueueViewModel { get; set; }
    #endregion Dependencies

    /// <summary>Default Constructor </summary>
    public AutoEncodeClientViewModel() { }

    public void Initialize()
    {
        RegisterChildViewModel(SourceFilesViewModel);
        RegisterChildViewModel(EncodingJobQueueViewModel);

        SourceFilesViewModel.Initialize();
        EncodingJobQueueViewModel.Initialize();
    }

    public void Shutdown()
    {
        SourceFilesViewModel?.Shutdown();
        EncodingJobQueueViewModel?.Shutdown();
    }
}
