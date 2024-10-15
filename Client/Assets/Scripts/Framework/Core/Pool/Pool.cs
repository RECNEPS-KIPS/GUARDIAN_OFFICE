﻿// author:KIPKIPS
// describe:对象池类
using System.Collections.Generic;

namespace Framework.Core.Pool {
    public abstract class Pool<T> : IPool<T> {
        // Gets the current count.
        public int CurCount {
            get {
                return _cacheStack.Count;
            }
        }
        protected IObjectFactory<T> _factory; //定义实现接口的类对象
        protected Stack<T> _cacheStack = new Stack<T>();

        // default is 5
        protected int MaxCount = 5;
        public virtual T Allocate() {
            return _cacheStack.Count == 0 ? _factory.Create() : _cacheStack.Pop();
        }
        public abstract bool Recycle(T obj);
    }
}