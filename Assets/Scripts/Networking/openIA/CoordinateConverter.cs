#nullable enable

using UnityEngine;

namespace Networking.openIA
{
    public static class CoordinateConverter
    {
        public static Vector3 UnityToOpenIAWorld(Model.Model model, Vector3 position)
        {
            var modelUnitToLocal = Mathf.Max(model.Size.x, model.Size.y, model.Size.z);
            var localPosition = model.transform.InverseTransformPoint(position);
            var diffPosition = model.BottomBackRightCorner - localPosition;
            var newPosition = diffPosition * modelUnitToLocal;
            return UnityToOpenIACoordinates(newPosition);
        }

        public static Vector3 OpenIAToUnityWorld(Model.Model model, Vector3 position)
        {
            var modelUnitToLocal = Mathf.Max(model.Size.x, model.Size.y, model.Size.z);
            var unityCoordinates = OpenIAToUnityCoordinates(position);
            var converted = unityCoordinates * modelUnitToLocal;
            var localPosition = converted + model.BottomBackRightCorner;
            return model.transform.TransformPoint(localPosition);
        }

        public static Vector3 UnityToOpenIADirection(Vector3 direction) => UnityToOpenIACoordinates(direction);

        public static Vector3 OpenIAToUnityDirection(Vector3 direction) => OpenIAToUnityCoordinates(direction);

        private static Vector3 OpenIAToUnityCoordinates(Vector3 vec) => new(-vec.x, vec.z, -vec.y);

        private static Vector3 UnityToOpenIACoordinates(Vector3 vec) => new(-vec.x, -vec.z, vec.y);
    }
}
