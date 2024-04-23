using UnityEngine;

namespace Networking.Screens
{
    public class Screen : MonoBehaviour
    {
        [SerializeField]
        private int id;

        public int ID => id;
    }
}