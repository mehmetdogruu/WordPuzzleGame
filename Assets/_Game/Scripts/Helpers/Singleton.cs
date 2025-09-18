using UnityEngine;

namespace Helpers
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        public static bool InstanceExists => _instance != null;


        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        Debug.LogError("An instance of " + typeof(T)?.ToString() + " is needed in the scene, but there is none.");
                    }
                }

                return _instance;
            }
        }
    }
}
