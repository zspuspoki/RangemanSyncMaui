using Mapsui.UI.Maui;

namespace RangemanSync
{
    public interface IMapPageView
    {
        void PlaceOnMapClicked(Position p);
        void UpdateMapToUseMbTilesFile();
        void UpdateMapToUseWebBasedMbTiles();
        void AddLinesBetweenPinsAsLayer();
        void RemoveSelectedPin();
        Task DisplayAlert(string title, string message, string button);
        void ShowOnMap(Position p);
        void DisplayProgressMessage(string message);
    }
}
