﻿using System;
using System.Data;
using Framework;
using Framework.Common;
using Framework.Core.Manager.Config;
using Framework.Core.Manager.Event;
using Framework.Core.Manager.ResourcesLoad;
using Framework.Core.Singleton;
using GamePlay.Character;
using UnityEngine;
using CharacterController = GamePlay.Character.CharacterController;

namespace GamePlay.Player
{
    [MonoSingletonPath("[Manager]/PlayerManager")]
    public class PlayerManager:MonoSingleton<PlayerManager>
    {
        [NonSerialized]
        public const int PROTAGONIST_ID = 10002;
        
        private const string LOGTag = "PlayerManager";

        [SerializeField]
        private PlayerAttr _playerAttr;

        public PlayerAttr PlayerAttr
        {
            get
            {
                if (_playerAttr != null) return _playerAttr;
                _playerAttr = new PlayerAttr();
                _playerAttr.InitAttrValues();

                return _playerAttr;
            }
        }
        
        [SerializeField]
        private PlayerStatus _playerStatus;

        public PlayerStatus PlayerStatus
        {
            get
            {
                if (_playerStatus != null) return _playerStatus;
                _playerStatus = new PlayerStatus();
                _playerStatus.InitStatusValues();

                return _playerStatus;
            }
        }
        
        private CharacterController CharacterController;

        public void Launch()
        {
            
        }
        public override void Initialize()
        {
            EventManager.Register(EEvent.SCENE_LOAD_FINISHED,OnSceneLoadFinished);
        }
        
        public override void Dispose()
        {
            EventManager.Remove(EEvent.SCENE_LOAD_FINISHED,OnSceneLoadFinished);
        }

        void OnSceneLoadFinished(dynamic sceneCf)
        {
            if (CharacterController != null)
            {
                return;
            }
            var loadPlayer = sceneCf != null && sceneCf["loadPlayer"];
            if (loadPlayer)
            {
                LoadPlayerController(sceneCf);
            }
        }

        private Transform CharacterControllerRoot;

        void LoadPlayerController(dynamic sceneCf)
        {
            LogManager.Log(LOGTag,"LoadPlayerController");
            var playerCf = ConfigManager.GetConfigByID(EConfig.Character, PROTAGONIST_ID);
            var modelPath = playerCf["modelPath"];
            var ctrlType = playerCf["ctrlType"];
            var ctrlCfList = ConfigManager.GetConfig(EConfig.CharacterController);
            dynamic ctrlCf = null;
            for (int i = 0; i < ctrlCfList.Count; i++)
            {
                if (ctrlCfList[i]["ctrlType"] == ctrlType)
                {
                    ctrlCf = ctrlCfList[i];
                    break;
                }
            }

            if (ctrlCf != null)
            {
                var initPos = Vector3.zero;
                if (sceneCf != null)
                {
                    var cfInitPos = sceneCf["initPos"];
                    if (cfInitPos != null)
                    {
                        initPos = cfInitPos;
                        LogManager.Log(LOGTag,"CharacterCtrl initPos:",initPos);
                    }
                }
                GameObject ctrlGo = Instantiate(ResourcesLoadManager.LoadAsset<GameObject>(ctrlCf["path"]), initPos, Quaternion.identity);
                CharacterController = ctrlGo.GetComponent<CharacterController>();
                ctrlGo.name = "CharacterController";
                CharacterControllerRoot = ctrlGo.transform;
                DontDestroyOnLoad(CharacterControllerRoot);
                if ((DEF.ECharacterControllerType)ctrlCf["ctrlType"] != DEF.ECharacterControllerType.FPS)
                {
                    GameObject modelGo = Instantiate(ResourcesLoadManager.LoadAsset<GameObject>(modelPath));
                    var animCtrl = modelGo.AddComponent<AnimationControl>();
                    animCtrl.Init(ctrlGo.GetComponent<MovementController>());
                    var mt = modelGo.transform;
                    
                    // LogManager.Log(LOGTag,"LoadPlayerController",mt==null,cc==null);
                    CommonUtils.ResetGO(mt,CharacterController.ModelMountTrs);
                }
            }
        }

        private void Update()
        {
            UpdateStatus();
            UpdateAttr();
        }

        void UpdateStatus()
        {
            
        }

        void UpdateAttr()
        {
            
        }   
    }
}