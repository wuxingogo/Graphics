using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public class DLSSPass
    {
        #region Render Graph Helper
        public static uint ExpectedDeviceVersion = 0x02;

        public struct ViewResourceHandles
        {
            public TextureHandle source;
            public TextureHandle output;
            public TextureHandle depth;
            public TextureHandle motionVectors;
            public void WriteResources(RenderGraphBuilder builder)
            {
                source = builder.WriteTexture(source);
                output = builder.WriteTexture(output);
                depth = builder.WriteTexture(depth);
                motionVectors = builder.WriteTexture(motionVectors);
            }
        }

        public struct CameraResourcesHandles
        {
            internal ViewResourceHandles resources;
            internal bool copyToViews;
            internal ViewResourceHandles tmpView0;
            internal ViewResourceHandles tmpView1;
        }

        public static ViewResources GetViewResources(in ViewResourceHandles handles)
        {
            var resources = new ViewResources
            {
                source = (RenderTexture)handles.source,
                output = (RenderTexture)handles.output,
                depth = (RenderTexture)handles.depth,
                motionVectors = (RenderTexture)handles.motionVectors
            };
            return resources;
        }

        public static CameraResourcesHandles CreateCameraResources(HDCamera camera, RenderGraph renderGraph, RenderGraphBuilder builder, in ViewResourceHandles resources)
        {
            var camResources = new CameraResourcesHandles();
            camResources.resources = resources;
            camResources.copyToViews = camera.xr.enabled && camera.xr.singlePassEnabled && camera.xr.viewCount > 1;

            if (camResources.copyToViews)
            {
                TextureHandle GetTmpViewXrTex(in TextureHandle handle)
                {
                    var newTexDesc = renderGraph.GetTextureDesc(handle);
                    newTexDesc.slices = 1;
                    newTexDesc.dimension = TextureDimension.Tex2D;
                    return renderGraph.CreateTexture(newTexDesc);
                }

                void CreateCopyNoXR(in ViewResourceHandles input, out ViewResourceHandles newResources)
                {
                    newResources.source = GetTmpViewXrTex(input.source);
                    newResources.output = GetTmpViewXrTex(input.output);
                    newResources.depth = GetTmpViewXrTex(input.depth);
                    newResources.motionVectors = GetTmpViewXrTex(input.motionVectors);
                    newResources.WriteResources(builder);
                }

                CreateCopyNoXR(resources, out camResources.tmpView0);
                CreateCopyNoXR(resources, out camResources.tmpView1);
            }

            return camResources;
        }

        public static CameraResources GetCameraResources(in CameraResourcesHandles handles)
        {
            var camResources = new CameraResources
            {
                resources = GetViewResources(handles.resources),
                copyToViews = handles.copyToViews
            };

            if (camResources.copyToViews)
            {
                camResources.tmpView0 = GetViewResources(handles.tmpView0);
                camResources.tmpView1 = GetViewResources(handles.tmpView1);
            }

            return camResources;
        }

        #endregion

        #region public members, general engine code
        public struct Parameters
        {
            public HDCamera hdCamera;
            public float sharpness;
        }

        public struct ViewResources
        {
            public RenderTexture source;
            public RenderTexture output;
            public RenderTexture depth;
            public RenderTexture motionVectors;
        }

        public struct CameraResources
        {
            internal ViewResources resources;
            internal bool copyToViews;
            internal ViewResources tmpView0;
            internal ViewResources tmpView1;
        }

        public static bool SetupFeature(bool resetDeviceIfCreated = true)
        {
#if ENABLE_NVIDIA_MODULE
            if (!Unity.External.NVIDIA.Plugins.IsPluginLoaded(Unity.External.NVIDIA.Plugins.Plugin.NVUnityPlugin))
                return false;

            if (ExpectedDeviceVersion != Unity.External.NVIDIA.Device.GetVersion())
            {
                Debug.LogWarning("Cannot instantiate NVIDIA device because the version HDRP expects does not match the backend version.");
                return false;
            }

            if (!SystemInfo.graphicsDeviceVendor.ToLower().Contains("nvidia"))
                return false;

            var device = Unity.External.NVIDIA.Device.CreateDevice();
            if (device == null)
                return false;

            if (resetDeviceIfCreated)
                Unity.External.NVIDIA.DebugView.instance.Reset();

            return device.IsFeatureAvailable(Unity.External.NVIDIA.Feature.DLSS);
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

            dlssPass = new DLSSPass(Unity.External.NVIDIA.Device.GetDevice());
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
            DLSSPass.CameraResources resources,
            CommandBuffer cmdBuffer)
        {
#if ENABLE_NVIDIA_MODULE
            InternalNVIDIARender(parameters, resources, cmdBuffer);
#endif
        }

        #endregion

        #region private members, nvidia specific code
#if ENABLE_NVIDIA_MODULE
        private Dictionary<CameraKey, DLSSPass.CameraState> m_CameraStates = new Dictionary<CameraKey, DLSSPass.CameraState>();
        private List<CameraKey> m_InvalidCameraKeys = new List<CameraKey>();

        private CommandBuffer m_CommandBuffer = new CommandBuffer();
        private UInt64 m_FrameId = 0;

        private Unity.External.NVIDIA.Device m_Device = null;

        private DLSSPass(Unity.External.NVIDIA.Device device)
        {
            m_Device = device;
        }

        //Amount of inactive frames dlss has rendered before we clean / destroy the plugin state.
        private static UInt64 sMaximumFrameExpiration = 400;

        private struct CameraKey
        {
            private int m_HashCode;
            public CameraKey(Camera camera)
            {
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
            public Unity.External.NVIDIA.NVSDK_NGX_PerfQuality_Value perfQuality;
            public DLSSPass.Resolution inputRes;
            public DLSSPass.Resolution outputRes;
            public float sharpness;
            public float jitterX;
            public float jitterY;
            public bool reset;
        }

        private class ViewState
        {
            private Unity.External.NVIDIA.DLSSCommand m_DlssCommand = null;
            private Unity.External.NVIDIA.Device m_Device;
            private DlssViewData m_Data = new DlssViewData();

            public ViewState(Unity.External.NVIDIA.Device device)
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

                    if (m_DlssCommand != null)
                    {
                        m_Device.DestroyFeature(cmdBuffer, m_DlssCommand);
                        m_DlssCommand = null;
                    }

                    var settings = new Unity.External.NVIDIA.InitDLSSCmdData();
                    settings.RTX_Value = 0;
                    settings.SetFlag(Unity.External.NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.IsHDR, true);
                    settings.SetFlag(Unity.External.NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.MVLowRes, true);
                    settings.SetFlag(Unity.External.NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.DepthInverted, true);
                    settings.SetFlag(Unity.External.NVIDIA.NVSDK_NGX_DLSS_Feature_Flags.DoSharpening, true);
                    settings.InputRTWidth = viewData.inputRes.width;
                    settings.InputRTHeight = viewData.inputRes.height;
                    settings.OutputRTWidth = viewData.outputRes.width;
                    settings.OutputRTHeight = viewData.outputRes.height;
                    settings.Quality = viewData.perfQuality;
                    m_DlssCommand = m_Device.CreateFeature(cmdBuffer, settings);
                }

                m_Data = viewData;
                m_Data.reset = isNew || viewData.reset;
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

                m_DlssCommand.ExecuteData.Sharpness = m_Data.sharpness;
                m_DlssCommand.ExecuteData.MVScaleX = -((float)m_Data.inputRes.width);
                m_DlssCommand.ExecuteData.MVScaleY = -((float)m_Data.inputRes.height);
                m_DlssCommand.ExecuteData.SubrectOffsetX = 0;
                m_DlssCommand.ExecuteData.SubrectOffsetY = 0;
                m_DlssCommand.ExecuteData.SubrectWidth = m_Data.inputRes.width;
                m_DlssCommand.ExecuteData.SubrectHeight = m_Data.inputRes.height;
                m_DlssCommand.ExecuteData.JitterOffsetX = m_Data.jitterX;
                m_DlssCommand.ExecuteData.JitterOffsetY = m_Data.jitterY;
                m_DlssCommand.ExecuteData.PreExposure = 1.0f;
                m_DlssCommand.ExecuteData.InvertYAxis = 1u;
                m_DlssCommand.ExecuteData.InvertXAxis = 0u;
                m_DlssCommand.ExecuteData.Reset = m_Data.reset ? 1 : 0;

                m_Device.SetTexture(cmdBuffer, m_DlssCommand, Unity.External.NVIDIA.ExecuteDLSSCmdData.Textures.ColorInput, source);
                m_Device.SetTexture(cmdBuffer, m_DlssCommand, Unity.External.NVIDIA.ExecuteDLSSCmdData.Textures.ColorOutput, output);
                m_Device.SetTexture(cmdBuffer, m_DlssCommand, Unity.External.NVIDIA.ExecuteDLSSCmdData.Textures.Depth, depth);
                m_Device.SetTexture(cmdBuffer, m_DlssCommand, Unity.External.NVIDIA.ExecuteDLSSCmdData.Textures.MotionVectors, motionVectors);
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
            WeakReference<Camera> m_CamReference = null;
            ViewState[] m_Views = null;
            Unity.External.NVIDIA.Device m_Device = null;

            public UInt64 LastFrameId { set; get; }

            public CameraState(Unity.External.NVIDIA.Device device, Camera camera)
            {
                m_CamReference = new WeakReference<Camera>(camera);
                m_Device = device;
            }

            public bool IsAlive()
            {
                return m_CamReference.TryGetTarget(out _);
            }

            public void SubmitCommands(
                HDCamera camera,
                in DlssViewData viewData,
                in CameraResources camResources,
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

                void RunPass(ViewState viewState, CommandBuffer cmdBuffer, in DlssViewData viewData, in ViewResources viewResources)
                {
                    viewState.UpdateViewState(viewData, cmdBuffer);
                    viewState.SubmitDlssCommands(
                        viewResources.source,
                        viewResources.depth,
                        viewResources.motionVectors,
                        viewResources.output, cmdBuffer);
                }

                if (camResources.copyToViews)
                {
                    Assertions.Assert.IsTrue(camera.xr.enabled && camera.xr.singlePassEnabled, "XR must be enabled for tmp copying to views to occur");

                    //copy to tmp views first, to maximize pipelining
                    for (int viewId = 0; viewId < m_Views.Length; ++viewId)
                    {
                        ViewState viewState = m_Views[viewId];
                        ViewResources tmpResources = viewId == 0 ? camResources.tmpView0 : camResources.tmpView1;

                        cmdBuffer.CopyTexture(camResources.resources.source, viewId, tmpResources.source, 0);
                        cmdBuffer.CopyTexture(camResources.resources.depth, viewId, tmpResources.depth, 0);
                        cmdBuffer.CopyTexture(camResources.resources.motionVectors, viewId, tmpResources.motionVectors, 0);
                    }

                    for (int viewId = 0; viewId < m_Views.Length; ++viewId)
                    {
                        ViewState viewState = m_Views[viewId];
                        ViewResources tmpResources = viewId == 0 ? camResources.tmpView0 : camResources.tmpView1;
                        RunPass(viewState, cmdBuffer, viewData, tmpResources);
                        cmdBuffer.CopyTexture(tmpResources.output, 0, camResources.resources.output, viewId);
                    }
                }
                else
                {
                    RunPass(m_Views[activeViewId], cmdBuffer, viewData, camResources.resources);
                }
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
                if (kv.Value.IsAlive() && !HasCameraStateExpired(kv.Value))
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
                cameraState = new DLSSPass.CameraState(m_Device, hdCamera.camera);
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

        public void InternalNVIDIARender(in DLSSPass.Parameters parameters, DLSSPass.CameraResources resources, CommandBuffer cmdBuffer)
        {
            if (m_Device == null || m_CameraStates.Count == 0)
                return;

            if (!m_CameraStates.TryGetValue(new CameraKey(parameters.hdCamera.camera), out var cameraState))
                return;

            var dlssViewData = new DlssViewData();
            dlssViewData.perfQuality = (Unity.External.NVIDIA.NVSDK_NGX_PerfQuality_Value)HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.DLSSPerfQualitySetting;
            dlssViewData.inputRes  = new Resolution() { width = (uint)parameters.hdCamera.actualWidth, height = (uint)parameters.hdCamera.actualHeight };
            dlssViewData.outputRes = new Resolution() { width = (uint)DynamicResolutionHandler.instance.finalViewport.x, height = (uint)DynamicResolutionHandler.instance.finalViewport.y };
            dlssViewData.sharpness  = parameters.sharpness;
            dlssViewData.jitterX = -parameters.hdCamera.taaJitter.x;
            dlssViewData.jitterY = -parameters.hdCamera.taaJitter.y;
            dlssViewData.reset = parameters.hdCamera.isFirstFrame;
            cameraState.SubmitCommands(parameters.hdCamera, dlssViewData, resources, cmdBuffer);
        }

#endif
        #endregion
    }
}
