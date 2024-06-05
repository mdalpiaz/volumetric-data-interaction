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
            //Debug.DrawRay(newPosition, Vector3.forward, Color.red, 60);
            return SwapCoordinates(newPosition);
        }
        
        public static Vector3 OpenIAToUnity(Model.Model model, Vector3 position)
        {
            var scale = model.transform.localScale;
            var modelUnitToLocal = Mathf.Max(model.Size.x * scale.x, model.Size.y * scale.y, model.Size.z * scale.z);
            var convertedCoordinates = SwapCoordinates(position);
            var newPosition = convertedCoordinates * modelUnitToLocal;
            newPosition += model.transform.TransformPoint(model.BottomBackRightCorner);
            //Debug.DrawRay(newPosition, Vector3.forward, Color.green, 60);
            return newPosition;
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
