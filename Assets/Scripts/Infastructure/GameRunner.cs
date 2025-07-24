using UnityEngine;
using Zenject;

namespace Infastructure
{
    public class GameRunner : MonoBehaviour
    {
        GameBootstrapper.Factory gameBootstrapperFactory;

        [Inject]
        void Construct(GameBootstrapper.Factory bootstrapperFactory) => 
            gameBootstrapperFactory = bootstrapperFactory;

        private void Awake()
        {
            GameBootstrapper bootstrapper = FindObjectOfType<GameBootstrapper>();
      
            if(bootstrapper != null) return;

            gameBootstrapperFactory.Create();
        }
    }
}