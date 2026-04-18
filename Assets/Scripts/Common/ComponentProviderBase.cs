using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public abstract class ComponentProviderBase : MonoBehaviour
    {
        private readonly Dictionary<Type, object> _map = new Dictionary<Type, object>();

        public void Initialize() =>
            RegisterMap(_map);

        protected abstract void RegisterMap(Dictionary<Type, object> map);

        public void Add<T>(T value) where T : class =>
            _map[typeof(T)] = value;

        public T Get<T>() where T : class
        {
            if (_map.TryGetValue(typeof(T), out var component))
                return component as T;

            return null;
        }

        public bool TryGet<T>(out T component) where T : class
        {
            if (_map.TryGetValue(typeof(T), out var value))
            {
                component = value as T;
                return component != null;
            }

            component = null;
            return false;
        }
    }
}