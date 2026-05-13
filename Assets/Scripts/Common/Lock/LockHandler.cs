using System;
using System.Collections.Generic;

namespace Common.Lock
{
    public class LockHandler
    {
        private readonly HashSet<object> _owners = new();

        public void Acquire(object owner, Action onAction)
        {
            if (_owners.Add(owner))
                onAction?.Invoke();
        }

        public void Release(object owner, Action onAction)
        {
            if (_owners.Remove(owner) && _owners.Count == 0)
                onAction?.Invoke();
        }
    }
}