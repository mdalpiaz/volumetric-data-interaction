#nullable enable

using UnityEngine;

namespace Networking.openIA
{
    public static class CoordinateConverter
    {
        public static Vector3 UnityToOpenIA(Model.Model model, Vector3 localPosition)
        {
            var modelUnitToLocal = Mathf.Max(model.Size.x, model.Size.y, model.Size.z);
            var newPosition = localPosition / modelUnitToLocal;
            Debug.DrawRay(newPosition, Vector3.forward, Color.red, 60);
            return SwapCoordinates(newPosition);
        }
        
        public static Vector3 OpenIAToUnity(Model.Model model, Vector3 position)
        {
            var modelUnitToLocal = Mathf.Max(model.Size.x, model.Size.y, model.Size.z);
            var convertedCoordinates = SwapCoordinates(position);
            var newPosition = convertedCoordinates * modelUnitToLocal;
            Debug.DrawRay(newPosition, Vector3.forward, Color.green, 60);
            return newPosition;
        }
        
        private static Vector3 SwapCoordinates(Vector3 vec)
        {
            return new Vector3(-vec.x, vec.z, vec.y);
        }
    }
}
