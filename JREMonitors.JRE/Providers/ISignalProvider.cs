using System;
using System.Collections.Generic;

namespace JREMonitors.JRE.Providers
{
    public interface ISignalProvider<TSignal> where TSignal : struct, Enum
    {
        TSignal? ActiveSignalSystem { get; }
        IReadOnlyList<TIMSSignalSection> SignalSections { get; }
        int CurrentSectionIndex { get; }
        int StopSectionIndex { get; }
    }
}