using Managers;
using Runtime.Interfaces;
using Runtime.Managers;
using UnityEngine;

namespace Runtime.Commands
{
    public class LevelDestroyerCommand : ICommand
    {
        private LevelManager _levelManager;

        public LevelDestroyerCommand(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void Execute()
        {
            Object.Destroy(_levelManager.levelHolder.transform.GetChild(0).gameObject);
        }
    }
}