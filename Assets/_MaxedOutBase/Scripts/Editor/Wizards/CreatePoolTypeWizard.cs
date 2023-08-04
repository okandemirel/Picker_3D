using System;
using System.Collections.Generic;
using _MaxedOutBase.Scripts.Editor.Settings;
using _MaxedOutBase.Scripts.Editor.Settings.CodeGenerationOperations;
using JetBrains.Annotations;
using UnityEditor;

namespace _MaxedOutBase.Scripts.Editor.Wizards
{
    public class CreatePoolTypeWizard : ScriptableWizard
    {
        [MenuItem("MaxedOutEntertainment/Add PoolType")]
        [UsedImplicitly]
        private static void CreateWizard()
        {
            DisplayWizard("Add Property Panel", typeof(CreatePoolTypeWizard), "Add");
        }

        private static CodeGenerationSettings _settings;
        private CodeGenerationOperationConfig _operationConfig;

        public string Name;

        [UsedImplicitly]
        private void OnWizardCreate()
        {
            if (string.IsNullOrEmpty(Name))
                return;

            _settings = AssetDatabase.LoadAssetAtPath<CodeGenerationSettings>(CodeGenPaths.SETTINGS_PATH);
            _operationConfig = _settings.PoolKeyGenerationConfig;

            Dictionary<Type, object> startArgs = new Dictionary<Type, object>()
            {
                {
                    typeof(PoolTypeCodeOperation),
                    new PoolTypeCodeOperation.StartArgs()
                    {
                        Name = this.Name
                    }
                }
            };

            foreach (CodeGenerationOperation operation in _operationConfig.Operations)
                operation.Begin(startArgs);

            //Create necessary operate args.
            Dictionary<Type, object> operateArgs = new Dictionary<Type, object>() { };
            foreach (CodeGenerationOperation operation in _operationConfig.Operations)
                operation.Operate(operateArgs);

            EditorPrefs.SetString("Name", Name);
            AssetDatabase.Refresh();
        }
    }
}