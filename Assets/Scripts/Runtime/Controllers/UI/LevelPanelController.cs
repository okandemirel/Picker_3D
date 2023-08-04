using Runtime.Signals;
using Signals;
using TMPro;
using UnityEngine;

namespace Runtime.Controllers.UI
{
    public class LevelPanelController : MonoBehaviour
    {
        #region Self Variables

        #region Serialized Variables

        [SerializeField] private TextMeshProUGUI levelText;

        #endregion

        #endregion

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            UISignals.Instance.onSetNewLevelValue += OnSetNewLevelValue;
        }

        private void UnsubscribeEvents()
        {
            UISignals.Instance.onSetNewLevelValue -= OnSetNewLevelValue;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        public void OnSetNewLevelValue(int levelValue)
        {
            levelText.text = "LEVEL " + ++levelValue;
        }
    }
}