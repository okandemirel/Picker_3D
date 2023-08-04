using _Modules.SaveModule.Scripts.Interfaces;

namespace _Modules.SaveModule.Scripts.Commands
{ 
    public class LoadCommand
    {
        public T Execute<T>(string dataKey,string path) where T : ISaveableEntity
        {
            T objectToReturn = ES3.Load<T>(dataKey,path);
            return objectToReturn;
        }
    
    }
}