using System.Collections.Generic;
using Runtime.Enums;
using Runtime.Signals;
using Signals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Controllers.UI
{
    public class UIPanelController : MonoBehaviour
    {
        #region Self Variables

        #region Serialized Variables

        [SerializeField] private List<GameObject> layers = new List<GameObject>();

        #endregion

        #endregion

        #region Event Subscriptions

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            CoreUISignals.Instance.onOpenPanel += OnOpenPanel;
            CoreUISignals.Instance.onClosePanel += OnClosePanel;
            CoreUISignals.Instance.onCloseAllPanels += OnCloseAllPanel;
        }

        private void UnsubscribeEvents()
        {
            CoreUISignals.Instance.onOpenPanel -= OnOpenPanel;
            CoreUISignals.Instance.onClosePanel -= OnClosePanel;
            CoreUISignals.Instance.onCloseAllPanels += OnCloseAllPanel;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        #endregion

        [Button("OpenPanel")]
        private void OnOpenPanel(UIPanelTypes panel, int layerValue)
        {
            CoreUISignals.Instance.onClosePanel?.Invoke(layerValue);
            Instantiate(Resources.Load<GameObject>($"Screens/{panel}Panel"), layers[layerValue].transform);
        }

        [Button("ClosePanel")]
        private void OnClosePanel(int layerValue)
        {
            if (layers[layerValue].transform.childCount > 0)
                for (int i = 0; i < layers[layerValue].transform.childCount; i++)
                {
                    Destroy(layers[layerValue].transform.GetChild(i).gameObject);
                }
        }

        [Button("CloseAllPanels")]
        private void OnCloseAllPanel()
        {
            foreach (var layer in layers)
            {
                for (int i = 0; i < layer.transform.childCount; i++)
                {
                    Destroy(layer.transform.GetChild(i).gameObject);
                }
            }
        }
    }
}