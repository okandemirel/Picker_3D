using System;
using Enums;
using Runtime.Extensions;
using UnityEngine.Events;

namespace Runtime.Signals
{
    public class CoreGameSignals : MonoSingleton<CoreGameSignals>
    {
        public UnityAction<GameStates> onChangeGameState = delegate { };
        public UnityAction<byte> onLevelInitialize = delegate { };
        public UnityAction onClearActiveLevel = delegate { };
        public UnityAction onLevelSuccessful = delegate { };
        public UnityAction onLevelFailed = delegate { };
        public UnityAction onNextLevel = delegate { };
        public UnityAction onRestartLevel = delegate { };
        public UnityAction onPlay = delegate { };
        public UnityAction onReset = delegate { };
        public Func<byte> onGetLevelValue = delegate { return 0; };

        public UnityAction<byte> onStageAreaSuccessful = delegate { };
        public UnityAction onStageAreaEntered = delegate { };
        public UnityAction onFinishAreaEntered = delegate { };
        public UnityAction onMiniGameAreaEntered = delegate { };
        public UnityAction onMultiplierAreaEntered = delegate { };
    }
}