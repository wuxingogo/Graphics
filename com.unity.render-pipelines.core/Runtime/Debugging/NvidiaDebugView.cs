using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if ENABLE_NVIDIA_MODULE
namespace Unity.External.NVIDIA
{
    public class DebugView
    {
        public static DebugView instance = new DebugView();

        private const uint InvalidId = 0xffffffff;
        private uint m_DebugViewId = InvalidId;

        public enum DeviceState
        {
            Unknown,
            MissingPluginDLL,
            DeviceCreationFailed,
            Active
        }

        public class Container<T> where T : struct
        {
            public T data = new T();
        }

        public class Data
        {
            public DeviceState deviceState = DeviceState.Unknown;
            public bool dlssSupported = false;
            public FeatureDebugInfos debugInfos;
            public Container<DLSSDebugFeatureInfos>[] dlssFeatureInfos = null;
        }

        public Data data = new Data();

        public void Reset()
        {
            m_DebugViewId = InvalidId;
        }

        public void Update()
        {
            Device device = NVIDIA.Device.GetDevice();
            bool panelIsOpen = DebugManager.instance.displayRuntimeUI || DebugManager.instance.displayEditorUI;
            if (device != null)
            {
                if (panelIsOpen && m_DebugViewId == InvalidId)
                {
                    m_DebugViewId = device.CreateDebugView();
                }
                else if (!panelIsOpen && m_DebugViewId != InvalidId)
                {
                    device.DeleteDebugView(m_DebugViewId);
                    m_DebugViewId = InvalidId;
                }
            }

            if (device != null)
            {
                if (m_DebugViewId != InvalidId)
                {
                    data.deviceState = DeviceState.Active;
                    data.dlssSupported = device.IsFeatureAvailable(NVIDIA.Feature.DLSS);
                    data.debugInfos = device.GetFeatureDebugInfos(m_DebugViewId);
                    data.dlssFeatureInfos = TranslateDlssFeatureArray(data.dlssFeatureInfos, data.debugInfos);
                }
                else
                {
                    data.deviceState = DeviceState.Unknown;
                }
            }
            else if (device == null)
            {
                bool isPluginLoaded = Plugins.IsPluginLoaded(Plugins.Plugin.NVUnityPlugin);
                data.deviceState = isPluginLoaded ?  DeviceState.DeviceCreationFailed : DeviceState.MissingPluginDLL;
                data.dlssSupported = false;
                data.dlssFeatureInfos = null;
            }

            UpdateDebugUITable();
        }

        private static Container<DLSSDebugFeatureInfos>[] TranslateDlssFeatureArray(Container<DLSSDebugFeatureInfos>[] oldArray, in FeatureDebugInfos rawDebugInfos)
        {
            if (rawDebugInfos.dlssInfosCount == 0)
                return null;

            Container<DLSSDebugFeatureInfos>[] targetArray = oldArray;
            if ((targetArray == null || targetArray.Length != rawDebugInfos.dlssInfosCount))
            {
                targetArray = new Container<DLSSDebugFeatureInfos>[rawDebugInfos.dlssInfosCount];
            }

            //copy data over
            unsafe
            {
                for (int i = 0; i < rawDebugInfos.dlssInfosCount; ++i)
                {
                    if (targetArray[i] == null)
                        targetArray[i] = new Container<DLSSDebugFeatureInfos>();
                    targetArray[i].data = rawDebugInfos.dlssInfos[i];
                }
            }

            return targetArray;
        }

        #region Debug User Interface

        public DebugUI.Container m_DebugWidget = null;
        public DebugUI.Table.Row[] m_DlssViewStateTableRows = null;
        public DebugUI.Container m_DlssViewStateTableHeader = null;
        public DebugUI.Table m_DlssViewStateTable = null;
        public DebugUI.Widget CreateWidget()
        {
            if (m_DebugWidget != null)
                return m_DebugWidget;

            m_DlssViewStateTableHeader = new DebugUI.Container()
            {
                children =
                {
                    new DebugUI.Container() {
                        displayName = "Status",
                    },
                    new DebugUI.Container() {
                        displayName = "Input resolution",
                    },
                    new DebugUI.Container() {
                        displayName = "Output resolution",
                    },
                    new DebugUI.Container() {
                        displayName = "Quality",
                    }
                }
            };

            m_DlssViewStateTable = new DebugUI.Table()
            {
                displayName = "DLSS Slot ID",
                isReadOnly = true
            };

            m_DlssViewStateTable.children.Add(m_DlssViewStateTableHeader);

            m_DebugWidget = new DebugUI.Container() {
                displayName = "NVIDIA device debug view",
                children =
                {
                    new DebugUI.Value()
                    {
                        displayName = "NVUnityPlugin Version",
                        getter = () => data.debugInfos.NVDeviceVersion.ToString("X2"),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "NGX API Version",
                        getter = () => data.debugInfos.NGXVersion.ToString("X2"),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "Device Status",
                        getter = () => data.deviceState.ToString(),
                    },
                    new DebugUI.Value()
                    {
                        displayName = "DLSS Supported",
                        getter = () => data.dlssSupported ? "True" : "False",
                    },
                    m_DlssViewStateTable
                }
            };

            return m_DebugWidget;
        }

        private void UpdateDebugUITable()
        {
            if (data.dlssFeatureInfos == null)
            {
                if (m_DlssViewStateTableRows != null)
                    foreach (var w in m_DlssViewStateTableRows)
                        m_DlssViewStateTable.children.Remove(w);
                m_DlssViewStateTableRows = null;
                return;
            }

            String resToString(uint a, uint b)
            {
                return "" + a + "x" + b;
            }

            if (m_DlssViewStateTableRows == null || m_DlssViewStateTableRows.Length != data.dlssFeatureInfos.Length)
            {
                if (m_DlssViewStateTableRows != null)
                    foreach (var w in m_DlssViewStateTableRows)
                        m_DlssViewStateTable.children.Remove(w);

                m_DlssViewStateTableRows = new DebugUI.Table.Row[data.dlssFeatureInfos.Length];
                for (int r = 0; r < data.dlssFeatureInfos.Length; ++r)
                {
                    var c = data.dlssFeatureInfos[r];
                    var dlssStateRow = new DebugUI.Table.Row()
                    {
                        children =
                        {
                            new DebugUI.Value()
                            {
                                getter = () => c.data.validFeature ? "Valid" : "Invalid"
                            },
                            new DebugUI.Value()
                            {
                                getter = () => resToString(c.data.initData.InputRTWidth, c.data.initData.InputRTHeight)
                            },
                            new DebugUI.Value()
                            {
                                getter = () => resToString(c.data.initData.OutputRTWidth, c.data.initData.OutputRTHeight)
                            },
                            new DebugUI.Value()
                            {
                                getter = () => c.data.initData.Quality.ToString()
                            }
                        }
                    };
                    m_DlssViewStateTableRows[r] = dlssStateRow;
                }
                m_DlssViewStateTable.children.Add(m_DlssViewStateTableRows);
            }

            for (int r = 0; r < m_DlssViewStateTableRows.Length; ++r)
            {
                m_DlssViewStateTableRows[r].displayName = Convert.ToString(data.dlssFeatureInfos[r].data.featureSlot);
            }
        }

        #endregion
    }
}
#endif
