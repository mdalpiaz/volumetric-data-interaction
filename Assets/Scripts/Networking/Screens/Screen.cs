using System;
using UnityEngine;

namespace Networking.Screens
{
    [Serializable]
    public struct Screen
    {
        public int id;
        public Transform transform;
    }
}