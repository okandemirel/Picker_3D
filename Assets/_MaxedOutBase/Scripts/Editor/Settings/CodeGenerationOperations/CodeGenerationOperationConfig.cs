using System.Collections.Generic;
using UnityEngine;

namespace _MaxedOutBase.Scripts.Editor.Settings.CodeGenerationOperations
{
    [CreateAssetMenu(fileName = "CodeGenerationOperationConfig",
        menuName = "MaxedOutEntertainment/Admin/Code Generation Operation Config")]
    public class CodeGenerationOperationConfig : ScriptableObject
    {
        [SerializeField] private List<CodeGenerationOperation> _operations;

        public CodeGenerationOperation[] Operations
        {
            get => _operations.ToArray();
        }
    }
}