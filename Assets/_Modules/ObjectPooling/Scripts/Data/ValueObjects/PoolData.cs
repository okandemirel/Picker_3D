using System;
using _Modules.ObjectPooling.Scripts.Enums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Modules.ObjectPooling.Scripts.Data.ValueObjects
{
    [Serializable]
    [HideReferenceObjectPicker]
    public class PoolData
    {
        public int Amount;
        public GameObject Prefab;

        // public Attribute Data; 

        [HideInInspector] public PoolType Type;
    }
}