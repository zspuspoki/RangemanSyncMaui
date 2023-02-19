namespace RangemanSync
{
    public interface ISaveTextFileService
    {
        void SaveFile(string fileName);

        void SaveFile(string fileName, string fileContent);
    }
}
