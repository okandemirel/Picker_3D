using System.Collections.Generic;
using _Modules.ObjectPooling.Scripts.Data.UnityObjects;
using _Modules.ObjectPooling.Scripts.Enums;
using Signals;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Managers
{
    public class PoolingManager : MonoBehaviour
    {
        #region Self Variables

        #region Serialized Variables

        [SerializeField] private CD_Pool data;

        [SerializeField] private Transform poolParent;

        #endregion

        #region Private Variables

        private Dictionary<PoolType, Queue<GameObject>> _poolableObjectList;

        #endregion

        #endregion

        private void Awake()
        {
            data = GetPoolData();
        }

        private CD_Pool GetPoolData()
        {
            return Resources.Load<CD_Pool>("Data/CD_Pool");
        }

        #region Event Subscriptions

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            PoolSignals.Instance.onDequeuePoolableGameObject += OnDequeuePoolableGameObject;
            PoolSignals.Instance.onEnqueuePooledGameObject += OnEnqueuePooledGameObject;
        }

        private GameObject OnDequeuePoolableGameObject(PoolType type)
        {
            if (!_poolableObjectList.ContainsKey(type))
            {
                Debug.LogError($"Dictionary does not contain this key: {type}...");
                return null;
            }

            var deQueuedPoolObject = _poolableObjectList[type].Dequeue();
            if (deQueuedPoolObject.activeSelf) OnDequeuePoolableGameObject(type);
            deQueuedPoolObject.SetActive(true);
            return deQueuedPoolObject;
        }

        private void OnEnqueuePooledGameObject(GameObject poolObject, PoolType type)
        {
            poolObject.transform.parent = poolParent.transform;
            poolObject.transform.localPosition = Vector3.zero;
            poolObject.transform.localEulerAngles = Vector3.zero;

            poolObject.gameObject.SetActive(false);

            _poolableObjectList[type].Enqueue(poolObject);
        }

        private void UnSubscribeEvents()
        {
            PoolSignals.Instance.onDequeuePoolableGameObject -= OnDequeuePoolableGameObject;
            PoolSignals.Instance.onEnqueuePooledGameObject -= OnEnqueuePooledGameObject;
        }

        private void OnDisable()
        {
            UnSubscribeEvents();
        }

        #endregion

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            _poolableObjectList = new Dictionary<PoolType, Queue<GameObject>>();

            foreach (var pool in data.PoolList)
            {
                Queue<GameObject> poolableObjects = new Queue<GameObject>();

                for (int i = 0; i < pool.Value.Amount; i++)
                {
                    var go = Object.Instantiate(pool.Value.Prefab, poolParent, true);
                    go.SetActive(false);
                    pool.Value.Type = pool.Key;


                    poolableObjects.Enqueue(go);
                }

                _poolableObjectList.Add(pool.Key, poolableObjects);
            }
        }
    }
}