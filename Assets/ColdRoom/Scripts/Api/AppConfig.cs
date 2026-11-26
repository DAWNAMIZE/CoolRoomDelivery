using UnityEngine;

namespace ColdRoom.Api
{
    [CreateAssetMenu(fileName = "AppConfig", menuName = "ColdRoom/App Config")]
    public sealed class AppConfig : ScriptableObject
    {
        [Tooltip("Use the local JSON-backed mock API. Turn off to talk to apiBaseUrl.")]
        public bool useMockApi = true;

        [Tooltip("Base URL for the hosted API. Ignored while useMockApi is on.")]
        public string apiBaseUrl = "https://api.camaanstores.test";

        public static AppConfig Load()
        {
            var config = Resources.Load<AppConfig>("AppConfig");
            if (config != null)
            {
                return config;
            }

            Debug.LogWarning("[ColdRoom] Resources/AppConfig.asset is missing. Using in-memory defaults.");
            return CreateInstance<AppConfig>();
        }
    }
}
