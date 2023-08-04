using System;
using System.IO;
using System.Text;
using _MaxedOutBase.Scripts.Editor.Settings;
using _MaxedOutBase.Scripts.Editor.Settings.CodeGenerationOperations;
using _MaxedOutBase.Scripts.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace _MaxedOutBase.Scripts.Editor
{
    [CreateAssetMenu(fileName = "PoolKeyCodeOperation",
        menuName = "MaxedOutEntertainment/Admin/Code Generation Operations/PoolKeyCodeOperation")]
    public class PoolTypeCodeOperation : CodeGenerationOperation
    <PoolTypeCodeOperation,
        PoolTypeCodeOperation.StartArgs,
        PoolTypeCodeOperation.OperateArgs>
    {
        public struct StartArgs
        {
            public string Name;
        }

        public struct OperateArgs
        {
        }

        private string _name;

        protected override void OnBegin(StartArgs arg)
        {
            base.OnBegin(arg);
            DirectoryHelpers.EnsurePathExistence(_sharedSettings.ProjectEnumsPath);
            _name = arg.Name;
        }

        protected override void OnOperate(OperateArgs arg)
        {
            string poolKeyPath = _sharedSettings.ProjectEnumsPath + "/" + "PoolType.cs";
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(poolKeyPath);

            string data;

            if (obj == null)
            {
                data = LoadTemplate();

                Debug.Log("File didn't existed generating...");
            }
            else
            {
                data = LoadFileOnPath(poolKeyPath);

                Debug.Log("Changing on existing file...");
            }

            if (data.Contains(_name))
            {
                Debug.Log("Constant already exists");
                return;
            }

            string addition = "\r\t\t";
            addition += "//-%Name%";
            addition += "\r\t\t";
            addition += "%Name%,";
            addition += "//-";
            addition += "\r\t\t";
            addition += "ADDPOINT";
            data = data.Replace("//*ADDITION*//", addition);
            data = data.Replace("%Name%", _name);
            data = data.Replace("ADDPOINT", "//*ADDITION*//");
            CodeUtilities.SaveFile(data, poolKeyPath);
            Debug.Log("Added PoolType");
        }

        private string LoadTemplate()
        {
            try
            {
                string data = string.Empty;
                string path = _sharedSettings.TestTemplatePath + "/TemplatePoolType" + ".txt";
                StreamReader theReader = new StreamReader(path, Encoding.Default);
                using (theReader)
                {
                    data = theReader.ReadToEnd();
                    theReader.Close();
                }

                return data;
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}\n", e.Message);
                return string.Empty;
            }
        }

        private string LoadFileOnPath(string filePath)
        {
            try
            {
                Debug.Log("Loading File = " + filePath);
                string data = string.Empty;
                string path = filePath;
                StreamReader theReader = new StreamReader(path, Encoding.Default);
                using (theReader)
                {
                    data = theReader.ReadToEnd();
                    theReader.Close();
                }

                return data;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return string.Empty;
            }
        }
    }
}