#nullable enable

using UnityEngine;

namespace Networking.openIA
{
    public static class CoordinateConverter
    {
        public static Vector3 UnityToOpenIA(Model.Model model, Vector3 localPosition)
        {
            var scale = model.transform.localScale;
            var modelUnitToLocal = Mathf.Max(model.Size.x * scale.x, model.Size.y * scale.y, model.Size.z * scale.z);
            var newPosition = localPosition / modelUnitToLocal;
            return SwapCoordinates(newPosition);
        }
        
        public static Vector3 OpenIAToUnity(Model.Model model, Vector3 position)
        {
            var scale = model.transform.localScale;
            var modelUnitToLocal = Mathf.Max(model.Size.x * scale.x, model.Size.y * scale.y, model.Size.z * scale.z);
            var convertedCoordinates = SwapCoordinates(position);
            var newPosition = convertedCoordinates * modelUnitToLocal;
            newPosition += model.transform.TransformPoint(model.BottomBackRightCorner);
            return newPosition;
        }

        public static Vector3 UnityToOpenIADirection(Model.Model model, Vector3 direction)
        {
            return SwapCoordinates(direction);
        }

        public static Vector3 OpenIAToUnityDirection(Model.Model model, Vector3 direction)
        {
            return SwapCoordinates(direction);
        }

        private static Vector3 SwapCoordinates(Vector3 vec)
        {
            return new Vector3(-vec.x, vec.z, -vec.y);
        }
    }
}
