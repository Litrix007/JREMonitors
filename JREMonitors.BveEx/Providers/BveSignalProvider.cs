using System;
using System.Collections.Generic;
using BveEx.PluginHost;
using BveTypes.ClassWrappers;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using JREMonitors.JRE;
using JREMonitors.JRE.Providers;

namespace JREMonitors.BveEx.Providers
{
    public class BveSignalProvider<T> : ITickUpdatable, ISignalProvider<T>, ISignalController<T> where T : struct, Enum
    {
        private readonly IBveHacker _bveHacker;
        private readonly IDebugger _debugger;
        private readonly List<TIMSSignalSection> _timsSignalSections = new List<TIMSSignalSection>();

        public BveSignalProvider(DataHub hub)
        {
            _debugger = hub.GetOrNull<IDebugger>();
            _bveHacker = hub.Get<IBveHacker>();
        }

        public void SetActiveSignalSystem(T? signalSystem)
        {
            ActiveSignalSystem = signalSystem;
        }

        public T? ActiveSignalSystem { get; private set; }

        public IReadOnlyList<TIMSSignalSection> SignalSections => _timsSignalSections;

        public int CurrentSectionIndex { get; private set; } = -1;

        public int StopSectionIndex { get; private set; } = -1;

        public void Update(TimeSpan elapsed)
        {
            var sectionManager = _bveHacker.Scenario.SectionManager;
            var vehicleLocation = _bveHacker.Scenario.VehicleLocation.Location;
            var sections = sectionManager.Sections;
            if (_timsSignalSections.Count == 0 && sections.Count > 0)
            {
                _timsSignalSections.Capacity = sections.Count;
                for (var i = 0; i < sections.Count; i++)
                {
                    var srction = (Section)sections[i];
                    _timsSignalSections.Add(new TIMSSignalSection(srction.Location, srction.SectionCount.ToString()));
                }
            }

            CurrentSectionIndex = GetVehicleSectionIndex(sections, vehicleLocation,
                CurrentSectionIndex >= 0 ? CurrentSectionIndex : 0);
            var lastSection = sectionManager.LastSection;
            StopSectionIndex = lastSection != null ? lastSection.SectionCount : -1;
            _debugger?.AddLine($"CurrentSectionIndex: {CurrentSectionIndex}");
            _debugger?.AddLine($"StopSectionIndex: {StopSectionIndex}");
        }

        private static int GetVehicleSectionIndex(MapFunctionList sections, double vehicleLocation, int lastIndex)
        {
            if (sections.Count == 0) return 0;
            var index = lastIndex;
            if (index >= sections.Count) index = sections.Count - 1;
            if (index < 0) index = 0;
            while (index >= 0 && ((Section)sections[index]).Location > vehicleLocation) index--;

            while (index < sections.Count - 1 && ((Section)sections[index + 1]).Location <= vehicleLocation) index++;

            return Math.Max(0, index);
        }
    }
}