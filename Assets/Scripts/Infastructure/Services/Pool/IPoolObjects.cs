using UnityEngine;

namespace Infastructure.Services.Pool
{
    public interface IPoolObjects<TObject> where TObject : MonoBehaviour
    {
        TObject GetObjectFromPool();
        void ReturnObjectToPool(TObject poolObj);
    }
}