using System;
using System.Collections.Generic;
using System.Reflection;
using BveEx.PluginHost;
using BveTypes.ClassWrappers;

namespace JREMonitors.BveEx.Utils
{
    public class PerformanceData
    {
        private static readonly FieldInfo BeIndexCacheField =
            typeof(be).GetField("a", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly FieldInfo FwFieldA =
            typeof(fw).GetField("a", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo FwFieldC =
            typeof(fw).GetField("c", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo FwFieldD =
            typeof(fw).GetField("d", BindingFlags.Instance | BindingFlags.NonPublic);


        public PerformanceData(VehiclePerformance performance)
        {
            if (performance == null) throw new ArgumentNullException(nameof(performance));
            BackupStepSet(performance.Power, PowerSteps);
            BackupStepSet(performance.Brake, BrakeSteps);
        }

        public List<StepSnapshot> PowerSteps { get; } = new List<StepSnapshot>();
        public List<StepSnapshot> BrakeSteps { get; } = new List<StepSnapshot>();

        public static bool TryLoadFromFile(IBveHacker bveHacker, Scenario scenario, string path)
        {
            if (bveHacker == null || scenario == null || string.IsNullOrEmpty(path)) return false;
            var performance = scenario.Vehicle?.Instruments?.Electricity?.Performance;
            if (performance == null) return false;

            var backup = new PerformanceData(performance);
            bveHacker.LoadingProgressForm.ErrorListView.Items.Clear();
            bveHacker.LoadingProgressForm.ErrorCount = 0;
            try
            {
                ClearPerformance(performance);
                performance.LoadFromFile(bveHacker.LoadingProgressForm, path);

                if (bveHacker.LoadingProgressForm.ErrorCount > 0)
                {
                    backup.RestoreTo(performance);
                    return false;
                }

                EnsureStepSetValid(performance.Power);
                EnsureStepSetValid(performance.Brake);
                return true;
            }
            catch (Exception)
            {
                backup.RestoreTo(performance);
                return false;
            }
        }

        public void RestoreTo(VehiclePerformance performance)
        {
            if (performance == null) return;

            RestoreStepSet(performance.Power, PowerSteps);
            RestoreStepSet(performance.Brake, BrakeSteps);

            EnsureStepSetValid(performance.Power);
            EnsureStepSetValid(performance.Brake);
        }

        public static void ClearPerformance(VehiclePerformance performance)
        {
            if (performance == null) return;
            ClearStepSet(performance.Power);
            ClearStepSet(performance.Brake);
        }

        private static void ClearStepSet(VehicleStepSet stepSet)
        {
            if (stepSet?.Src == null) return;
            var rawFw = stepSet.Src;
            FwFieldA?.SetValue(rawFw, null);
            FwFieldC?.SetValue(rawFw, null);
            FwFieldD?.SetValue(rawFw, 0);
        }

        private static void EnsureStepSetValid(VehicleStepSet stepSet)
        {
            if (stepSet?.Src == null) return;
            var rawFw = stepSet.Src;
            if (!(FwFieldA?.GetValue(rawFw) is Array akArray) || akArray.Length == 0) return;
            var currentIdx = (int)(FwFieldD?.GetValue(rawFw) ?? 0);
            if (currentIdx < 0 || currentIdx >= akArray.Length)
            {
                currentIdx = 0;
                FwFieldD?.SetValue(rawFw, 0);
            }

            var currentAk = akArray.GetValue(currentIdx);
            FwFieldC?.SetValue(rawFw, currentAk);
            for (var i = 0; i < akArray.Length; i++)
            {
                if (!(akArray.GetValue(i) is ak akItem)) continue;
                foreach (var pair in akItem.k()) ResetCurveCacheIndex(pair.Value);
            }
        }

        private static void ResetCurveCacheIndex(be curve)
        {
            if (curve == null) return;
            BeIndexCacheField?.SetValue(curve, 0);
        }

        private static void BackupStepSet(VehicleStepSet stepSet, List<StepSnapshot> targetList)
        {
            targetList.Clear();
            if (stepSet == null) return;

            for (var i = 0; i < stepSet.StepCount; i++)
            {
                var step = stepSet.GetStep(i);
                var snap = new StepSnapshot
                {
                    RequiredNotchUp = step.RequiredNotchUp,
                    RequiredNotchDown = step.RequiredNotchDown,
                    JerkRegulationUp = step.JerkRegulationUp,
                    JerkRegulationDown = step.JerkRegulationDown,
                    BreakerDelayOn = step.BreakerDelayOn,
                    BreakerDelayOff = step.BreakerDelayOff,
                    ResetTime = step.ResetTime,
                    CurrentReducingTime = step.CurrentReducingTime,
                    StopDelayUp = step.StopDelayUp,
                    StopDelayDown = step.StopDelayDown,
                    CurrentLimitingValueEmpty = step.CurrentLimitingValueEmpty,
                    CurrentLimitingValueFull = step.CurrentLimitingValueFull
                };

                var rawAk = (ak)step.Src;
                var curvesDict = rawAk.k();

                foreach (var pair in curvesDict)
                {
                    var curveName = pair.Key;
                    var rawCurve = pair.Value;

                    var points = new List<(double Speed, double Value)>(rawCurve.Count);
                    for (var p = 0; p < rawCurve.Count; p++)
                    {
                        var pt = rawCurve[p];
                        points.Add((pt.b(), pt.a()));
                    }

                    snap.Curves[curveName] = points;
                }

                targetList.Add(snap);
            }
        }

        private static void RestoreStepSet(VehicleStepSet stepSet, List<StepSnapshot> sourceList)
        {
            if (stepSet?.Src == null || sourceList == null) return;
            var rawFw = stepSet.Src;
            if (!(FwFieldA?.GetValue(rawFw) is Array akArray) || akArray.Length != sourceList.Count)
            {
                var newArray = new ak[sourceList.Count];
                for (var idx = 0; idx < sourceList.Count; idx++) newArray[idx] = new ak();
                FwFieldA?.SetValue(rawFw, newArray);
            }

            for (var i = 0; i < stepSet.StepCount && i < sourceList.Count; i++)
            {
                var step = stepSet.GetStep(i);
                var snap = sourceList[i];

                step.RequiredNotchUp = snap.RequiredNotchUp;
                step.RequiredNotchDown = snap.RequiredNotchDown;
                step.JerkRegulationUp = snap.JerkRegulationUp;
                step.JerkRegulationDown = snap.JerkRegulationDown;
                step.BreakerDelayOn = snap.BreakerDelayOn;
                step.BreakerDelayOff = snap.BreakerDelayOff;
                step.ResetTime = snap.ResetTime;
                step.CurrentReducingTime = snap.CurrentReducingTime;
                step.StopDelayUp = snap.StopDelayUp;
                step.StopDelayDown = snap.StopDelayDown;
                step.CurrentLimitingValueEmpty = snap.CurrentLimitingValueEmpty;
                step.CurrentLimitingValueFull = snap.CurrentLimitingValueFull;

                var rawAk = (ak)step.Src;
                var curvesDict = rawAk.k();

                foreach (var pair in snap.Curves)
                    if (curvesDict.TryGetValue(pair.Key, out var rawCurve))
                    {
                        rawCurve.Clear();
                        ResetCurveCacheIndex(rawCurve);

                        foreach (var (speed, val) in pair.Value) rawCurve.Add(new fx(speed, val));
                    }
            }
        }

        public class StepSnapshot
        {
            public readonly Dictionary<string, List<(double Speed, double Value)>> Curves
                = new Dictionary<string, List<(double Speed, double Value)>>(StringComparer.OrdinalIgnoreCase);

            public double BreakerDelayOff;
            public double BreakerDelayOn;
            public double CurrentLimitingValueEmpty;
            public double CurrentLimitingValueFull;
            public double CurrentReducingTime;
            public double JerkRegulationDown;
            public double JerkRegulationUp;
            public int RequiredNotchDown;
            public int RequiredNotchUp;
            public double ResetTime;
            public double StopDelayDown;
            public double StopDelayUp;
        }
    }
}