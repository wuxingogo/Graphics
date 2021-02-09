using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    public static class HDDynamicResolutionPlatformCapabilities
    {
        public enum Flag
        {
            PrepostUpscalerDetected
        }

        public static bool GetFlag(Flag flag)
        {
            return (s_FeatureFlags & (1 << (int)flag)) != 0;
        }

        #region private and internal state

        private static int s_FeatureFlags = 0;

        internal static void SetFeatureFlag(Flag flag, bool state)
        {
            int mask = (1 << (int)flag);
            if (state)
                s_FeatureFlags |= mask;
            else
                s_FeatureFlags &= ~mask;
        }

        #endregion
    }
}
