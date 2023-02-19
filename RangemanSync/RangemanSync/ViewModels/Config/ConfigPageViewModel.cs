using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetroLog.Maui;

namespace RangemanSync.ViewModels.Config
{
    public partial class ConfigPageViewModel : BaseViewModel
    {
        public IAsyncRelayCommand ApplyCommand { get; }

        public ConfigPageViewModel(ISaveTextFileService saveTextFileService)
        {
            ApplyCommand = new AsyncRelayCommand(ApplySettings);
            this.saveTextFileService = saveTextFileService;
        }

        [ObservableProperty]
        bool downloadLogFiles;

        [ObservableProperty]
        string progressMessage;

        private readonly ISaveTextFileService saveTextFileService;

        private async Task ApplySettings()
        {
            if(downloadLogFiles)
            {
                var logController = new LogController();
                var logTextEntries = await logController.GetLogList();
                var logText = string.Join('\n', logTextEntries);
                saveTextFileService.SaveFile("logs.txt", logText);

            }
        }
    }
}
