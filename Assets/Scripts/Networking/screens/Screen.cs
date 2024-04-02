using System;
using UnityEngine;

namespace Networking.screens
{
    [Serializable]
    public struct Screen
    {
        public int id;
        public Transform transform;
    }
}