using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VFX;
using System.Reflection;
#endif
using NUnit.Framework;
using Object = UnityEngine.Object;
using UnityEngine.VFX.Utility;

namespace UnityEngine.VFX.Test
{
    public class VFXGraphicsTests
    {
        int m_previousCaptureFrameRate;
        float m_previousFixedTimeStep;
        float m_previousMaxDeltaTime;
        [OneTimeSetUp]
        public void Init()
        {
            m_previousCaptureFrameRate = Time.captureFramerate;
            m_previousFixedTimeStep = UnityEngine.VFX.VFXManager.fixedTimeStep;
            m_previousMaxDeltaTime = UnityEngine.VFX.VFXManager.maxDeltaTime;
        }

        [UnityTest, Category("VisualEffect")]
        [PrebuildSetup("SetupGraphicsTestCases")]
        [UseGraphicsTestCases]
        public IEnumerator Run(GraphicsTestCase testCase)
        {
#if UNITY_EDITOR
            while (SceneView.sceneViews.Count > 0)
            {
                var sceneView = SceneView.sceneViews[0] as SceneView;
                sceneView.Close();
            }
#endif
            SceneManagement.SceneManager.LoadScene(testCase.ScenePath);

            // Always wait one frame for scene load
            yield return null;

            var testSettingsInScene = Object.FindObjectOfType<GraphicsTestSettings>();
            var vfxTestSettingsInScene = Object.FindObjectOfType<VFXGraphicsTestSettings>();

            //Setup frame rate capture
            float simulateTime = VFXGraphicsTestSettings.defaultSimulateTime;
            int captureFrameRate = VFXGraphicsTestSettings.defaultCaptureFrameRate;

            if (vfxTestSettingsInScene != null)
            {
                simulateTime = vfxTestSettingsInScene.simulateTime;
                captureFrameRate = vfxTestSettingsInScene.captureFrameRate;
            }
            float period = 1.0f / captureFrameRate;

            Time.captureFramerate = captureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = period;
            UnityEngine.VFX.VFXManager.maxDeltaTime = period;

            //Waiting for the capture frame rate to be effective
            const int maxFrameWaiting = 8;
            int maxFrame = maxFrameWaiting;
            while (Time.deltaTime != period && maxFrame-- > 0)
                yield return null;
            Assert.Greater(maxFrame, 0);

            int captureSizeWidth = 512;
            int captureSizeHeight = 512;
            if (testSettingsInScene != null)
            {
                captureSizeWidth = testSettingsInScene.ImageComparisonSettings.TargetWidth;
                captureSizeHeight = testSettingsInScene.ImageComparisonSettings.TargetHeight;
            }

            var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            if (camera)
            {
                var vfxComponents = Resources.FindObjectsOfTypeAll<VisualEffect>();
#if UNITY_EDITOR
                var vfxAssets = vfxComponents.Select(o => o.visualEffectAsset).Where(o => o != null).Distinct();
                foreach (var vfx in vfxAssets)
                {
                    //Use Reflection as workaround of the access issue in .net 4 (TODO : Clean this as soon as possible)
                    //var graph = vfx.GetResource().GetOrCreateGraph(); is possible with .net 3.5 but compilation fail with 4.0
                    var visualEffectAssetExt = AppDomain.CurrentDomain.GetAssemblies()  .Select(o => o.GetType("UnityEditor.VFX.VisualEffectAssetExtensions"))
                                                                                        .Where(o => o != null)
                                                                                        .FirstOrDefault();
                    var fnGetResource = visualEffectAssetExt.GetMethod("GetResource");
                    fnGetResource = fnGetResource.MakeGenericMethod(new Type[]{ typeof(VisualEffectAsset)});
                    var resource = fnGetResource.Invoke(null, new object[] { vfx });
                    var fnGetOrCreate = visualEffectAssetExt.GetMethod("GetOrCreateGraph");
                    var graph = fnGetOrCreate.Invoke(null, new object[] { resource }) as VFXGraph;
                    graph.RecompileIfNeeded();
                }
#endif

                var rt = RenderTexture.GetTemporary(captureSizeWidth, captureSizeHeight, 24);
                camera.targetTexture = rt;

                //Waiting for the rendering to be ready, if at least one component has been culled, camera is ready
                maxFrame = maxFrameWaiting;
                while (vfxComponents.All(o => o.culled) && maxFrame-- > 0)
                    yield return null;
                Assert.Greater(maxFrame, 0);

                foreach (var component in vfxComponents)
                    component.Reinit();

#if UNITY_EDITOR
                //When we change the graph, if animator was already enable, we should reinitialize animator to force all BindValues
                var animators = Resources.FindObjectsOfTypeAll<Animator>();
                foreach (var animator in animators)
                {
                    animator.Rebind();
                }
                var audioSources = Resources.FindObjectsOfTypeAll<AudioSource>();
#endif
                var paramBinders = Resources.FindObjectsOfTypeAll<VFXPropertyBinder>();
                foreach (var paramBinder in paramBinders)
                {
                    var binders = paramBinder.GetPropertyBinders<VFXBinderBase>();
                    foreach (var binder in binders)
                    {
                        binder.Reset();
                    }
                }

                int waitFrameCount = (int)(simulateTime / period);
                int startFrameIndex = Time.frameCount;
                int expectedFrameIndex = startFrameIndex + waitFrameCount;

                while (Time.frameCount != expectedFrameIndex)
                {
                    yield return null;
#if UNITY_EDITOR
                    foreach (var audioSource in audioSources)
                        if (audioSource.clip != null && audioSource.playOnAwake)
                            audioSource.PlayDelayed(Mathf.Repeat(simulateTime, audioSource.clip.length));
#endif
                }

                Texture2D actual = null;
                try
                {
                    camera.targetTexture = null;
                    actual = new Texture2D(captureSizeWidth, captureSizeHeight, TextureFormat.RGB24, false);
                    RenderTexture.active = rt;
                    actual.ReadPixels(new Rect(0, 0, captureSizeWidth, captureSizeHeight), 0, 0);
                    RenderTexture.active = null;
                    actual.Apply();

                    var imageComparisonSettings = new ImageComparisonSettings() { AverageCorrectnessThreshold = VFXGraphicsTestSettings.defaultAverageCorrectnessThreshold };
                    if (testSettingsInScene != null)
                    {
                        imageComparisonSettings.AverageCorrectnessThreshold = testSettingsInScene.ImageComparisonSettings.AverageCorrectnessThreshold;
                    }

                    ImageAssert.AreEqual(testCase.ReferenceImage, actual, imageComparisonSettings);

                }
                finally
                {
                    RenderTexture.ReleaseTemporary(rt);
                    if (actual != null)
                        UnityEngine.Object.Destroy(actual);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
#if UNITY_EDITOR
            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Time.captureFramerate = m_previousCaptureFrameRate;
            UnityEngine.VFX.VFXManager.fixedTimeStep = m_previousFixedTimeStep;
            UnityEngine.VFX.VFXManager.maxDeltaTime = m_previousMaxDeltaTime;
        }
    }
}
