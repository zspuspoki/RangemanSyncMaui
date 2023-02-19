using Android.Content;

namespace RangemanSync.Platforms.Android
{
    public class SaveFileService : ISaveTextFileService
    {
        private readonly MainActivity activity;

        public SaveFileService(MainActivity activity)
        {
            this.activity = activity;
        }

        public void SaveFile(string fileName)
        {
            Intent intentCreate = new Intent(Intent.ActionCreateDocument);
            intentCreate.AddCategory(Intent.CategoryOpenable);
            intentCreate.SetType("application/gpx+xml");
            intentCreate.PutExtra(Intent.ExtraTitle, fileName);
            activity.StartActivityForResult(intentCreate, ActivityRequestCode.SaveGPXFile);
        }

        public void SaveFile(string fileName, string fileContent)
        {
            throw new NotImplementedException();
        }
    }
}
