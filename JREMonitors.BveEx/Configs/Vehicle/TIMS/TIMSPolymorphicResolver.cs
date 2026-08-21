using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JREMonitors.BveEx.Configs.Vehicle.TIMS
{
    public class TIMSPolymorphicResolver : DefaultJsonTypeInfoResolver
    {
        private readonly Type _signalType;


        public TIMSPolymorphicResolver(Type signalType)
        {
            _signalType = signalType;
        }

        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);
            if (type != typeof(TIMSRouteNodeConfig)) return typeInfo;
            if (typeInfo.PolymorphismOptions == null)
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "type"
                };

            var closedStationConfigType = typeof(TIMSStationConfig<>).MakeGenericType(_signalType);
            typeInfo.PolymorphismOptions.DerivedTypes.Add(
                new JsonDerivedType(closedStationConfigType, "station")
            );
            var closedChangePointType = typeof(TIMSSignalSystemChangePointConfig<>).MakeGenericType(_signalType);
            typeInfo.PolymorphismOptions.DerivedTypes.Add(
                new JsonDerivedType(closedChangePointType, "signalSystemChange")
            );
            return typeInfo;
        }
    }
}