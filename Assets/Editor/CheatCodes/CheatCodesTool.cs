using SpiderController;
using UnityEditor;
using UnityEngine;

namespace Editor.CheatCodes
{
    public class CheatCodesTool
    {
        [MenuItem("Cheats/Go To Biosphere")]
        public static void GoToBiosphere()
        {
            Spider spider = Object.FindObjectOfType<Spider>();

            spider.transform.position = new Vector3(4.32000017f, 111.580002f, -172.179993f);
            spider.transform.rotation = Quaternion.Euler(new Vector3(0f, 180, 0f));
        }
    }
}