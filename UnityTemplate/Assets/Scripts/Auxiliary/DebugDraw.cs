using UnityEngine;

namespace kekchpek.Auxiliary
{
    public static class DebugDraw
    {

        public static void DrawSquare(Vector3 position, float size, Color color, float duration = 0f)
        {
            var halfSize = size * 0.08f;
            var topLeft = position + new Vector3(-halfSize, halfSize, 0);
            var topRight = position + new Vector3(halfSize, halfSize, 0);
            var bottomLeft = position + new Vector3(-halfSize, -halfSize, 0);
            var bottomRight = position + new Vector3(halfSize, -halfSize, 0);

            Debug.DrawLine(topLeft, topRight, color, duration);
            Debug.DrawLine(topRight, bottomRight, color, duration);
            Debug.DrawLine(bottomRight, bottomLeft, color, duration);
            Debug.DrawLine(bottomLeft, topLeft, color, duration);
            Debug.DrawLine(bottomLeft, topRight, color, duration);
            Debug.DrawLine(bottomRight, topLeft, color, duration);
        }
        
    }
}