using _Modules.SaveModule.Scripts.Interfaces;

namespace _Modules.SaveModule.Scripts.Commands
{
    public class SaveCommand
    {
        public void Execute<T>(T dataToSave,string dataKey,string path) where T : ISaveableEntity
        {
            ES3.Save(dataKey,dataToSave,path);
        }
    }
}