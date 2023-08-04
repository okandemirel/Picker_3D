using Managers;
using Runtime.Interfaces;
using Runtime.Managers;
using Unity.VisualScripting;
using UnityEngine;

namespace Runtime.Commands
{
    public class LevelLoaderCommand : ICommand
    {
        private LevelManager _levelManager;

        public LevelLoaderCommand(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void Execute(int parameter)
        {
            var resourceRequest = Resources.LoadAsync<GameObject>($"Prefabs/LevelPrefabs/level {parameter}");
            resourceRequest.completed += operation =>
            {
                var newLevel = Object.Instantiate(ComponentHolderProtocol.GameObject(resourceRequest.asset),
                    Vector3.zero, Quaternion.identity);
                if (newLevel != null) newLevel.transform.SetParent(_levelManager.levelHolder.transform);
            };
        }
    }
}