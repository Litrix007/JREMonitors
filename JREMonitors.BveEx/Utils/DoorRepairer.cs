using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BveTypes.ClassWrappers;

namespace JREMonitors.BveEx.Utils
{
    public static class DoorRepairer
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly MethodInfo MethodLeftOpen = typeof(cg).GetMethod("d", Flags, null,
            new[] { typeof(object), typeof(EventArgs) }, null);

        private static readonly MethodInfo MethodLeftClose = typeof(cg).GetMethod("c", Flags, null,
            new[] { typeof(object), typeof(EventArgs) }, null);

        private static readonly MethodInfo MethodRightOpen = typeof(cg).GetMethod("b", Flags, null,
            new[] { typeof(object), typeof(EventArgs) }, null);

        private static readonly MethodInfo MethodRightClose = typeof(cg).GetMethod("a", Flags, null,
            new[] { typeof(object), typeof(EventArgs) }, null);

        private static readonly MethodInfo AvTickMethod = typeof(av).GetMethod("a", Flags, null,
            new[] { typeof(object), typeof(EventArgs) }, null);

        private static readonly FieldInfo CnField = typeof(f).GetField("b", Flags);

        private static object _lastVehicleSrc;
        private static object _cachedCgInstance;

        private static cg GetActiveCgInstance(Vehicle vehicle)
        {
            if (vehicle == null) return null;
            var vehicleSrc = vehicle.Src;
            if (vehicleSrc == null) return null;

            if (vehicleSrc == _lastVehicleSrc && _cachedCgInstance != null)
                return (cg)_cachedCgInstance;

            _lastVehicleSrc = vehicleSrc;
            _cachedCgInstance = null;

            var candidates = new List<object> { vehicleSrc };
            var vehicleFields = vehicleSrc.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            candidates.AddRange(vehicleFields.Select(field => field.GetValue(vehicleSrc)).Where(val => val != null));
            foreach (var obj in candidates)
            {
                var objFields = obj.GetType().GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

                foreach (var f in objFields)
                {
                    if (!typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                    if (!(f.GetValue(obj) is Delegate del)) continue;
                    var invocationList = del.GetInvocationList();
                    foreach (var d in invocationList)
                        if (d.Target is cg)
                        {
                            _cachedCgInstance = d.Target;
                            return (cg)d.Target;
                        }
                }
            }

            return null;
        }

        public static void CleanUpOldDoors(Vehicle vehicle)
        {
            if (vehicle == null) return;
            if (CnField == null || AvTickMethod == null) return;
            if (!(vehicle.Src is f vehicleSrc)) return;
            if (!(CnField.GetValue(vehicleSrc) is cn cnInstance)) return;
            var leftDoorSet = vehicle.Doors.GetSide(DoorSide.Left);
            var rightDoorSet = vehicle.Doors.GetSide(DoorSide.Right);
            var oldDoors = leftDoorSet.CarDoors.Concat(rightDoorSet.CarDoors)
                .Select(d => d.Src)
                .ToList();
            foreach (var oldDoor in oldDoors)
            {
                if (oldDoor == null) continue;
                try
                {
                    var handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), oldDoor, AvTickMethod);
                    cnInstance.d(handler);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public static void RepairDoorSounds(Vehicle vehicle)
        {
            if (vehicle == null) return;
            var cgInstance = GetActiveCgInstance(vehicle);
            if (cgInstance == null) return;
            if (MethodLeftOpen == null || MethodLeftClose == null || MethodRightOpen == null ||
                MethodRightClose == null) return;
            var handlerLeftOpen =
                (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), cgInstance, MethodLeftOpen);
            var handlerLeftClose =
                (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), cgInstance, MethodLeftClose);
            var handlerRightOpen =
                (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), cgInstance, MethodRightOpen);
            var handlerRightClose =
                (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), cgInstance, MethodRightClose);
            var leftDoorSet = vehicle.Doors.GetSide(DoorSide.Left);
            var rightDoorSet = vehicle.Doors.GetSide(DoorSide.Right);
            if (leftDoorSet.CarDoors.Count == 0 || rightDoorSet.CarDoors.Count == 0) return;
            if (!(leftDoorSet.CarDoors[0].Src is av leftFirstDoor) ||
                !(rightDoorSet.CarDoors[0].Src is av rightFirstDoor)) return;
            leftFirstDoor.b(handlerLeftOpen);
            leftFirstDoor.e(handlerLeftClose);
            rightFirstDoor.b(handlerRightOpen);
            rightFirstDoor.e(handlerRightClose);
        }
    }
}