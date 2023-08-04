using _Modules.SaveModule.Scripts.Data;
using DG.Tweening;
using Managers;
using Runtime.Commands;
using Runtime.Enums;
using Runtime.Signals;
using Signals;
using UnityEngine;

namespace Runtime.Managers
{
    public class LevelManager : MonoBehaviour
    {
        #region Self Variables

        #region Public Variables

        public LevelManager()
        {
            _levelLoader = new LevelLoaderCommand(this);
            _levelDestroyer = new LevelDestroyerCommand(this);
        }

        #endregion

        #region Serialized Variables

        [Header("Holder")] [SerializeField] internal GameObject levelHolder;

        [Space] [SerializeField] private int totalLevelCount;

        #endregion

        #region Private Variables

        private LevelLoaderCommand _levelLoader;
        private LevelDestroyerCommand _levelDestroyer;
        private GameData _gameData;

        #endregion

        #endregion

        private void Awake()
        {
            AssignSaveData();
        }

        private void AssignSaveData()
        {
            _gameData = SaveDistributorManager.GetSaveData();
        }

        private void OnEnable()
        {
            SubscribeEvents();

            _gameData.Level = GetLevelID();
            CoreGameSignals.Instance.onLevelInitialize?.Invoke(_gameData.Level);
        }

        private void SubscribeEvents()
        {
            CoreGameSignals.Instance.onLevelInitialize += _levelLoader.Execute;
            CoreGameSignals.Instance.onClearActiveLevel += _levelDestroyer.Execute;
            CoreGameSignals.Instance.onGetLevelID += GetLevelID;
            CoreGameSignals.Instance.onNextLevel += OnNextLevel;
            CoreGameSignals.Instance.onRestartLevel += OnRestartLevel;
        }

        private void UnsubscribeEvents()
        {
            CoreGameSignals.Instance.onLevelInitialize -= _levelLoader.Execute;
            CoreGameSignals.Instance.onClearActiveLevel -= _levelDestroyer.Execute;
            CoreGameSignals.Instance.onGetLevelID -= GetLevelID;
            CoreGameSignals.Instance.onNextLevel -= OnNextLevel;
            CoreGameSignals.Instance.onRestartLevel -= OnRestartLevel;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }


        private int GetLevelID()
        {
            return _gameData.Level % totalLevelCount;
        }


        private void OnNextLevel()
        {
            _gameData.Level++;
            SaveDistributorManager.SaveData();
            CoreGameSignals.Instance.onClearActiveLevel?.Invoke();
            DOVirtual.DelayedCall(.1f, () => CoreGameSignals.Instance.onLevelInitialize?.Invoke(GetLevelID()));
            CoreUISignals.Instance.onCloseAllPanels?.Invoke();
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Start, 0);
            
        }

        private void OnRestartLevel()
        {
            CoreGameSignals.Instance.onClearActiveLevel?.Invoke();
            DOVirtual.DelayedCall(.1f, () => CoreGameSignals.Instance.onLevelInitialize?.Invoke(GetLevelID()));
            CoreUISignals.Instance.onCloseAllPanels?.Invoke();
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Start, 0);
            SaveDistributorManager.SaveData();
        }
    }
}