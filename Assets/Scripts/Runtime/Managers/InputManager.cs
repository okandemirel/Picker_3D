using System.Collections.Generic;
using Runtime.Data.UnityObject;
using Runtime.Keys;
using Runtime.Signals;
using Signals;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runtime.Managers
{
    public class InputManager : MonoBehaviour
    {
        #region Singleton

        public static InputManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        #endregion

        #region Self Variables

        #region Public Variables

        [Header("Data")] public CD_Input InputData;
        [Header("Additional Variables")] public bool IsAvailableForTouch;

        #endregion

        #region Serialized Variables

        [SerializeField] private bool isFirstTimeTouchTaken;

        #endregion

        #region Private Variables

        private float _positionValuesX;

        private bool _isTouching;

        private float _currentVelocity; //ref type
        private Vector2? _mousePosition; //ref type
        private Vector3 _moveVector; //ref type

        #endregion

        #endregion

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            CoreGameSignals.Instance.onReset += ResetData;
            CoreGameSignals.Instance.onPlay += OnPlay;
            InputSignals.Instance.onChangeInputState += OnChangeInputState;
        }

        private void UnSubscribeEvents()
        {
            CoreGameSignals.Instance.onReset -= ResetData;
            CoreGameSignals.Instance.onPlay -= OnPlay;
            InputSignals.Instance.onChangeInputState -= OnChangeInputState;
        }

        private void OnDisable()
        {
            UnSubscribeEvents();
        }


        private void Update()
        {
            if (!IsAvailableForTouch) return;

            if (Input.GetMouseButtonUp(0) && !IsPointerOverUIElement())
            {
                _isTouching = false;

                InputSignals.Instance.onInputReleased?.Invoke();
                InputSignals.Instance.onChangeInputState?.Invoke(false);
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUIElement())
            {
                _isTouching = true;
                InputSignals.Instance.onInputTaken?.Invoke();
                if (!isFirstTimeTouchTaken)
                {
                    isFirstTimeTouchTaken = true;
                    //onFirstTimeTouchTaken?.Invoke();
                }

                _mousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(0) && !IsPointerOverUIElement())
            {
                if (_isTouching)
                {
                    if (_mousePosition != null)
                    {
                        Vector2 mouseDeltaPos = (Vector2) Input.mousePosition - _mousePosition.Value;


                        if (mouseDeltaPos.x > InputData.Data.HorizontalInputSpeed)
                            _moveVector.x = InputData.Data.HorizontalInputSpeed / 10f * mouseDeltaPos.x;
                        else if (mouseDeltaPos.x < -InputData.Data.HorizontalInputSpeed)
                            _moveVector.x = -InputData.Data.HorizontalInputSpeed / 10f * -mouseDeltaPos.x;
                        else
                            _moveVector.x = Mathf.SmoothDamp(_moveVector.x, 0f, ref _currentVelocity,
                                InputData.Data.HorizontalInputClampStopValue);

                        _mousePosition = Input.mousePosition;

                        InputSignals.Instance.onInputDragged?.Invoke(new HorizontalnputParams()
                        {
                            HorizontalInputValue = _moveVector.x,
                            HorizontalInputClampNegativeSide = InputData.Data.HorizontalInputClampNegativeSide,
                            HorizontalInputClampPositiveSide = InputData.Data.HorizontalInputClampPositiveSide
                        });
                    }
                }
            }
        }

        private void OnPlay()
        {
            IsAvailableForTouch = true;
        }


        private void OnChangeInputState(bool state)
        {
            IsAvailableForTouch = state;
        }

        private bool IsPointerOverUIElement()
        {
            var eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        private void ResetData()
        {
            _isTouching = false;
            isFirstTimeTouchTaken = false;
        }
    }
}