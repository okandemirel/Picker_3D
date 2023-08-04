using _MaxedOutBase.Scripts.Editor.Settings.CodeGenerationOperations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace _MaxedOutBase.Scripts.Editor.Settings
{
    [CreateAssetMenu(fileName = "CodeGenerationSettings",
        menuName = "MaxedOutEntertainment/Admin/Code Generation Settings")]
    public class CodeGenerationSettings : ScriptableObject
    {
        [SerializeField] private CodeGenerationOperationConfig _poolKeyGenerationConfig;
        [SerializeField] private CodeGenerationOperationConfig _clickerTemplateGenerationConfig;

        public CodeGenerationOperationConfig PoolKeyGenerationConfig
        {
            get => _poolKeyGenerationConfig;
        }
        public CodeGenerationOperationConfig ClickerTemplateGenerationConfig
        {
            get => _clickerTemplateGenerationConfig;
        }

        [SerializeField] [BoxGroup("MaxedOutEntertainment-Base Paths")]
        private DefaultAsset _testTemplateFolderInfo;

        public string TestTemplatePath
        {
            get => GetFolderInfoFolderPath(_testTemplateFolderInfo);
        }

        [SerializeField] [BoxGroup("Project Paths")] [FolderPath]
        private string _projectResourcesPath;

        public string ProjectResourcesPath
        {
            get => _projectResourcesPath;
        }

        private string GetFolderInfoFolderPath(Object asset)
        {
            return AssetDatabase.GetAssetPath(asset).Replace("/" + asset.name + ".folderInfo", "");
        }

        [SerializeField] [BoxGroup("Project Paths")] [FolderPath]
        private string _projectEnumsPath;

        public string ProjectEnumsPath
        {
            get => _projectEnumsPath;
        }

        [SerializeField] [BoxGroup("Template Paths")] [FolderPath]
        private string _projectTemplatePath;

        public string ProjectTemplatePath
        {
            get => _projectTemplatePath;
        }
    }
}