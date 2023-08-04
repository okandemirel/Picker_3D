using Cinemachine;
using Runtime.Signals;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime.Managers
{
    public class CameraManager : MonoBehaviour
    {
        #region Self Variables

        #region Serialized Variables

        [SerializeField] private CinemachineVirtualCamera virtualCamera;

        #endregion

        #region Private Variables

        private float3 _firstPosition;

        #endregion

        #endregion

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            _firstPosition = transform.position;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            CameraSignals.Instance.onSetCameraTarget += OnSetCameraTarget;
            CoreGameSignals.Instance.onReset += OnReset;
        }

        private void OnSetCameraTarget()
        {
            var player = FindObjectOfType<PlayerManager>().transform;
            virtualCamera.Follow = player;
            //virtualCamera.LookAt = player;
        }

        private void OnReset()
        {
            transform.position = _firstPosition;
        }

        private void UnSubscribeEvents()
        {
            CameraSignals.Instance.onSetCameraTarget -= OnSetCameraTarget;
            CoreGameSignals.Instance.onReset -= OnReset;
        }

        private void OnDisable()
        {
            UnSubscribeEvents();
        }
    }
}