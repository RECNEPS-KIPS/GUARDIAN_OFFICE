﻿// author:KIPKIPS
// describe:普通单例类
namespace Framework.Core.Singleton {
    /// <summary>
    /// 单例类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> : ISingleton where T : Singleton<T> {
        /// <summary>
        /// 静态实例
        /// </summary>
        protected static T _instance;

        // 标签锁：确保当一个线程位于代码的临界区时，另一个线程不进入临界区。
        // 如果其他线程试图进入锁定的代码，则它将一直等待（即被阻止），直到该对象被释放
        private static readonly object _lock = new ();
        
        /// <summary>
        /// 静态属性
        /// </summary>
        public static T Instance {
            get {
                lock (_lock) {
                    _instance ??= SingletonCreator.CreateSingleton<T>();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 资源释放
        /// </summary>
        public virtual void Dispose() {
            _instance = null;
        }
        
        /// <summary>
        /// 单例初始化方法
        /// </summary>
        public virtual void Initialize() {
        }
    }
}