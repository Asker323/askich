using UnityEngine;

namespace Askich
{
    /// <summary>Shared combat rules; independent of scene objects and frame rate.</summary>
    public static class ArenaRules
    {
        public const int FinalWave = 5;
        public const float HalfWidth = 12.65f;
        public const float HalfHeight = 6.05f;
        public static int WaveSize(int wave) => 12 + Mathf.Clamp(wave, 1, FinalWave) * 6;
        public static Vector2 Clamp(Vector2 p, float radius = 0.35f) => new Vector2(
            Mathf.Clamp(p.x, -HalfWidth + radius, HalfWidth - radius),
            Mathf.Clamp(p.y, -HalfHeight + radius, HalfHeight - radius));

        // Swept collision avoids tunnelling even when a projectile crosses its target in one frame.
        public static bool SegmentHits(Vector2 from, Vector2 to, Vector2 center, float radius)
        {
            Vector2 delta = to - from;
            float t = delta.sqrMagnitude < 0.000001f ? 0 : Mathf.Clamp01(Vector2.Dot(center - from, delta) / delta.sqrMagnitude);
            return (from + delta * t - center).sqrMagnitude <= radius * radius;
        }
    }
}
