using _Modules.SaveModule.Scripts.Interfaces;

namespace _Modules.SaveModule.Scripts.Data
{
    public class SaveConfigData
    {
        private const string _extention = ".es3";
        private string _path;
        private string _dataKey;

        public string GetKeyName<T>(T dataToSave) where T : ISaveableEntity
        {
            _dataKey = dataToSave.GetType().Name;
            return _dataKey;
        }

        public string GetPathName<T>(T dataToSave) where T : ISaveableEntity
        {
            _path = dataToSave.GetType().Name + _extention;
            return _path;
        }
    }
}