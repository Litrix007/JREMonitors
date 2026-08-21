using System;

namespace JREMonitors.JRE.Providers
{
    public interface ISignalController<TSignal> where TSignal : struct, Enum
    {
        void SetActiveSignalSystem(TSignal? signalSystem);
    }
}