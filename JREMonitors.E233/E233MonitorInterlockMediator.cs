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
        private E233MonitorStateController _m1StateController;
        private Monitor _m2;
        private E233MonitorStateController _m2StateController;
        private Monitor _m3;
        private E233MonitorStateController _m3StateController;

        public E233MonitorInterlockMediator(Dictionary<string, Monitor> monitors)
        {
            monitors.TryGetValue(MonitorIds.Monitor1, out _m1);
            monitors.TryGetValue(MonitorIds.Monitor2, out _m2);
            monitors.TryGetValue(MonitorIds.Monitor3, out _m3);
            RebuildStateControllerAssignments();
        }

        public void Update(TimeSpan elapsed)
        {
            if (_m1StateController != null && _m1StateController.ConsumeChangeToTIMSMain())
                MoveTIMSMain(_m1StateController, _m2StateController, _m3StateController);
            else if (_m2StateController != null && _m2StateController.ConsumeChangeToTIMSMain())
                MoveTIMSMain(_m2StateController, _m1StateController, _m3StateController);
            else if (_m3StateController != null && _m3StateController.ConsumeChangeToTIMSMain())
                MoveTIMSMain(_m3StateController, _m1StateController, _m2StateController);

            GetMonitorByType(E233MonitorType.TIMSMain, out var timsMainMonitor, out var timsMainMonitorStateController);
            GetMonitorByType(E233MonitorType.Tid, out var tidMonitor, out var tidMonitorStateController);
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
                    else if (_m1StateController != null) TryChangeToDefaultScreen(_m1, _m1StateController);
                }

                if (_m2 != null && _m2 != timsMainMonitor)
                {
                    if (timsMainMonitor.ActiveScreenId == ScreenIds.X00AA)
                        _m2.ChangeScreen(ScreenIds.S00AA);
                    else if (_m2StateController != null) TryChangeToDefaultScreen(_m2, _m2StateController);
                }

                if (_m3 != null && _m3 != timsMainMonitor)
                {
                    if (timsMainMonitor.ActiveScreenId == ScreenIds.X00AA)
                        _m3.ChangeScreen(ScreenIds.S00AA);
                    else if (_m3StateController != null) TryChangeToDefaultScreen(_m3, _m3StateController);
                }
            }
            else
            {
                if (_m1 != null && _m1StateController != null) TryChangeToDefaultScreen(_m1, _m1StateController);
                if (_m2 != null && _m2StateController != null) TryChangeToDefaultScreen(_m2, _m2StateController);
                if (_m3 != null && _m3StateController != null) TryChangeToDefaultScreen(_m3, _m3StateController);
            }

            if (timsMainMonitor != null)
            {
                if (tidMonitor == null)
                {
                    tidMonitor = timsMainMonitor;
                    tidMonitorStateController = timsMainMonitorStateController;
                    timsMainMonitorStateController.HasOtherMonitorToShowSafetyLamps = false;
                }
                else
                {
                    timsMainMonitorStateController.HasOtherMonitorToShowSafetyLamps = true;
                }
            }

            var isSafetyLampVisibleExternally = tidMonitor != null && (tidMonitor.ActiveScreenId == ScreenIds.Tid
                                                                       || tidMonitor.ActiveScreenId ==
                                                                       ScreenIds.TidChangeToTIMSWarning);
            if (tidMonitorStateController != null)
            {
                tidMonitorStateController.IsSafetyLampsVisibleExternally = false;
                if (_m1StateController != null && _m1StateController != tidMonitorStateController)
                    _m1StateController.IsSafetyLampsVisibleExternally = isSafetyLampVisibleExternally;

                if (_m2StateController != null && _m2StateController != tidMonitorStateController)
                    _m2StateController.IsSafetyLampsVisibleExternally = isSafetyLampVisibleExternally;

                if (_m3StateController != null && _m3StateController != tidMonitorStateController)
                    _m3StateController.IsSafetyLampsVisibleExternally = isSafetyLampVisibleExternally;
            }
            else
            {
                if (_m1StateController != null)
                    _m1StateController.IsSafetyLampsVisibleExternally = false;

                if (_m2StateController != null)
                    _m2StateController.IsSafetyLampsVisibleExternally = false;

                if (_m3StateController != null)
                    _m3StateController.IsSafetyLampsVisibleExternally = false;
            }
        }

        private void RebuildStateControllerAssignments()
        {
            _m1StateController = _m1?.LocalDataHub?.GetOrNull<E233MonitorStateController>();
            _m2StateController = _m2?.LocalDataHub?.GetOrNull<E233MonitorStateController>();
            _m3StateController = _m3?.LocalDataHub?.GetOrNull<E233MonitorStateController>();
            if (_m1StateController != null)
                _m1StateController.MonitorType = E233MonitorType.Meter;
            var m2IsMain = _m2StateController != null &&
                           _m2StateController.MonitorType == E233MonitorType.TIMSMain;
            var m3IsMain = _m3StateController != null &&
                           _m3StateController.MonitorType == E233MonitorType.TIMSMain;

            if (m2IsMain && m3IsMain)
            {
                _m3StateController.MonitorType = E233MonitorType.Tid;
            }
            else if (m2IsMain)
            {
                if (_m3StateController != null) _m3StateController.MonitorType = E233MonitorType.Tid;
            }
            else if (m3IsMain)
            {
                if (_m2StateController != null) _m2StateController.MonitorType = E233MonitorType.Tid;
            }
            else
            {
                if (_m2StateController != null)
                {
                    _m2StateController.MonitorType = E233MonitorType.TIMSMain;
                    if (_m3StateController != null) _m3StateController.MonitorType = E233MonitorType.Tid;
                }
                else if (_m3StateController != null)
                {
                    _m3StateController.MonitorType = E233MonitorType.TIMSMain;
                }
            }
        }

        public void AddMonitor(Monitor monitor)
        {
            if (monitor == null) return;
            if (monitor.Id == MonitorIds.Monitor1) _m1 = monitor;
            else if (monitor.Id == MonitorIds.Monitor2) _m2 = monitor;
            else if (monitor.Id == MonitorIds.Monitor3) _m3 = monitor;
            RebuildStateControllerAssignments();
        }

        public void RemoveMonitor(Monitor monitor)
        {
            if (monitor == null) return;
            E233MonitorStateController removedController;
            if (monitor.Id == MonitorIds.Monitor1)
            {
                removedController = _m1StateController;
                _m1 = null;
                _m1StateController = null;
            }
            else if (monitor.Id == MonitorIds.Monitor2)
            {
                removedController = _m2StateController;
                _m2 = null;
                _m2StateController = null;
            }
            else if (monitor.Id == MonitorIds.Monitor3)
            {
                removedController = _m3StateController;
                _m3 = null;
                _m3StateController = null;
            }
            else
            {
                return;
            }

            var removedWasMain = removedController != null &&
                                 removedController.MonitorType == E233MonitorType.TIMSMain;
            RebuildStateControllerAssignments();
            if (!removedWasMain) return;
            GetMonitorByType(E233MonitorType.TIMSMain, out var newMain, out _);
            if (newMain != null && newMain.ActiveScreenId == ScreenIds.S00AA)
                newMain.ChangeScreen(ScreenIds.X00AA);
        }

        private void GetMonitorByType(E233MonitorType type, out Monitor monitor,
            out E233MonitorStateController monitorStateController)
        {
            if (_m1StateController?.MonitorType == type)
            {
                monitor = _m1;
                monitorStateController = _m1StateController;
                return;
            }

            if (_m2StateController?.MonitorType == type)
            {
                monitor = _m2;
                monitorStateController = _m2StateController;
                return;
            }

            if (_m3StateController?.MonitorType == type)
            {
                monitor = _m3;
                monitorStateController = _m3StateController;
                return;
            }

            monitor = null;
            monitorStateController = null;
        }

        private void MoveTIMSMain(E233MonitorStateController controllerChangingToMain,
            E233MonitorStateController c1, E233MonitorStateController c2)
        {
            var oldType = controllerChangingToMain.MonitorType;
            if (c1 != null && c1.MonitorType == E233MonitorType.TIMSMain)
                c1.MonitorType = oldType;
            else if (c2 != null && c2.MonitorType == E233MonitorType.TIMSMain) c2.MonitorType = oldType;
            controllerChangingToMain.MonitorType = E233MonitorType.TIMSMain;
        }

        private static void TryChangeToDefaultScreen(Monitor monitor, E233MonitorStateController monitorStateController)
        {
            var scr = monitor.ActiveScreenId;
            if (scr != ScreenIds.S00AA && scr != ScreenIds.X00AA) return;
            if (monitorStateController.MonitorType == E233MonitorType.Meter) monitor.ChangeScreen(ScreenIds.Meter);
            else if (monitorStateController.MonitorType == E233MonitorType.Tid) monitor.ChangeScreen(ScreenIds.Tid);
        }
    }
}