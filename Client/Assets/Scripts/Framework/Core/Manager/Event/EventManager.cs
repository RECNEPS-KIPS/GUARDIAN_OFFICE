﻿// author:KIPKIPS
// describe:消息管理器
using Framework.Core.Singleton;
using System.Collections.Generic;
using System;
using Framework.Core.Pool;

namespace Framework.Core.Manager.Event {
    /// <summary>
    /// 事件管理器
    /// </summary>
    [MonoSingletonPath("[Manager]/EventManager")]
    public class EventManager : MonoSingleton<EventManager> {
        private const string LOGTag = "EventManager";
        private readonly BasalPool<EventEntity> _eventEntityPool = new BasalPool<EventEntity>();
        private readonly Dictionary<EventType, EventEntity> _eventDict = new Dictionary<EventType, EventEntity>();
        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void Register(EventType type, Action<dynamic> callback) {
            if (!_eventDict.ContainsKey(type)) {
                var e = _eventEntityPool.Allocate();
                _eventDict[type] = e;
                e.AddCallback(callback);
            } else {
                _eventDict[type].AddCallback(callback);
            }
        }
        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void Register(EventType type, Action callback) {
            if (!_eventDict.ContainsKey(type)) {
                var e = _eventEntityPool.Allocate();
                _eventDict[type] = e;
                e.AddCallback(callback);
            } else {
                _eventDict[type].AddCallback(callback);
            }
        }
        /// <summary>
        /// 解绑事件
        /// </summary>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void Remove(EventType type, Action<dynamic> callback)
        {
            if (!_eventDict.ContainsKey(type)) return;
            _eventDict[type].RemoveCallback(callback);
            if (_eventDict[type].CanRemove) {
                _eventEntityPool.Recycle(_eventDict[type]);
                // EventQueue.Remove(type);
            }
        }
        /// <summary>
        /// 解绑事件
        /// </summary>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void Remove(EventType type, Action callback)
        {
            if (!_eventDict.ContainsKey(type)) return;
            _eventDict[type].RemoveCallback(callback);
            if (_eventDict[type].CanRemove) {
                // EventQueue.Remove(type);
                _eventEntityPool.Recycle(_eventDict[type]);
            }
        }
        /// <summary>
        /// 派发事件
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void Dispatch(EventType type, dynamic data = null) {
            if (_eventDict != null && _eventDict.TryGetValue(type, out var value)) {
                value.Execute(data);
            }
        }
    }
}