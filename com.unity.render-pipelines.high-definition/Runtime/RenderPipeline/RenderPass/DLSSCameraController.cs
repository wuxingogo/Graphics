using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Control camera component for Deep Learning Super Sampling.
    /// </summary>
    [HDRPHelpURLAttribute("DLSS-Camera-Controller")]
    [ExecuteAlways]
    [AddComponentMenu("Rendering/DLSS Camera Controller")]
    [RequireComponent(typeof(Camera))]
    public sealed class DLSSCameraController : MonoBehaviour
    {
        private Camera m_Camera = null;
        private HDAdditionalCameraData m_CamComponent = null;

        public bool Enable = true;
        public bool EnableOptimalSettings = true;

        private void Start()
        {
            m_Camera = GetComponentInParent<Camera>();
        }

        private void Update()
        {
            if (m_Camera == null)
                return;

            if (m_CamComponent == null && !m_Camera.TryGetComponent<HDAdditionalCameraData>(out m_CamComponent))
                return;

            m_CamComponent.allowDynamicResolution = Enable;
            m_CamComponent.allowDeepLearningSuperSamplingOptimalSettings = EnableOptimalSettings;
        }
    }
}
