using UnityEngine;

namespace Networking.openIA
{
    public static class CoordinateConverter
    {
        public static Vector3 UnityToOpenIA(Vector3 vec)
        {
            return new Vector3(-vec.x, vec.z, vec.y);
        }

        public static Vector3 OpenIAToUnity(Vector3 vec)
        {
            return new Vector3(-vec.x, vec.z, vec.y);
        }
    }
}
