﻿// author:KIPKIPS
// describe:管理UI框架

using System;
using System.Collections.Generic;
using Framework.Core.Container;
using UnityEngine;
using Framework.Core.Singleton;
using UnityEditor;
using Framework.Core.Manager.Camera;
using Framework.Core.Manager.ResourcesLoad;
using Framework.Core.ResourcesAssets;

namespace Framework.Core.Manager.UI
{
    /// <summary>
    /// 窗口类型
    /// </summary>
    public enum UIType
    {
        /// <summary>
        /// 自由弹窗
        /// </summary>
        Freedom = 1,

        /// <summary>
        /// 固定弹窗
        /// </summary>
        Fixed = 2,

        /// <summary>
        /// 栈类型弹窗
        /// </summary>
        Stack = 3,
    }

    /// <summary>
    /// UI框架管理器
    /// </summary>
    [MonoSingletonPath("[Manager]/UIManager")]
    public class UIManager : MonoSingleton<UIManager>
    {
        /// <summary>
        /// 定义各个UI界面数据
        /// </summary>
        private readonly Dictionary< EUI, UIData> UIDataDict = new()
        {
            {
                EUI.ExampleUI,
                new UIData
                {
                    UIPrefabPath = "UI/Pages/Example/ExampleUI",
                    UIType = UIType.Stack
                }
            },
            {
                EUI.StartUI,
                new UIData
                {
                    UIPrefabPath = "UI/Pages/Start/StartUI",
                    UIType = UIType.Stack
                }
            },
            {
                EUI.PlotUI,
                new UIData
                {
                    UIPrefabPath = "UI/Pages/Plot/PlotUI",
                    UIType = UIType.Stack
                }
            },
            {
                EUI.LobbyMainUI,
                new UIData
                {
                    UIPrefabPath = "UI/Pages/Lobby/LobbyMainUI",
                    UIType = UIType.Stack
                }
            },
            {
                EUI.MainUI,
                new UIData
                {
                    UIPrefabPath = "UI/Pages/Main/MainUI",
                    UIType = UIType.Stack
                }
            }
        };

        private const string LOGTag = "UIManager";
        private readonly Stack<BaseUI> _uiStack = new Stack<BaseUI>();
        private readonly Dictionary<int, BaseUI> _baseUIDict = new Dictionary<int, BaseUI>();

        private static Transform UICameraRoot => CameraManager.Instance.UICamera.transform;
        private UnityEngine.Camera _uiCamera;
        private static UnityEngine.Camera UICamera => CameraManager.Instance.UICamera;

        public Transform CanvasRoot
        {
            get
            {
                if (_canvasRoot != null) return _canvasRoot;
                _canvasRoot = UICameraRoot.Find("UIRoot");
                return _canvasRoot;
            }
        }

        private Transform _canvasRoot;

        /// <summary>
        /// UI框架初始化
        /// </summary>
        public override void Initialize()
        {
            RegistUIBinding();
            InitUIData();
        }

        void RegistUIBinding()
        {
            UIBinding.Register();
        }

        private void InitUICamera()
        {
            DontDestroyOnLoad(UICameraRoot);
        }

        /// <summary>
        /// UI框架启动
        /// </summary>
        public void Launch()
        {
            InitUICamera();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        public void OpenUI( EUI id, dynamic options = null)
        {
            UIStackPush(id, options);
        }

        private BaseUI GetUIById( EUI id,bool IsAsync = false)
        {
            _baseUIDict.TryGetValue((int)id, out var ui);
            if (ui != null)
            {
                return ui;
            }
            
            var go = Instantiate(ResourcesLoadManager.LoadAsset<GameObject>($"{DEF.RESOURCES_ASSETS_PATH}/{UIDataDict[id].UIPrefabPath}.prefab"),CanvasRoot);
            ui = go.transform.GetComponent<BaseUI>();
            go.transform.name = UIDataDict[id].Name;
            ui.UIId = id;
            _baseUIDict.Add((int)id, ui);
            ui.OnInit();
            return ui;
        }

        private void UIStackPush( EUI uiId, dynamic options = null)
        {
            var ui = GetUIById(uiId);
            LogManager.Log("Open UI === ", ui.name);
            // ui.Canvas.sortingOrder = 0;//UIDataDict[uiId].Layer;
            // ui.Canvas.worldCamera = UICamera;
            if (ui.IsShow)
            {
                return;
            }

            //显示当前界面时,应该先去判断当前栈是否为空,不为空说明当前有界面显示,把当前的界面OnPause掉
            if (_uiStack.Count > 0)
            {
                _uiStack.Peek().OnPause();
            }

            //每次入栈(显示页面的时候),触发ui的OnEnter方法
            ui.OnEnter(options);
            ui.transform.gameObject.SetActive(true);
            _uiStack.Push(ui);
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="id"></param>
        public void Close( EUI id)
        {
            var ui = GetUIById(id);
            ui.OnExit();
            ui.transform.gameObject.SetActive(false);
            LogManager.Log("Close UI === ", ui.name);
        }

        /// <summary>
        /// 
        /// </summary>
        public void UIStackPop()
        {
            if (_uiStack.Count <= 0) return;
            _uiStack.Pop(); //关闭栈顶界面
            // Destroy(ui.gameObject);
            if (_uiStack.Count > 0)
            {
                _uiStack.Peek().OnResume(); //恢复原先的界面
            }
        }

        private void InitUIData()
        {
            foreach (var kvp in UIDataDict)
            {
                kvp.Value.ID = kvp.Key;
                kvp.Value.Name = kvp.Key.ToString();
            }

            LogManager.Log(LOGTag, "UI data is parsed");
        }
        /// <summary>
        /// UI数据类
        /// </summary>
        [Serializable]
        public class UIData
        {
            /// <summary>
            /// 界面名称
            /// </summary>
            public string Name;

            /// <summary>
            /// 界面ID
            /// </summary>
            public  EUI ID;

            /// <summary>
            /// 界面资源路径
            /// </summary>
            public string UIPrefabPath;

            /// <summary>
            /// 所属层级
            /// </summary>
            public int Layer;

            /// <summary>
            /// 界面类型
            /// </summary>
            public UIType UIType;
        }
    }
}