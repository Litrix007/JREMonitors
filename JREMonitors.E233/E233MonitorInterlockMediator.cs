using System;
using System.Collections.Generic;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.Providers;
using JREMonitors.E233.Constants;
using JREMonitors.JRE.Constants;

namespace JREMonitors.E233
{
    public class E233MonitorInterlockMediator : ITickUpdatable
    {
        private Monitor _m1;
        private E233MonitorStates _m1States;
        private Monitor _m2;
        private E233MonitorStates _m2States;
        private Monitor _m3;
        private E233MonitorStates _m3States;

        public E233MonitorInterlockMediator(Dictionary<string, Monitor> monitors)
        {
            monitors.TryGetValue(MonitorIds.Monitor1, out _m1);
            monitors.TryGetValue(MonitorIds.Monitor2, out _m2);
            monitors.TryGetValue(MonitorIds.Monitor3, out _m3);
            RebuildStateAssignments();
        }

        public void Update(TimeSpan elapsed)
        {
            if (_m1States != null && _m1States.ConsumeChangeToTIMSMain())
                MoveTIMSMain(_m1States, _m2States, _m3States);
            else if (_m2States != null && _m2States.ConsumeChangeToTIMSMain())
                MoveTIMSMain(_m2States, _m1States, _m3States);
            else if (_m3States != null && _m3States.ConsumeChangeToTIMSMain())
                MoveTIMSMain(_m3States, _m1States, _m2States);

            GetMonitorByType(E233MonitorType.TIMSMain, out var timsMainMonitor, out var timsMainMonitorStates);
            GetMonitorByType(E233MonitorType.Tid, out var tidMonitor, out var tidMonitorStates);
            if (timsMainMonitor != null)
            {
                var timsScr = timsMainMonitor.ActiveScreenId;
                if (timsScr == ScreenIds.S00AA)
                    timsMainMonitor.ChangeScreen(ScreenIds.S00AB);
                else if (timsScr == ScreenIds.Tid && tidMonitor != null && tidMonitor != timsMainMonitor)
                    timsMainMonitor.ChangeScreen(ScreenIds.S00AB);
            }

            if (timsMainMonitor != null)
            {
                if (_m1 != null && _m1 != timsMainMonitor)
                {
                    if (timsMainMonitor.ActiveScreenId == ScreenIds.X00AA)
                        _m1.ChangeScreen(ScreenIds.S00AA);
                    else if (_m1States != null) TryChangeToDefaultScreen(_m1, _m1States);
                }

                if (_m2 != null && _m2 != timsMainMonitor)
                {
                    if (timsMainMonitor.ActiveScreenId == ScreenIds.X00AA)
                        _m2.ChangeScreen(ScreenIds.S00AA);
                    else if (_m2States != null) TryChangeToDefaultScreen(_m2, _m2States);
                }

                if (_m3 != null && _m3 != timsMainMonitor)
                {
                    if (timsMainMonitor.ActiveScreenId == ScreenIds.X00AA)
                        _m3.ChangeScreen(ScreenIds.S00AA);
                    else if (_m3States != null) TryChangeToDefaultScreen(_m3, _m3States);
                }
            }
            else
            {
                if (_m1 != null && _m1States != null) TryChangeToDefaultScreen(_m1, _m1States);
                if (_m2 != null && _m2States != null) TryChangeToDefaultScreen(_m2, _m2States);
                if (_m3 != null && _m3States != null) TryChangeToDefaultScreen(_m3, _m3States);
            }

            if (timsMainMonitor != null)
            {
                if (tidMonitor == null)
                {
                    tidMonitor = timsMainMonitor;
                    tidMonitorStates = timsMainMonitorStates;
                    timsMainMonitorStates.HasOtherMonitorToShowSafetyLamps = false;
                }
                else
                {
                    timsMainMonitorStates.HasOtherMonitorToShowSafetyLamps = true;
                }
            }

            var isSafetyLampVisibleExternally = tidMonitor != null && (tidMonitor.ActiveScreenId == ScreenIds.Tid
                                                                       || tidMonitor.ActiveScreenId ==
                                                                       ScreenIds.TidChangeToTIMSWarning);
            if (tidMonitorStates != null)
            {
                tidMonitorStates.IsSafetyLampsVisibleExternally = false;
                if (_m1States != null && _m1States != tidMonitorStates)
                    _m1States.IsSafetyLampsVisibleExternally = isSafetyLampVisibleExternally;

                if (_m2States != null && _m2States != tidMonitorStates)
                    _m2States.IsSafetyLampsVisibleExternally = isSafetyLampVisibleExternally;

                if (_m3States != null && _m3States != tidMonitorStates)
                    _m3States.IsSafetyLampsVisibleExternally = isSafetyLampVisibleExternally;
            }
            else
            {
                if (_m1States != null)
                    _m1States.IsSafetyLampsVisibleExternally = false;

                if (_m2States != null)
                    _m2States.IsSafetyLampsVisibleExternally = false;

                if (_m3States != null)
                    _m3States.IsSafetyLampsVisibleExternally = false;
            }
        }

