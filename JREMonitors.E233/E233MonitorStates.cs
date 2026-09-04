namespace JREMonitors.E233
{
    public enum E233MonitorType
    {
        Meter,
        TIMSMain,
        Tid
    }

    public class E233MonitorStates
    {
        private bool _shouldChangeToTIMSMain;
        private bool _userOverrideHide = true;
        public bool SupportsTasc { get; set; }
        public bool HasOtherMonitorToShowSafetyLamps { get; set; }
        public bool IsSafetyLampsVisibleExternally { get; set; }
        public E233MonitorType MonitorType { get; set; }

        public bool IsSafetyLampVisibleOnMeterScreen => !IsSafetyLampsVisibleExternally || !_userOverrideHide;

        public void ToggleLocalSafetyLampVisibility()
        {
            if (!IsSafetyLampsVisibleExternally) return;
            _userOverrideHide = !_userOverrideHide;
        }

        public void RequestChangeToTIMSMain()
        {
            _shouldChangeToTIMSMain = true;
        }

        public bool ConsumeChangeToTIMSMain()
        {
            var shouldChangeToTIMSMain = _shouldChangeToTIMSMain;
            _shouldChangeToTIMSMain = false;
            return shouldChangeToTIMSMain;
        }
    }
}