using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace _MaxedOutBase.Scripts.Editor.Settings
{
    public static class DirectoryHelpers
    {
        public static string CreateOrGetFolderPath(string path,string folderName)
        {
            EnsurePathExistence(path);
            
            if (!AssetDatabase.IsValidFolder(path + "/" + folderName))
                AssetDatabase.CreateFolder(path, folderName);
            return path + "/" + folderName;
        }

        /// <summary>
        /// If directory path doesn't exist it will create, if it exist it will do nothing.
        /// </summary>
        /// <param name="path"></param>
        public static void EnsurePathExistence(string path)
        {
            string[] splitFoldersArray = path.Split('/');
            List<string> splitFolders = splitFoldersArray.ToList();
            splitFolders.RemoveAt(0); //Removing Assets directory it's special.

            //Ensure path exists.
            string directory = "Assets";
            foreach (string folder in splitFolders)
            {
                if (!AssetDatabase.IsValidFolder(directory+"/"+folder))
                    AssetDatabase.CreateFolder(directory, folder);

                directory += "/" + folder;
            }
        }
        
        public static string GetFolderInfoFolderPath(UnityEngine.Object asset)
        {
            return AssetDatabase.GetAssetPath(asset).Replace("/" + asset.name + ".folderInfo","");
        }
    }
}