using Android.Content;

namespace RangemanSync.Platforms.Android
{
    public class SaveGPXFileService : ISaveGPXFileService
    {
        private readonly MainActivity activity;

        public SaveGPXFileService(MainActivity activity)
        {
            this.activity = activity;
        }

        public void SaveGPXFile(string fileName)
        {
            Intent intentCreate = new Intent(Intent.ActionCreateDocument);
            intentCreate.AddCategory(Intent.CategoryOpenable);
            intentCreate.SetType("application/gpx+xml");
            intentCreate.PutExtra(Intent.ExtraTitle, fileName);
            activity.StartActivityForResult(intentCreate, ActivityRequestCode.SaveGPXFile);
        }

        public void SaveGPXFile(string fileName, string fileContent)
        {
            throw new NotImplementedException();
        }
    }
}
