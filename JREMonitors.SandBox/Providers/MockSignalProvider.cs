using System;
using System.Collections.Generic;
using JREMonitors.JRE;
using JREMonitors.JRE.Providers;

namespace JREMonitors.SandBox.Providers
{
    public class MockSignalProvider<T> : ISignalProvider<T>, ISignalController<T> where T : struct, Enum
    {
        public void SetActiveSignalSystem(T? signalSystem)
        {
            ActiveSignalSystem = signalSystem;
        }

        public T? ActiveSignalSystem { get; private set; }

        public IReadOnlyList<TIMSSignalSection> SignalSections { get; } = new[]
        {
            new TIMSSignalSection(300, "南1"),
            new TIMSSignalSection(400),
            new TIMSSignalSection(500),
            new TIMSSignalSection(600),
            new TIMSSignalSection(700),
            new TIMSSignalSection(800),
            new TIMSSignalSection(900),
            new TIMSSignalSection(1000),
            new TIMSSignalSection(1100),
            new TIMSSignalSection(1200),
            new TIMSSignalSection(1300)
        };

        public int CurrentSectionIndex => 0;
        public int StopSectionIndex => 10;
    }
}