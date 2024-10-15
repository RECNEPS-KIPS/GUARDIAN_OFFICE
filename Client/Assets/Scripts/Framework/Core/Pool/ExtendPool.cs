﻿// author:KIPKIPS
// describe:单例对象池
using System;
using Framework.Core.Singleton;

namespace Framework.Core.Pool {
    //池对象容器
    public class ExtendPool<T> : Pool<T>, ISingleton where T : IPoolAble, new() {
        public void Initialize() {
        }
        protected ExtendPool() {
            _factory = new ObjectFactory<T>();
        }
        public static ExtendPool<T> Instance {
            get {
                return SingletonProperty<ExtendPool<T>>.Instance;
            }
        }
        public void Dispose() {
            SingletonProperty<ExtendPool<T>>.Dispose();
        }
        public void Init(int maxCount, int initCount) {
            if (maxCount > 0) {
                initCount = Math.Min(maxCount, initCount);
                MaxCount = maxCount;
            }
            if (CurCount < initCount) {
                for (int i = CurCount; i < initCount; ++i) {
                    Recycle(_factory.Create());
                }
            }
        }
        public int MaxCacheCount {
            get => MaxCount;
            set {
                MaxCount = value;
                if (_cacheStack != null && MaxCount > 0 && MaxCount < _cacheStack.Count) {
                    int removeCount = MaxCount - _cacheStack.Count;
                    while (removeCount > 0) {
                        _cacheStack.Pop();
                        --removeCount;
                    }
                }
            }
        }
        // 分配实例
        public override T Allocate() {
            T result = base.Allocate();
            result.IsRecycled = false;
            return result;
        }

        // 回收实例
        public override bool Recycle(T obj) {
            if (obj == null || obj.IsRecycled) {
                return false;
            }
            if (MaxCount > 0 && _cacheStack.Count >= MaxCount) {
                obj.OnRecycled();
                return false;
            }
            obj.IsRecycled = true;
            obj.OnRecycled();
            _cacheStack.Push(obj);
            return true;
        }
    }
}