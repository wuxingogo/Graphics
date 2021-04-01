using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    public static class HDDynamicResolutionPlatformCapabilities
    {
        /// <summary>
        /// True if the render pipeline detected DLSS capable platform. False otherwise.
        /// </summary>
        public static bool DLSSDetected { get { return m_DLSSDetected; } }

        /// <summary>
        /// User only variable, to control wether DLSS is on or off programmatically / from user code..
        /// </summary>
        public static bool DLSSEnabled { set { m_DLSSEnabled = value; } get { return m_DLSSEnabled; } }

        private static bool m_DLSSDetected = false;
        private static bool m_DLSSEnabled = true;

        internal static void ActivateDLSS() { m_DLSSDetected = true; m_DLSSEnabled = true; }
    }
}
