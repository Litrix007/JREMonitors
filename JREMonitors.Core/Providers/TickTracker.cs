namespace JREMonitors.Core.Providers
{
    public class TickTracker
    {
        private readonly IDelayProvider _delayProvider;

        public TickTracker(IDelayProvider delayProvider)
        {
            _delayProvider = delayProvider;
            IsWaitingForTick = false;
            TickCount = -1;
        }

        public long TickCount { get; private set; }

        public bool ShouldTrigger => _delayProvider.TickCount != TickCount;

        public bool IsWaitingForTick { get; private set; }

        public bool TrackAndSync()
        {
            var shouldTrigger = ShouldTrigger;
            Sync();
            return shouldTrigger;
        }

        public long Sync()
        {
            TickCount = _delayProvider.TickCount;
            return TickCount;
        }

        public void TryRequestNextTick()
        {
            if (IsWaitingForTick) return;
            Sync();
            IsWaitingForTick = true;
        }

        public bool ConsumeNextTick()
        {
            if (IsWaitingForTick && ShouldTrigger)
            {
                IsWaitingForTick = false;
                Sync();
                return true;
            }

            return false;
        }

        public void Reset()
        {
            TickCount = -1;
            IsWaitingForTick = false;
        }
    }
}