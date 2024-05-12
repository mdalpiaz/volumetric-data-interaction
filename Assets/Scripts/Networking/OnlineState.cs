#nullable enable

using UnityEngine;

namespace Networking
{
    public class OnlineState : MonoBehaviour
    {
        public static bool IsOnline => Instance.isOnline;

        private static OnlineState Instance { get; set; } = null!;

        [SerializeField]
        private bool isOnline;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this);
            }
        }
    }
}