﻿// author:KIPKIPS
// describe:mono单例类
using UnityEngine;

namespace Framework.Core.Singleton {

    /// <summary>
    /// 静态类：MonoBehaviour类的单例
    /// 泛型类：Where约束表示T类型必须继承MonoSingleton T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour, ISingleton where T : MonoSingleton<T> {
        // 静态实例
        private static T _instance;

        // 静态属性：封装相关实例对象
        /// <summary>
        /// 
        /// </summary>
        public static T Instance {
            get {
                if (_instance == null && !_onApplicationQuit) {
                    _instance = SingletonCreator.CreateMonoSingleton<T>();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 实现接口的单例初始化
        /// </summary>
        public virtual void Initialize() {
        }
        
        /// <summary>
        /// 资源释放
        /// </summary>
        public virtual void Dispose() {
            if (SingletonCreator.IsUnitTestMode) {
                var curTrans = transform;
                do {
                    var parent = curTrans.parent;
                    DestroyImmediate(curTrans.gameObject);
                    curTrans = parent;
                } while (curTrans != null);
                _instance = null;
            } else {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 当前应用程序是否结束 标签
        /// </summary>
        private static bool _onApplicationQuit;
        
        /// <summary>
        /// 应用程序退出 释放当前对象并销毁相关GameObject
        /// </summary>
        protected virtual void OnApplicationQuit() {
            _onApplicationQuit = true;
            if (_instance == null) return;
            Destroy(_instance.gameObject);
            _instance = null;
        }
        
        /// <summary>
        /// 释放当前对象
        /// </summary>
        protected virtual void OnDestroy() {
            _instance = null;
        }
        
        /// <summary>
        /// 判断当前应用程序是否退出
        /// </summary>
        public static bool IsApplicationQuit => _onApplicationQuit;
    }
}