using System;
using System.Collections.Generic;
using System.Linq;
using JREMonitors.BveEx.Registries;
using JREMonitors.E233.Constants;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.ICCard;
using DirectInputIds = JREMonitors.JRE.Constants.DirectInputIds;

namespace JREMonitors.BveEx.Configs.Vehicle
{
    public class E233_1000Config : E233Config
    {
        public override string VehicleName => "E233-1000";
        protected override bool DefaultSupportsTasc => true;
        protected override bool CanSetSupportsTasc => true;

        public override HashSet<string> AllowedInputs => base.AllowedInputs.Union(DirectInputIds.Atc6LampIds)
            .Union(DirectInputIds.DatcLampIds).Union(DirectInputIds.TascBaseLampIds)
            .ToHashSet();

        public override HashSet<string> AllowedBuiltinInputPresets =>
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { BuiltinInputPresetRegistry.Atc6, BuiltinInputPresetRegistry.Mi5000Datc };

        public override HashSet<E233SignalSystem> AllowedSignalSystems => new HashSet<E233SignalSystem>
        {
            E233SignalSystem.Datc, E233SignalSystem.Atc6
        };

        public override TIMSDisplayMode DefaultDisplayMode => TIMSDisplayMode.EDen;

        public override IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs =>
            TIMSFormationSpecs.FormationSpecs1000;
    }
}