using System;
using System.Collections.Generic;
using System.Linq;
using JREMonitors.BveEx.Registries;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.JRE.Constants;
using JREMonitors.JRE.Providers;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    // ReSharper disable once InconsistentNaming
    public class E233_0Config : E233Config
    {
        public override string VehicleName => "E233-0";

        public override HashSet<string> AllowedInputs =>
            base.AllowedInputs
                .Union(new[] { DirectInputIds.HoldSpeed })
                .Union(DirectInputIds.AtsPLampIds)
                .Union(DirectInputIds.AtsSLampIds)
                .Union(DirectInputIds.TascBaseLampIds)
                .Union(DirectInputIds.TascExtraLampIds)
                .ToHashSet();

        public override HashSet<string> AllowedBuiltinInputPresets { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { BuiltinInputPresetRegistry.GapAtsP, BuiltinInputPresetRegistry.GapAtsS };

        public override HashSet<E233SignalSystem> AllowedSignalSystems => null;
        public override TIMSDisplayMode DefaultDisplayMode => TIMSDisplayMode.MDen;

        public override IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs =>
            TIMSFormationSpecs.FormationSpecs0;
    }
}