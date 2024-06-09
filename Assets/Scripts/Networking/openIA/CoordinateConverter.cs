#nullable enable

using UnityEngine;

namespace Networking.openIA
{
    public static class CoordinateConverter
    {
        public static Vector3 UnityToOpenIAWorld(Model.Model model, Vector3 position)
        {
            var localPosition = model.transform.InverseTransformPoint(position);
            var modelUnitToLocal = Mathf.Max(model.Size.x, model.Size.y, model.Size.z);
            var diffPosition = model.BottomBackRightCorner - localPosition;
            var newPosition = diffPosition * modelUnitToLocal;
            return SwapCoordinates(newPosition);
        }

        public static Vector3 UnityToOpenIALocal(Model.Model model, Vector3 localPosition)
        {
            var modelUnitToLocal = Mathf.Max(model.Size.x, model.Size.y, model.Size.z);
            var newPosition = localPosition / modelUnitToLocal;
            return SwapCoordPos(newPosition);
        }

        public static Vector3 OpenIAToUnityWorld(Model.Model model, Vector3 position)
        {
            var localPosition = OpenIAToUnityLocal(model, position);
            return model.transform.TransformPoint(localPosition);
        }
        
        public static Vector3 OpenIAToUnityLocal(Model.Model model, Vector3 position)
        {
            var modelUnitToLocal = Mathf.Max(model.Size.x, model.Size.y, model.Size.z);
            var unityCoordinates = SwapCoordinates(position);
            var converted = unityCoordinates * modelUnitToLocal;
            return converted + model.BottomBackRightCorner;
        }

        public static Vector3 UnityToOpenIADirection(Vector3 direction) => SwapCoordinates(direction);

        public static Vector3 OpenIAToUnityDirection(Vector3 direction) => SwapCoordinates(direction);

        private static Vector3 SwapCoordinates(Vector3 vec) => new(-vec.x, vec.z, -vec.y);

        private static Vector3 SwapCoordPos(Vector3 vec) => new(-vec.x, vec.z, vec.y);
    }
}