        private void RebuildStateAssignments()
        {
            _m1States = _m1?.LocalDataHub?.GetOrNull<E233MonitorStates>();
            _m2States = _m2?.LocalDataHub?.GetOrNull<E233MonitorStates>();
            _m3States = _m3?.LocalDataHub?.GetOrNull<E233MonitorStates>();
            if (_m1States != null)
                _m1States.MonitorType = E233MonitorType.Meter;
            var m2IsMain = _m2States != null &&
                           _m2States.MonitorType == E233MonitorType.TIMSMain;
            var m3IsMain = _m3States != null &&
                           _m3States.MonitorType == E233MonitorType.TIMSMain;

            if (m2IsMain && m3IsMain)
            {
                _m3States.MonitorType = E233MonitorType.Tid;
            }
            else if (m2IsMain)
            {
                if (_m3States != null) _m3States.MonitorType = E233MonitorType.Tid;
            }
            else if (m3IsMain)
            {
                if (_m2States != null) _m2States.MonitorType = E233MonitorType.Tid;
            }
            else
            {
                if (_m2States != null)
                {
                    _m2States.MonitorType = E233MonitorType.TIMSMain;
                    if (_m3States != null) _m3States.MonitorType = E233MonitorType.Tid;
                }
                else if (_m3States != null)
                {
                    _m3States.MonitorType = E233MonitorType.TIMSMain;
                }
            }
        }

        public void AddMonitor(Monitor monitor)
        {
            if (monitor == null) return;
            if (monitor.Id == MonitorIds.Monitor1) _m1 = monitor;
            else if (monitor.Id == MonitorIds.Monitor2) _m2 = monitor;
            else if (monitor.Id == MonitorIds.Monitor3) _m3 = monitor;
            RebuildStateAssignments();
        }

        public void RemoveMonitor(Monitor monitor)
        {
            if (monitor == null) return;
            E233MonitorStates removedController;
            if (monitor.Id == MonitorIds.Monitor1)
            {
                removedController = _m1States;
                _m1 = null;
                _m1States = null;
            }
            else if (monitor.Id == MonitorIds.Monitor2)
            {
                removedController = _m2States;
                _m2 = null;
                _m2States = null;
            }
            else if (monitor.Id == MonitorIds.Monitor3)
            {
                removedController = _m3States;
                _m3 = null;
                _m3States = null;
            }
            else
            {
                return;
            }

            var removedWasMain = removedController != null &&
                                 removedController.MonitorType == E233MonitorType.TIMSMain;
            RebuildStateAssignments();
            if (!removedWasMain) return;
            GetMonitorByType(E233MonitorType.TIMSMain, out var newMain, out _);
            if (newMain != null && newMain.ActiveScreenId == ScreenIds.S00AA)
                newMain.ChangeScreen(ScreenIds.X00AA);
        }

        private void GetMonitorByType(E233MonitorType type, out Monitor monitor,
            out E233MonitorStates monitorStates)
        {
            if (_m1States?.MonitorType == type)
            {
                monitor = _m1;
                monitorStates = _m1States;
                return;
            }

            if (_m2States?.MonitorType == type)
            {
                monitor = _m2;
                monitorStates = _m2States;
                return;
            }

            if (_m3States?.MonitorType == type)
            {
                monitor = _m3;
                monitorStates = _m3States;
                return;
            }

            monitor = null;
            monitorStates = null;
        }

        private void MoveTIMSMain(E233MonitorStates controllerChangingToMain,
            E233MonitorStates c1, E233MonitorStates c2)
        {
            var oldType = controllerChangingToMain.MonitorType;
            if (c1 != null && c1.MonitorType == E233MonitorType.TIMSMain)
                c1.MonitorType = oldType;
            else if (c2 != null && c2.MonitorType == E233MonitorType.TIMSMain) c2.MonitorType = oldType;
            controllerChangingToMain.MonitorType = E233MonitorType.TIMSMain;
        }

        private static void TryChangeToDefaultScreen(Monitor monitor, E233MonitorStates monitorStates)
        {
            var scr = monitor.ActiveScreenId;
            if (scr != ScreenIds.S00AA && scr != ScreenIds.X00AA) return;
            if (monitorStates.MonitorType == E233MonitorType.Meter) monitor.ChangeScreen(ScreenIds.Meter);
            else if (monitorStates.MonitorType == E233MonitorType.Tid) monitor.ChangeScreen(ScreenIds.Tid);
        }
    }
}