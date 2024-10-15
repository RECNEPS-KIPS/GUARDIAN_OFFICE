﻿// author:KIPKIPS
// describe:可替换单例类,可被新创建单例替换
using UnityEngine;

namespace Framework.Core.Singleton {
    // 如果跳转到新的场景里已经有了实例，则删除已有示例，再创建新的实例
    public class ReplaceableMonoSingleton<T> : MonoBehaviour where T : Component {
        protected static T _instance;
        public float InitializationTime;

        // Singleton design pattern
        public static T Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null) {
                        GameObject obj = new GameObject();
                        obj.hideFlags = HideFlags.HideAndDontSave;
                        _instance = obj.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        // On awake, we check if there's already a copy of the object in the scene. If there's one, we destroy it.
        protected virtual void Awake() {
            if (!Application.isPlaying) {
                return;
            }
            InitializationTime = Time.time;
            DontDestroyOnLoad(this.gameObject);
            // we check for existing objects of the same type
            T[] check = FindObjectsOfType<T>();
            foreach (T searched in check) {
                if (searched != this) {
                    // if we find another object of the same type (not this), and if it's older than our current object, we destroy it.
                    if (searched.GetComponent<ReplaceableMonoSingleton<T>>().InitializationTime < InitializationTime) {
                        Destroy(searched.gameObject);
                    }
                }
            }
            if (_instance == null) {
                _instance = this as T;
            }
        }
    }
}