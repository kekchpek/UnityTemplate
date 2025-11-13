using UnityEngine;

namespace AuxiliaryComponents.StaticUtils
{
    public static class AuxiliaryMath
    {
        public static bool Approximately(Vector3 a, Vector3 b, float epsilon)
        {
            return Approximately(a.x, b.x, epsilon) &&
                   Approximately(a.y, b.y, epsilon) &&
                   Approximately(a.z, b.z, epsilon);
        }

        public static bool Approximately(Vector3 a, Vector3 b) => Approximately(a, b, Mathf.Epsilon);
        
        public static bool Approximately(Vector2 a, Vector2 b, float epsilon)
        {
            return Approximately(a.x, b.x, epsilon) &&
                   Approximately(a.y, b.y, epsilon);
        }

        public static bool Approximately(Vector2 a, Vector2 b) => Approximately(a, b, Mathf.Epsilon);

        public static bool Approximately(float a, float b, float epsilon) => (double) Mathf.Abs(b - a) < (double) Mathf.Max(1E-06f * Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)), epsilon * 8f);
        
        public static bool Approximately(float a, float b) => Approximately(a, b, Mathf.Epsilon);
    }
}