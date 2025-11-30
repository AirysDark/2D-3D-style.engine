using Microsoft.Xna.Framework;
using System;

namespace GE2D3D.MapEditor.World
{
    public class Entity
    {
        private const float Tolerance = 0.001f;

        /// <summary>
        /// Converts an integer 0?3 into a Y-axis rotation.
        /// </summary>
        public static Vector3 GetRotationFromInteger(int rotation)
        {
            rotation = ((rotation % 4) + 4) % 4; // Normalize

            return rotation switch
            {
                0 => new Vector3(0, 0, 0),
                1 => new Vector3(0, MathHelper.PiOver2, 0),
                2 => new Vector3(0, MathHelper.Pi, 0),
                3 => new Vector3(0, MathHelper.Pi * 1.5f, 0),
                _ => Vector3.Zero
            };
        }

        /// <summary>
        /// Converts a Y-axis rotation into an integer 0?3.
        /// Uses tolerance to avoid float equality problems.
        /// </summary>
        public static int GetRotationFromVector(Vector3 vector)
        {
            float y = NormalizeAngle(vector.Y);

            if (Approximately(y, 0f)) return 0;
            if (Approximately(y, MathHelper.PiOver2)) return 1;
            if (Approximately(y, MathHelper.Pi)) return 2;
            if (Approximately(y, MathHelper.Pi * 1.5f)) return 3;

            return 0;
        }

        private static float NormalizeAngle(float angle)
        {
            float twoPi = MathHelper.TwoPi;
            angle = angle % twoPi;
            if (angle < 0) angle += twoPi;
            return angle;
        }

        private static bool Approximately(float a, float b)
        {
            return Math.Abs(a - b) < Tolerance;
        }
    }
}