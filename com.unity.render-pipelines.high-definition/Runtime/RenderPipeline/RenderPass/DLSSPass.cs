using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering.HighDefinition
{
    public class DLSSPass
    {
        #region public members, general engine code
        public struct Parameters
        {
            public HDCamera hdCamera;
            public float sharpness;
        }

        public static bool SetupFeature(bool resetDeviceIfCreated = true)
        {
#if ENABLE_NVIDIA_MODULE
            if (!NVIDIA.Plugins.IsPluginLoaded(NVIDIA.Plugins.Plugin.NVUnityPlugin))
                return false;

            var device = NVIDIA.Device.CreateDevice();
            if (device == null)
                return false;

            if (resetDeviceIfCreated)
                NVIDIA.DebugView.instance.Reset();

            return device.IsFeatureAvailable(NVIDIA.Device.Feature.DLSS);
#else
            return false;
#endif
        }

        public static DLSSPass Create()
        {
            DLSSPass dlssPass = null;

#if ENABLE_NVIDIA_MODULE
            if (!SetupFeature(false))
                return null;

            dlssPass = new DLSSPass(NVIDIA.Device.GetDevice());
#endif
            return dlssPass;
        }

        public void BeginFrame(HDCamera hdCamera)
        {
#if ENABLE_NVIDIA_MODULE
            InternalNVIDIABeginFrame(hdCamera);
#endif
        }

        public void Render(
            DLSSPass.Parameters parameters,
            RenderTexture source,
            RenderTexture depth,
            RenderTexture motionVectors,
            RenderTexture output,
            CommandBuffer cmdBuffer)
        {
#if ENABLE_NVIDIA_MODULE
            InternalNVIDIARender(parameters, source, depth, motionVectors, output, cmdBuffer);
#endif
        }

        #endregion

        #region private members, nvidia specific code
#if ENABLE_NVIDIA_MODULE
        private Dictionary<CameraKey, DLSSPass.CameraState> m_CameraStates = new Dictionary<CameraKey, DLSSPass.CameraState>();
        private List<CameraKey> m_InvalidCameraKeys = new List<CameraKey>();

        private CommandBuffer m_CommandBuffer = new CommandBuffer();
        private UInt64 m_FrameId = 0;

        private NVIDIA.Device m_Device = null;

        private DLSSPass(NVIDIA.Device device)
        {
            m_Device = device;
        }

        //Amount of inactive frames dlss has rendered before we clean / destroy the plugin state.
        private static UInt64 sMaximumFrameExpiration = 400;

        private struct CameraKey
        {
            private WeakReference<Camera> m_WeakReference;
            private int m_HashCode;
            public CameraKey(Camera camera)
            {
                m_WeakReference = new WeakReference<Camera>(camera);
                m_HashCode = camera.GetInstanceID();
            }

            public override int GetHashCode()
            {
                return m_HashCode;
            }

            public override bool Equals(object obj)
            {
                if (obj.GetType() == typeof(CameraKey))
                    return GetHashCode() == ((CameraKey)obj).GetHashCode();

                return false;
            }

            public bool IsAlive()
            {
                return m_WeakReference.TryGetTarget(out _);
            }
        }

        private struct Resolution
        {
            public uint width;
            public uint height;

            public static bool operator==(Resolution a, Resolution b) =>
                a.width == b.width && a.height == b.height;

            public static bool operator!=(Resolution a, Resolution b) =>
                !(a == b);
            public override bool Equals(object obj)
            {
                if (obj is Resolution)
                    return (Resolution)obj == this;
                return false;
            }

            public override int GetHashCode()
            {
                return (int)(width ^ height);
            }
        }

        private struct DlssViewData
        {
            public NVIDIA.NVSDK_NGX_PerfQuality_Value perfQuality;
            public DLSSPass.Resolution inputRes;
            public DLSSPass.Resolution outputRes;
            public float sharpness;
            public float jitterX;
            public float jitterY;
        }

        private class ViewState
        {
            private NVIDIA.DLSSCommand m_DlssCommand = null;
            private NVIDIA.Device m_Device;
            private DlssViewData m_Data = new DlssViewData();

            public ViewState(NVIDIA.Device device)
            {
                m_Device = device;
                m_DlssCommand = null;
            }

            public void UpdateViewState(
                in DlssViewData viewData,
                CommandBuffer cmdBuffer)
            {
                bool isNew = false;
                if (viewData.outputRes != m_Data.outputRes || viewData.inputRes != m_Data.inputRes || viewData.perfQuality != m_Data.perfQuality || m_DlssCommand == null)
                {
                    isNew = true;
                    m_Data = viewData;

                    if (m_DlssCommand != null)
                    {
                        m_Device.DestroyFeature(cmdBuffer, m_DlssCommand);
                        m_DlssCommand = null;
                    }

                    var settings = new NVIDIA.InitDLSSCmdData();
                    settings.RTX_Value = 0;
                    settings.SetFlag(NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.IsHDR,         true);
                    settings.SetFlag(NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.MVLowRes,      true);
                    settings.SetFlag(NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.DepthInverted, true);
                    settings.SetFlag(NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.DoSharpening,  true);
                    settings.InputRTWidth   = m_Data.inputRes.width;
                    settings.InputRTHeight  = m_Data.inputRes.height;
                    settings.OutputRTWidth  = m_Data.outputRes.width;
                    settings.OutputRTHeight = m_Data.outputRes.height;
                    settings.Quality        = m_Data.perfQuality;
                    m_DlssCommand = m_Device.CreateFeature(cmdBuffer, settings);
                }

                if (m_DlssCommand != null)
                {
                    m_DlssCommand.ExecuteData.Sharpness = m_Data.sharpness;
                    m_DlssCommand.ExecuteData.MVScaleX = -((float)m_Data.inputRes.width);
                    m_DlssCommand.ExecuteData.MVScaleY = -((float)m_Data.inputRes.height);
                    m_DlssCommand.ExecuteData.SubrectOffsetX = 0;
                    m_DlssCommand.ExecuteData.SubrectOffsetY = 0;
                    m_DlssCommand.ExecuteData.SubrectWidth  = m_Data.inputRes.width;
                    m_DlssCommand.ExecuteData.SubrectHeight = m_Data.inputRes.height;
                    m_DlssCommand.ExecuteData.JitterOffsetX = m_Data.jitterX;
                    m_DlssCommand.ExecuteData.JitterOffsetY = m_Data.jitterY;
                    m_DlssCommand.ExecuteData.Reset = isNew ? 1 : 0;
                }
            }

            public void SubmitDlssCommands(
                RenderTexture source,
                RenderTexture depth,
                RenderTexture motionVectors,
                RenderTexture output,
                CommandBuffer cmdBuffer)
            {
                if (m_DlssCommand == null)
                    return;

                m_Device.SetTexture(cmdBuffer, m_DlssCommand, NVIDIA.ExecuteDLSSCmdData.Textures.ColorInput, source);
                m_Device.SetTexture(cmdBuffer, m_DlssCommand, NVIDIA.ExecuteDLSSCmdData.Textures.ColorOutput, output);
                m_Device.SetTexture(cmdBuffer, m_DlssCommand, NVIDIA.ExecuteDLSSCmdData.Textures.Depth, depth);
                m_Device.SetTexture(cmdBuffer, m_DlssCommand, NVIDIA.ExecuteDLSSCmdData.Textures.MotionVectors, motionVectors);
                m_Device.ExecuteCommand(cmdBuffer, m_DlssCommand);
            }

            public void Cleanup(CommandBuffer cmdBuffer)
            {
                if (m_DlssCommand != null)
                {
                    m_Device.DestroyFeature(cmdBuffer, m_DlssCommand);
                    m_DlssCommand = null;
                }
            }
        }

        private class CameraState
        {
            ViewState[] m_Views = null;
            NVIDIA.Device m_Device = null;

            public UInt64 LastFrameId { set; get; }

            public CameraState(NVIDIA.Device device)
            {
                m_Device = device;
            }

            public void SubmitCommands(
                HDCamera camera,
                in DlssViewData viewData,
                RenderTexture source,
                RenderTexture depth,
                RenderTexture motionVectors,
                RenderTexture output,
                CommandBuffer cmdBuffer)
            {
                int cameraViewCount = 1;
                int activeViewId = 0;
                if (camera.xr.enabled)
                {
                    cameraViewCount = camera.xr.singlePassEnabled ? camera.xr.viewCount : 2;
                    activeViewId = camera.xr.multipassId;
                }

                if (m_Views == null || m_Views.Length != cameraViewCount)
                {
                    if (m_Views != null)
                        Cleanup(cmdBuffer);

                    m_Views = new ViewState[cameraViewCount];
                    for (int viewId = 0; viewId < m_Views.Length; ++viewId)
                        m_Views[viewId] = new ViewState(m_Device);
                }

                m_Views[activeViewId].UpdateViewState(viewData, cmdBuffer);
                m_Views[activeViewId].SubmitDlssCommands(source, depth, motionVectors, output, cmdBuffer); 
            }

            public void Cleanup(CommandBuffer cmdBuffer)
            {
                foreach (var v in m_Views)
                    v.Cleanup(cmdBuffer);

                m_Views = null;
            }
        }

        private bool HasCameraStateExpired(CameraState cameraState)
        {
            return (m_FrameId - cameraState.LastFrameId) >= sMaximumFrameExpiration;
        }

        private void ProcessInvalidCameras()
        {
            foreach (KeyValuePair<CameraKey, CameraState> kv in m_CameraStates)
            {
                if (kv.Key.IsAlive() && !HasCameraStateExpired(kv.Value))
                    continue;

                m_InvalidCameraKeys.Add(kv.Key);
            }
        }

        private void CleanupCameraStates()
        {
            if (m_InvalidCameraKeys.Count == 0)
                return;

            m_CommandBuffer.Clear();
            foreach (var invalidKey in m_InvalidCameraKeys)
            {
                if (!m_CameraStates.TryGetValue(invalidKey, out var cameraState))
                    continue;

                cameraState.Cleanup(m_CommandBuffer);
                m_CameraStates.Remove(invalidKey);
            }
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            m_InvalidCameraKeys.Clear();
        }

        public void InternalNVIDIABeginFrame(HDCamera hdCamera)
        {
            if (m_Device == null)
                return;

            ProcessInvalidCameras();

            var cameraKey = new CameraKey(hdCamera.camera);
            CameraState cameraState = null;
            m_CameraStates.TryGetValue(cameraKey, out cameraState);
            bool dlssActive = hdCamera.IsDLSSEnabled();

            if (cameraState == null && dlssActive)
            {
                cameraState = new DLSSPass.CameraState(m_Device);
                m_CameraStates.Add(cameraKey, cameraState);
            }
            else if (cameraState != null && !dlssActive)
            {
                m_InvalidCameraKeys.Add(cameraKey);
            }

            if (cameraState != null)
                cameraState.LastFrameId = m_FrameId;

            CleanupCameraStates();
            ++m_FrameId;
        }

        public void InternalNVIDIARender(
            DLSSPass.Parameters parameters,
            RenderTexture source,
            RenderTexture depth,
            RenderTexture motionVectors,
            RenderTexture output,
            CommandBuffer cmdBuffer)
        {
            if (m_Device == null || m_CameraStates.Count == 0)
                return;

            if (!m_CameraStates.TryGetValue(new CameraKey(parameters.hdCamera.camera), out var cameraState))
                return;

            var dlssViewData = new DlssViewData();
            dlssViewData.perfQuality = (NVIDIA.NVSDK_NGX_PerfQuality_Value)HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.DLSSPerfQualitySetting;
            dlssViewData.inputRes  = new Resolution() { width = (uint)parameters.hdCamera.actualWidth, height = (uint)parameters.hdCamera.actualHeight };
            dlssViewData.outputRes = new Resolution() { width = (uint)DynamicResolutionHandler.instance.finalViewport.x, height = (uint)DynamicResolutionHandler.instance.finalViewport.y };
            dlssViewData.sharpness  = parameters.sharpness;
            dlssViewData.jitterX = -parameters.hdCamera.taaJitter.x;
            dlssViewData.jitterY = -parameters.hdCamera.taaJitter.y;

            cameraState.SubmitCommands(parameters.hdCamera, dlssViewData, source, depth, motionVectors, output, cmdBuffer);
        }

#endif
        #endregion
    }
}
