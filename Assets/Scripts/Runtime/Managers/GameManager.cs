//using Tabtale.TTPlugins;

using _Modules.SaveModule.Scripts.Data;
using Extensions;
using Managers;
using Runtime.Enums;
using Runtime.Signals;
using Signals;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    #region Self Variables

    #region Public Variables

    public GameStates States;

    #endregion
    
    #endregion

    protected override void Awake()
    {
        Application.targetFrameRate = 60;
        //TTPCore.Setup();
    }

    private void OnEnable()
    {
        CoreGameSignals.Instance.onChangeGameStates += OnChangeGameState;
    }

    private void OnDisable()
    {
        CoreGameSignals.Instance.onChangeGameStates -= OnChangeGameState;
    }
    
    private void OnChangeGameState(GameStates newState)
    {
        States = newState;
    }

   
}