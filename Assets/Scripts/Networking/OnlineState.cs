using System;
using UnityEngine;

namespace Networking
{
    public class OnlineState : MonoBehaviour
    {
        public static OnlineState Instance { get; private set; }
        
        [SerializeField]
        private bool isOnline;

        public bool IsOnline => isOnline;

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