using System.Collections.Generic;
using BveTypes.ClassWrappers;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using SlimDX.Direct3D9;

namespace JREMonitors.BveEx.Monitors
{
    public static class BveMonitorManagerFactory
    {
        public static BveMonitorManagerBase Create(
            DataHub dataHub,
            MonitorContext context,
            IEnumerable<MonitorProperties> monitorProperties,
            ITimeProvider timeProvider,
            bool showTextureBoundsRect,
            int bufferFrameCount
        )
        {
            var device = Direct3DProvider.Instance.Device;
            if (device is DeviceEx)
                return new BveMonitorManagerD3D9Ex(dataHub, context, monitorProperties, timeProvider,
                    showTextureBoundsRect, bufferFrameCount);

            return new BveMonitorManagerD3D9(dataHub, context, monitorProperties, timeProvider, showTextureBoundsRect,
                bufferFrameCount);
        }
    }
}