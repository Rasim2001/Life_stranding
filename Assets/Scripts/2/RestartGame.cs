using UnityEngine;
using UnityEngine.SceneManagement;

namespace _2
{
    public class RestartGame : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                SceneManager.LoadScene(0);
        }
    }
}