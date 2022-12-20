using WindowsFolderPicker = Windows.Storage.Pickers.FolderPicker;

namespace RangemanSync.Platforms.Windows
{
    public class SaveGPXFileService : ISaveGPXFileService
    {
        public async void SaveGPXFile(string fileName, string fileContent)
        {
            var folderPicker = new WindowsFolderPicker();
            // Might be needed to make it work on Windows 10
            folderPicker.FileTypeFilter.Add("*");

            // Get the current window's HWND by passing in the Window object
            var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;

            // Associate the HWND with the file picker
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var result = await folderPicker.PickSingleFolderAsync();

            if(result != null)
            {
                File.WriteAllText(Path.Combine(result.Path, fileName), fileContent);
            }
        }

        public void SaveGPXFile(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
