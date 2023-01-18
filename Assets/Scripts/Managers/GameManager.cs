using System;
using System.Collections.Generic;
using Cards;
using Player;
using Rooms;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using Utils.SaveUtils;
using Object = UnityEngine.Object;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private PlayerController _playerController;
        [SerializeField] private UIController _uiController;
        [SerializeField] private bool _chooseCards = true;
        [SerializeField] private CardManager _cardManager;

        #endregion

        #region Non-Serialized Fields

        private static GameManager _instance;
        private CharacterMap _controls; // for input callbacks
        private bool _playerControllerEnabled;
        private bool _uiControllerEnabled;
        private ActionMap _actionMapInUse;

        private static float _fixedTimeScale;

        #endregion

        #region Properties

        public static PlayerController PlayerController => _instance._playerController;
        public static Transform PlayerTransform { get; private set; }

        public static bool PlayerControllerEnabled
        {
            set
            {
                _instance._playerControllerEnabled = value;
                if (value && _instance._actionMapInUse is ActionMap.PLAYER)
                    _instance._controls?.Player.Enable();
                else
                    _instance._controls?.Player.Disable();
            }
        }

        public static bool UIControllerEnabled
        {
            set
            {
                _instance._uiControllerEnabled = value;
                if (value && _instance._actionMapInUse is ActionMap.UI)
                    _instance._controls?.UI.Enable();
                else
                    _instance._controls?.UI.Disable();
            }
        }

        private ActionMap ActionMapInUse
        {
            set
            {
                _actionMapInUse = value;
                switch (_actionMapInUse)
                {
                    case ActionMap.PLAYER when _playerControllerEnabled:
                        _controls.Player.Enable();
                        _controls.UI.Disable();
                        break;
                    case ActionMap.UI when _uiControllerEnabled:
                        _controls.UI.Enable();
                        _controls.Player.Disable();
                        break;
                }
            }
        }

        #endregion
        
        #region Events

        public static event Action FinishedCurrentRoom;
        public static event Action<float> TimeScaleChanged;
        
        #endregion

        #region Function Events

        private void Awake()
        {
            if (_instance != null)
                throw new DoubleGameManagerException();

            Init();
        }

        private void Init()
        {
            _instance = this;
            PlayerTransform = _playerController.transform;
            _fixedTimeScale = Time.fixedDeltaTime;
        }

        private void Start()
        {
            if (_controls == null)
            {
                _controls = new CharacterMap();
                _controls.Player.SetCallbacks(_playerController);
                _controls.UI.SetCallbacks(_uiController);
                ActionMapInUse = ActionMap.PLAYER;
            }
        }

        #endregion

        #region Public Methods

        public static void ScaleTime(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = timeScale * _fixedTimeScale;
            
            TimeScaleChanged?.Invoke(timeScale);
        }
        
        public static void RoomCleared()
        {
            if (_instance._chooseCards)
            {
                _instance._cardManager.ShowCards(CardChosen);
                _instance.ActionMapInUse = ActionMap.UI;
            }
            else
            {
                FinishedCurrentRoom?.Invoke();
            }
        }

        public static void PlayerKilled()
        {
            // LoadManager.ReloadStartingScene();
            LoadManager.LoadLose();
        }
        
        public static void BossKilled()
        {
            // LoadManager.ReloadStartingScene();
            LoadManager.LoadWin();
        }

        #endregion

        #region Private Methods
        
        private static void CardChosen()
        {
            _instance.ActionMapInUse = ActionMap.PLAYER;
            FinishedCurrentRoom?.Invoke();
        }

        private void SaveData()
        {
            SaveSystem.SaveData(new (){{SaveSystem.DataType.PLAYER, _playerController}});
        }

        private void LoadData()
        {
            SaveSystem.LoadData(new (){{SaveSystem.DataType.PLAYER, _playerController}});
        }

        #endregion

        #region Classes

        private class DoubleGameManagerException : Exception
        {
        }

        #endregion

        #region Enums

        private enum ActionMap : byte
        {
            PLAYER,
            UI,
        }

        #endregion
    }
}