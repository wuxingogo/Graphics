using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    public static class HDDynamicResolutionPlatformCapabilities
    {
        /// <summary>
        /// True if the render pipeline detected DLSS capable platform. False otherwise.
        /// </summary>
        public static bool DLSSDetected { get { return m_DLSSDetected; } }

        private static bool m_DLSSDetected = false;

        internal static void ActivateDLSS() { m_DLSSDetected = true; }
    }
}
