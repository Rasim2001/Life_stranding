using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.Pool
{
    public class PoolObjects<TObject> : IInitializable, IPoolObjects<TObject> where TObject : MonoBehaviour
    {
        private readonly DiContainer _diContainer;

        private readonly TObject _prefab;
        private readonly Transform _parent;
        private readonly int _poolSize = 3;

        private List<TObject> _pool;


        public PoolObjects(TObject prefab, Transform parent, DiContainer diContainer)
        {
            _prefab = prefab;
            _parent = parent;
            _diContainer = diContainer;
        }


        public void Initialize()
        {
            _pool = new List<TObject>(_poolSize);

            for (int i = 0; i < _poolSize; i++)
            {
                TObject newObj = CreateObject();
                newObj.gameObject.SetActive(false);
            }
        }


        public TObject GetObjectFromPool()
        {
            foreach (TObject poolObj in _pool)
            {
                if (!poolObj.gameObject.activeInHierarchy)
                {
                    poolObj.gameObject.SetActive(true);
                    return poolObj;
                }
            }

            return CreateObject();
        }

        public void ReturnObjectToPool(TObject poolObj)
        {
            poolObj.gameObject.SetActive(false);
            poolObj.transform.SetParent(_parent);
        }


        private TObject CreateObject()
        {
            TObject newObj = _diContainer.InstantiatePrefabForComponent<TObject>(_prefab, _parent);
            _pool.Add(newObj);

            return newObj;
        }
    }
}