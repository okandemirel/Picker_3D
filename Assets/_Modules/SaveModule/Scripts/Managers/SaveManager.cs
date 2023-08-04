using _Modules.SaveModule.Scripts.Commands;
using _Modules.SaveModule.Scripts.Data;
using _Modules.SaveModule.Scripts.Interfaces;

namespace _Modules.SaveModule.Scripts.Managers
{
    public class SaveManager
    {
        #region Self Variables

        #region Private Variables

        private LoadCommand _loadCommand;

        private SaveCommand _saveCommand;
        private SaveConfigData _saveConfig;
        private string _key;
        private string _path;

        #endregion

        #endregion
        
        public SaveManager()
        {
            Init();
        }
        private void Init()
        {
            _saveConfig = new SaveConfigData();
            _loadCommand = new LoadCommand();
            _saveCommand = new SaveCommand();
        }

        public T PreLoadData<T>(T gameData) where T : ISaveableEntity
        {
            _path=_saveConfig.GetPathName(gameData);
            _key=_saveConfig.GetKeyName(gameData);
            T dataInstance;
            if (!ES3.FileExists(_path))
            {
                if (!ES3.KeyExists(_key))
                {
                    dataInstance=gameData;
                    _saveCommand.Execute<T>(dataInstance,_key,_path);
                    return dataInstance;
                }
            }
            dataInstance=_loadCommand.Execute<T>(_key,_path);
            return dataInstance;
        }
        public void PreSaveData<T>(T gameData) where T : ISaveableEntity
        {
            _path=_saveConfig.GetPathName(gameData);
            _key=_saveConfig.GetKeyName(gameData);
            _saveCommand.Execute<T>(gameData,_key,_path);
        }
        
    }
}