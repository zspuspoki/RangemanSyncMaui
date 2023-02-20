namespace RangemanSync.Services
{
    public class ProgressMessagesService
    {
        public string FoundCasioDevice { get; } = "Found Casio device. Trying to connect ...";
        public string SuccessfullyConnectedToWatch { get; } = "Successfully connected to the watch. The watch commands are being sent to the watch. Please wait ...";
        public string FailedToConnectToWatch { get; } = "Failed to connect to watch";
        public string ConnectionSuccessfullyAborted { get; } = "Bluetooth: GPR-B1000 device scanning successfully aborted.";
        public string ErrorOccuredDuringDisconnectingWatch { get; } = "An unexpected error occured during disconnecting the watch.";

        public string PleaseConnectWatch { get; } = "Looking for Casio GPR-B1000 device. Please connect your watch by pressing and holding the bottom left button on it.";
        public string InitalStartMessage { get; } = "Press the download headers button to start downloading the previously recorded routes from the watch.";
        public string ZeroHeaders { get; } = "Headers downloading resulted 0 headers. Please make sure you have recorded routes on the watch. If yes, then please try again because the transmission has been terminated by the watch.";
        public string WatchCommandSendingError { get; } = "An error occured during sending watch commands. Please try to connect again";
        public string PleaseSelectLogHeader { get; } = "Please select a log header from the list or start downloading the list by using the download headers button if you haven't done it yet.";
        public string ThirtyMinutesOldHeader { get; } = "The header data is more than 30 minutes old. Please download the headers again by pressing the Download headers button.";
        public string TransmissionEndedWithoutReceivingAllData { get; } = "The data downloading from the watch has been ended without receiving all of the data including the end transmission command. Please try again by pressing the download as GPX button again.";
        public string GPXSuccessfullySaved { get; } = "GPX file has been successfully saved to the selected folder.";
    }
}
