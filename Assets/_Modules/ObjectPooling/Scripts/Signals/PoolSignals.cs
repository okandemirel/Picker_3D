using System;
using _Modules.ObjectPooling.Scripts.Enums;
using Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Signals
{
    public class PoolSignals : MonoSingleton<PoolSignals>
    {
        public Func<PoolType, GameObject> onDequeuePoolableGameObject = delegate { return null; };
        public UnityAction<GameObject, PoolType> onEnqueuePooledGameObject = delegate { };
    }
}