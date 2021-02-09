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

        public static bool SetupFeature()
        {
#if ENABLE_NVIDIA_MODULE
            if (!NVIDIA.Plugins.IsPluginLoaded(NVIDIA.Plugins.Plugin.NVUnityPlugin))
                return false;

            var device = NVIDIA.Device.CreateDevice();
            if (device == null)
                return false;

            return device.IsFeatureAvailable(NVIDIA.Device.Feature.DLSS);
#else
            return false;
#endif
        }

        public static DLSSPass Create()
        {
            DLSSPass dlssPass = null;

#if ENABLE_NVIDIA_MODULE
            if (!SetupFeature())
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
        private Dictionary<CameraKey, DLSSPass.ViewState> m_ViewStates = new Dictionary<CameraKey, DLSSPass.ViewState>();
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
            public uint Width;
            public uint Height;

            public static bool operator==(Resolution a, Resolution b) =>
                a.Width == b.Width && a.Height == b.Height;

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
                return (int)(Width ^ Height);
            }
        }

        private class ViewState
        {
            private NVIDIA.DLSSCommand m_DlssCommand = null;
            private NVIDIA.Device m_Device;
            private NVIDIA.NVSDK_NGX_PerfQuality_Value m_PerfQuality = NVIDIA.NVSDK_NGX_PerfQuality_Value.Balanced;
            private DLSSPass.Resolution m_OutputRes = new DLSSPass.Resolution();
            private DLSSPass.Resolution m_InputRes = new DLSSPass.Resolution();
            public UInt64 LastFrameId { set; get; }

            public ViewState(NVIDIA.Device device)
            {
                m_Device = device;
                m_DlssCommand = null;
            }

            public void UpdateViewState(
                HDCamera camera,
                NVIDIA.NVSDK_NGX_PerfQuality_Value perfQuality,
                float sharpness,
                DLSSPass.Resolution inputRes,
                DLSSPass.Resolution outputRes,
                CommandBuffer cmdBuffer)
            {
                bool isNew = false;
                if (m_OutputRes != outputRes || m_InputRes != inputRes || perfQuality != m_PerfQuality || m_DlssCommand == null)
                {
                    isNew = true;
                    m_OutputRes = outputRes;
                    m_InputRes  = inputRes;
                    m_PerfQuality = perfQuality;

                    if (m_DlssCommand != null)
                    {
                        m_Device.DestroyFeature(cmdBuffer, m_DlssCommand);
                        m_DlssCommand = null;
                    }

                    var settings = new NVIDIA.InitDLSSCmdData();
                    settings.RTX_Value = 0;
                    settings.Flags = (uint)
                        (NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.IsHDR
                            | NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.MVLowRes
                            | NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.DepthInverted
                            | NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.DoSharpening);
                    settings.InputRTWidth = inputRes.Width;
                    settings.InputRTHeight = inputRes.Height;
                    settings.OutputRTWidth = m_OutputRes.Width;
                    settings.OutputRTHeight = m_OutputRes.Height;
                    settings.Quality = m_PerfQuality;
                    m_DlssCommand = m_Device.CreateFeature(cmdBuffer, settings);
                }

                if (m_DlssCommand != null)
                {
                    m_DlssCommand.ExecuteData.Sharpness = sharpness;
                    m_DlssCommand.ExecuteData.MVScaleX = -((float)inputRes.Width);
                    m_DlssCommand.ExecuteData.MVScaleY = -((float)inputRes.Height);
                    m_DlssCommand.ExecuteData.SubrectOffsetX = 0;
                    m_DlssCommand.ExecuteData.SubrectOffsetY = 0;
                    m_DlssCommand.ExecuteData.SubrectWidth = inputRes.Width;
                    m_DlssCommand.ExecuteData.SubrectHeight = inputRes.Height;
                    m_DlssCommand.ExecuteData.JitterOffsetX = -camera.taaJitter.x;
                    m_DlssCommand.ExecuteData.JitterOffsetY = -camera.taaJitter.y;
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

                m_DlssCommand.ExecuteData.ColorInput = source.GetNativeTexturePtr();
                m_DlssCommand.ExecuteData.Depth = depth.GetNativeDepthBufferPtr();
                m_DlssCommand.ExecuteData.ColorOutput = output.GetNativeTexturePtr();
                m_DlssCommand.ExecuteData.MotionVectors = motionVectors.GetNativeTexturePtr();

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

        private bool HasViewStateExpired(ViewState viewState)
        {
            return (m_FrameId - viewState.LastFrameId) >= sMaximumFrameExpiration;
        }

        private void ProcessInvalidCameras()
        {
            foreach (KeyValuePair<CameraKey, ViewState> kv in m_ViewStates)
            {
                if (kv.Key.IsAlive() && !HasViewStateExpired(kv.Value))
                    continue;

                m_InvalidCameraKeys.Add(kv.Key);
            }
        }

        private void CleanupViewStates()
        {
            if (m_InvalidCameraKeys.Count == 0)
                return;

            m_CommandBuffer.Clear();
            foreach (var invalidKey in m_InvalidCameraKeys)
            {
                if (!m_ViewStates.TryGetValue(invalidKey, out var viewState))
                    continue;

                viewState.Cleanup(m_CommandBuffer);
                m_ViewStates.Remove(invalidKey);
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
            ViewState viewState = null;
            m_ViewStates.TryGetValue(cameraKey, out viewState);
            bool dlssActive = hdCamera.IsDLSSEnabled();

            if (viewState == null && dlssActive)
            {
                viewState = new DLSSPass.ViewState(m_Device);
                m_ViewStates.Add(cameraKey, viewState);
            }
            else if (viewState != null && !dlssActive)
            {
                m_InvalidCameraKeys.Add(cameraKey);
            }

            if (viewState != null)
                viewState.LastFrameId = m_FrameId;

            CleanupViewStates();
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
            if (m_Device == null || m_ViewStates.Count == 0)
                return;

            if (!m_ViewStates.TryGetValue(new CameraKey(parameters.hdCamera.camera), out var viewState))
                return;

            var perfQuality = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.DLSSPerfQualitySetting;

            viewState.UpdateViewState(
                parameters.hdCamera,
                (NVIDIA.NVSDK_NGX_PerfQuality_Value)perfQuality,
                parameters.sharpness,
                new DLSSPass.Resolution() // input res
                {
                    Width  = (uint)parameters.hdCamera.actualWidth,
                    Height = (uint)parameters.hdCamera.actualHeight
                },
                new DLSSPass.Resolution() // output res
                {
                    Width =  (uint)DynamicResolutionHandler.instance.finalViewport.x,
                    Height = (uint)DynamicResolutionHandler.instance.finalViewport.y
                },
                cmdBuffer
            );

            viewState.SubmitDlssCommands(
                source, depth, motionVectors, output, cmdBuffer);
        }

#endif
        #endregion
    }
}
