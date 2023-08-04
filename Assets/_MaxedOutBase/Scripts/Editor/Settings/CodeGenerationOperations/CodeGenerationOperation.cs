using System;
using System.Collections.Generic;
using UnityEngine;

namespace _MaxedOutBase.Scripts.Editor.Settings.CodeGenerationOperations
{
    public abstract class CodeGenerationOperation : ScriptableObject
    {
        [SerializeField] protected CodeGenerationSettings _sharedSettings;
        public abstract void Begin(Dictionary<Type, object> args);
        public abstract void Operate(Dictionary<Type, object> args);
    }

    //[CreateAssetMenu(fileName = "TSelf", menuName = "Rich/Admin/Code Generation Operations/'TSelf'")]
    public abstract class CodeGenerationOperation<TSelf, TStartArgs, TOperateArgs> : CodeGenerationOperation
        where TSelf : CodeGenerationOperation<TSelf, TStartArgs, TOperateArgs>
    {
        public sealed override void Begin(Dictionary<Type, object> args)
        {
            TStartArgs arg = default;
            if (args != null)
            {
                if (args.ContainsKey(typeof(TSelf)))
                {
                    object obj = args[typeof(TSelf)];
                    if (obj is TStartArgs tArg)
                    {
                        arg = tArg;
                    }
                }
            }

            OnBegin(arg);
        }

        public sealed override void Operate(Dictionary<Type, object> args)
        {
            TOperateArgs arg = default;
            if (args != null)
            {
                if (args.ContainsKey(typeof(TSelf)))
                {
                    object obj = args[typeof(TSelf)];
                    if (obj is TOperateArgs tArg)
                    {
                        arg = tArg;
                    }
                }
            }

            OnOperate(arg);
        }

        protected virtual void OnBegin(TStartArgs arg)
        {
        }

        protected abstract void OnOperate(TOperateArgs arg);
    }
}