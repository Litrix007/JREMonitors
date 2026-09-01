using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using BveEx.PluginHost;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Configs.Vehicle;
using JREMonitors.BveEx.Providers;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.State;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.ICCard;

namespace JREMonitors.BveEx.Services
{
    public class BveTIMSICCardService<TSignal> : TIMSICCardService<TSignal>, IJumpStationListener, IDisposable
        where TSignal : struct, Enum
    {
        private readonly IReadOnlyCollection<TSignal> _allowedSignalSystems;
        private readonly AssistantTextWrapper _assistantTextWrapper;
        private readonly IBveHacker _bveHacker;
        private readonly PerformanceData _initialPerformanceData;
        private readonly string _initialVehicleParametersPath;
        private readonly Scenario _scenario;
        private readonly string _vehicleName;
        private string _currentIcCardPath;
        private bool _firstUpdate = true;
        private ConfigFileWatcher _icCardWatcher;
        private bool _jumping;
        private bool _pendingIcCardReload;
        private Dictionary<string, string> _performanceCurvePaths;
        private Dictionary<string, string> _vehicleParametersMap;
        private bool _shouldForceInstant;

        public BveTIMSICCardService(
            DataHub dataHub,
            JREVehicleConfig<TSignal> vehicleConfig,
            string icCardPath = null
        ) : base(
            dataHub,
            vehicleConfig.FormationSpecs,
            "",
            "",
            vehicleConfig.DefaultDisplayMode,
            vehicleConfig.DefaultFormation,
            vehicleConfig.DefaultSignalSystem,
            Array.Empty<TIMSLeg<TSignal>>()
        )
        {
            _bveHacker = dataHub.Get<IBveHacker>();
            _scenario = dataHub.Get<Scenario>();
            _initialPerformanceData = new PerformanceData(_scenario.Vehicle.Instruments.Electricity.Performance);

            var loadingProgressForm = _bveHacker.LoadingProgressForm;
            var prevErrorCount = loadingProgressForm.ErrorCount;
            var prevAborted = loadingProgressForm.IsAborted;
            var prevItemCount = loadingProgressForm.ErrorListView.Items.Count;
            var vehicleFile = VehicleFile.FromFile(loadingProgressForm,
                _bveHacker.ScenarioInfo.VehicleFiles.SelectedFile.Path);
            var hasNewErrors = loadingProgressForm.ErrorCount > prevErrorCount;
            _initialVehicleParametersPath = !hasNewErrors && vehicleFile?.ParametersPath != null
                ? vehicleFile.ParametersPath
                : null;

            if (hasNewErrors)
            {
                while (loadingProgressForm.ErrorListView.Items.Count > prevItemCount)
                {
                    loadingProgressForm.ErrorListView.Items.RemoveAt(loadingProgressForm.ErrorListView.Items.Count - 1);
                }

                loadingProgressForm.ErrorCount = prevErrorCount;
                loadingProgressForm.IsAborted = prevAborted;
            }

            _allowedSignalSystems = vehicleConfig.AllowedSignalSystems;
            _performanceCurvePaths = GetAbsolutePaths(vehicleConfig.PerformanceCurves);
            _vehicleParametersMap = GetAbsolutePaths(vehicleConfig.VehicleParameters);
            _vehicleName = vehicleConfig.VehicleName;
            _assistantTextWrapper = dataHub.GetOrNull<AssistantTextWrapper>();
            Inserted = false;
            _currentIcCardPath = icCardPath;
            if (!string.IsNullOrEmpty(icCardPath))
            {
                var error = ReloadCard(out var readFilePaths);
                if (error != null)
                    throw new TIMSReloadException(error);
                if (vehicleConfig.EnableHotReload)
                    _icCardWatcher = new ConfigFileWatcher(readFilePaths,
                        () => Volatile.Write(ref _pendingIcCardReload, true));
            }
        }

        public sealed override bool Inserted { get; protected set; }

        public void Dispose()
        {
            _icCardWatcher?.Dispose();
            _icCardWatcher = null;
            var performance = _scenario.Vehicle?.Instruments?.Electricity?.Performance;
            _initialPerformanceData.RestoreTo(performance);
            if (!string.IsNullOrEmpty(_initialVehicleParametersPath) && _scenario.Vehicle != null)
            {
                try
                {
                    VehicleParametersLoader.LoadAndApply(_scenario.Vehicle, _initialVehicleParametersPath);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public void OnJumpStation()
        {
            _jumping = true;
        }

        private static Dictionary<string, string> GetAbsolutePaths(Dictionary<string, ConfigPath> paths)
        {
            return paths.Select(p => (p.Key, p.Value.GetAbsolutePath()))
                .ToDictionary(x => x.Key, x => x.Item2);
        }

        public void Reconfigure(JREVehicleConfig<TSignal> vehicleConfig, string newIcCardPath)
        {
            _currentIcCardPath = newIcCardPath;
            _performanceCurvePaths = GetAbsolutePaths(vehicleConfig.PerformanceCurves);
            _vehicleParametersMap = GetAbsolutePaths(vehicleConfig.VehicleParameters);
            DefaultDisplayMode = vehicleConfig.DefaultDisplayMode;
            DefaultFormation = vehicleConfig.DefaultFormation;
            DefaultSignalSystem = vehicleConfig.DefaultSignalSystem;
            Volatile.Write(ref _pendingIcCardReload, false);
            if (string.IsNullOrEmpty(newIcCardPath))
            {
                _icCardWatcher?.Dispose();
                _icCardWatcher = null;
                ApplyCard("", "", null);
                return;
            }

            var error = ReloadCard(out var readFilePaths);
            if (error != null)
            {
                _icCardWatcher?.Dispose();
                _icCardWatcher = new ConfigFileWatcher(new[] { newIcCardPath },
                    () => Volatile.Write(ref _pendingIcCardReload, true));
                throw new TIMSReloadException(error);
            }

            _icCardWatcher?.Dispose();
            _icCardWatcher = new ConfigFileWatcher(readFilePaths, () => Volatile.Write(ref _pendingIcCardReload, true));
            ForceInstant(true);
        }

        public override void Update(TimeSpan elapsed)
        {
            if (Volatile.Read(ref _pendingIcCardReload))
            {
                Volatile.Write(ref _pendingIcCardReload, false);
                var error = ReloadCard(out var readFilePaths);
                if (error == null)
                {
                    _icCardWatcher?.UpdatePaths(readFilePaths);
                    try
                    {
                        ForceInstant(true);
                        NotifyIcCardReloaded();
                    }
                    catch (Exception ex)
                    {
                        NotifyIcCardReloadFailed(ex.Message);
                    }
                }
                else
                {
                    NotifyIcCardReloadFailed(error);
                }
            }

            if (_jumping)
            {
                _jumping = false;
                _shouldForceInstant = true;
                return;
            }

            if (_firstUpdate || _shouldForceInstant)
            {
                _firstUpdate = false;
                _shouldForceInstant = false;
                ForceInstant();
            }

            base.Update(elapsed);
        }

        private string ReloadCard(out IReadOnlyList<string> readFilePaths)
        {
            if (string.IsNullOrEmpty(_currentIcCardPath) || !File.Exists(_currentIcCardPath))
            {
                ApplyCard("", "", null);
                readFilePaths = null;
                return $"IC Card file not found: {_currentIcCardPath}";
            }

            try
            {
                var result = TIMSICCardLoader.Load(
                    _currentIcCardPath,
                    FormationSpecs,
                    DefaultFormation,
                    DefaultSignalSystem,
                    _allowedSignalSystems,
                    _vehicleName,
                    _bveHacker.MapLoader.Stations,
                    out readFilePaths
                );
                ApplyCard(result.Depot, result.DutyNumber, result.Legs);
                return null;
            }
            catch (Exception ex)
            {
                ApplyCard("", "", null);
                Inserted = false;
                readFilePaths = null;
                return ex.Message;
            }
        }

        private void NotifyIcCardReloaded()
        {
            if (_assistantTextWrapper == null) return;
            _assistantTextWrapper.Text = "[JREMonitors] TIMS IC Card hot reloaded";
            _assistantTextWrapper.Alert(Color.LimeGreen);
        }

        private void NotifyIcCardReloadFailed(string error)
        {
            if (_assistantTextWrapper == null) return;
            _assistantTextWrapper.Text = $"[JREMonitors] TIMS IC Card hot reload failed: {error}";
            _assistantTextWrapper.Alert(Color.Red);
            Debugger?.AddLineLasting($"TIMS IC Card hot reload failed: {error}");
        }

        private void NotifyPerformanceCurveLoadFailed(string performanceCurvePath)
        {
            if (_assistantTextWrapper == null) return;
            _assistantTextWrapper.Text =
                $"[JREMonitors] Vehicle performance curve load failed: {performanceCurvePath}";
            _assistantTextWrapper.Alert(Color.Red);
            Debugger?.AddLineLasting($"Vehicle performance curve load failed: {performanceCurvePath}");
        }

        private void NotifyVehicleParametersLoadFailed(string vehicleParametersPath, Exception e)
        {
            if (_assistantTextWrapper == null) return;
            _assistantTextWrapper.Text =
                $"[JREMonitors] Vehicle parameters '{vehicleParametersPath}' load failed: {e}";
            _assistantTextWrapper.Alert(Color.Red);
            Debugger?.AddLineLasting($"Vehicle parameters '{vehicleParametersPath}' load failed: {e}");
        }

        private void NotifyVehicleParametersRestoreFailed(string vehicleParametersPath, Exception e)
        {
            if (_assistantTextWrapper == null) return;
            _assistantTextWrapper.Text =
                $"[JREMonitors] Vehicle parameters '{vehicleParametersPath}' restore failed: {e}";
            _assistantTextWrapper.Alert(Color.Red);
            Debugger?.AddLineLasting($"Vehicle parameters '{vehicleParametersPath}' restore failed: {e}");
        }

        protected override void OnFormationChanged(string formation, TIMSFormationSpec formationSpec, bool isHotReload)
        {
            var motorCarCount = 0;
            var trailerCarCount = 0;
            for (var i = 0; i < formationSpec.CarCount; i++)
            {
                var type = formationSpec[i].CarType;
                if (type == TIMSCarType.MotorCar)
                    motorCarCount++;
                else
                    trailerCarCount++;
            }
            var canRestore = !string.IsNullOrEmpty(_initialVehicleParametersPath);
            if (_vehicleParametersMap.TryGetValue(formation, out var vehicleParameterPath) &&
                vehicleParameterPath != null)
            {
                try
                {
                    VehicleParametersLoader.LoadAndApply(_scenario.Vehicle, vehicleParameterPath, motorCarCount,
                        trailerCarCount);
                    Debugger?.AddLineLasting($"Vehicle parameters '{vehicleParameterPath}' loaded");
                }
                catch (Exception e)
                {
                    if (isHotReload)
                        throw new TIMSReloadException(
                            $"Vehicle parameters '{vehicleParameterPath}' load failed: {e}");

                    if (canRestore)
                    {
                        try
                        {
                            VehicleParametersLoader.LoadAndApply(_scenario.Vehicle, _initialVehicleParametersPath,
                                motorCarCount, trailerCarCount);
                        }
                        catch (Exception)
                        {
                            // 忽略恢复失败
                        }
                    }

                    NotifyVehicleParametersLoadFailed(vehicleParameterPath, e);
                    return;
                }
            }
            else if (canRestore)
            {
                try
                {
                    VehicleParametersLoader.LoadAndApply(_scenario.Vehicle, _initialVehicleParametersPath,
                        motorCarCount, trailerCarCount);
                    Debugger?.AddLineLasting($"Vehicle parameters '{_initialVehicleParametersPath}' restored");
                }
                catch (Exception e)
                {
                    if (isHotReload)
                        throw new TIMSReloadException(
                            $"Vehicle parameters '{_initialVehicleParametersPath}' load failed: {e}");
                    NotifyVehicleParametersRestoreFailed(_initialVehicleParametersPath, e);
                    return;
                }
            }

            if (_performanceCurvePaths.TryGetValue(formation, out var performanceCurvePath) &&
                performanceCurvePath != null)
            {
                if (!PerformanceData.TryLoadFromFile(_bveHacker, _scenario, performanceCurvePath))
                {
                    if (isHotReload)
                        throw new TIMSReloadException($"Vehicle performance curve load failed: {performanceCurvePath}");
                    NotifyPerformanceCurveLoadFailed(performanceCurvePath);
                }
                else
                {
                    Debugger?.AddLineLasting($"Vehicle performance curve '{performanceCurvePath}' loaded");
                }
            }
            else
            {
                _initialPerformanceData.RestoreTo(_scenario.Vehicle.Instruments.Electricity.Performance);
                Debugger?.AddLineLasting("Vehicle performance curve restored");
            }
        }

        private sealed class TIMSReloadException : Exception
        {
            public TIMSReloadException(string message) : base(message)
            {
            }
        }
    }
}