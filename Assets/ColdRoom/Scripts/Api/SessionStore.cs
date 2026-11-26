using UnityEngine;

namespace ColdRoom.Api
{
    /// <summary>
    /// The session token survives page reloads. On WebGL PlayerPrefs is backed by IndexedDB.
    /// </summary>
    public static class SessionStore
    {
        private const string TokenKey = "coldroom.session.token";

        public static string Token
        {
            get => PlayerPrefs.GetString(TokenKey, string.Empty);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    PlayerPrefs.DeleteKey(TokenKey);
                }
                else
                {
                    PlayerPrefs.SetString(TokenKey, value);
                }

                PlayerPrefs.Save();
            }
        }

        public static bool HasToken => !string.IsNullOrWhiteSpace(Token);

        public static void Clear()
        {
            Token = null;
        }
    }
}
