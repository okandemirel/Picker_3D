using Extensions;
using UnityEngine.Events;

namespace Runtime.Signals
{
    public class UISignals : MonoSingleton<UISignals>
    {
        public UnityAction onUpdateThrowableCount = delegate { };
        public UnityAction onUpdateLeftEnemyCount = delegate { };
        public UnityAction<int> onSetNewLevelValue = delegate { };
    }
}