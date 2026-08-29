using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BveTypes.ClassWrappers;

namespace JREMonitors.BveEx.Utils
{
    public static class CarCountHelper
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

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

        private static readonly FieldInfo BrField = typeof(f).GetField("e", Flags);

        private static readonly FieldInfo[] BfFields = typeof(bf).GetFields(Flags);

        private static object _lastVehicleSrc;
        private static object _cachedCgInstance;

        public static void SetCarCount(Vehicle vehicle, int count, double carLength = 0)
        {
            if (vehicle == null || count <= 0) return;

            var leftSideDoors = vehicle.Doors.GetSide(DoorSide.Left);
            var rightSideDoors = vehicle.Doors.GetSide(DoorSide.Right);

            CleanUpOldDoors(vehicle);
            // HACK 不能用 vehicle.Doors.SetCarLength，会导致车门永远无法关闭
            SetSideCarCount(leftSideDoors, count);
            SetSideCarCount(rightSideDoors, count);
            RepairDoorSounds(vehicle);
            EnsureFbDoorArraySize(vehicle);
            if (carLength <= 0 && vehicle.Dynamics.CarLength > 0)
            {
                carLength = vehicle.Dynamics.CarLength;
            }

            if (carLength <= 0)
            {
                carLength = 20.0;
            }

            UpdateFgLength(vehicle, count, carLength);
        }

        private static void UpdateFgLength(Vehicle vehicle, int carCount, double carLength)
        {
            if (vehicle == null || !(vehicle.Src is f vehicleSrc) || BrField == null) return;
            if (!(BrField.GetValue(vehicleSrc) is br brInstance)) return;
            var fgInstance = brInstance.w();
            if (fgInstance == null) return;
            var totalTrainLength = carCount * carLength;
            fgInstance.a(totalTrainLength);
        }

        private static void SetSideCarCount(SideDoorSet doorSet, int length)
        {
            doorSet.SetCarLength(length);
            doorSet.SetState(doorSet.IsOpen ? DoorState.Open : DoorState.Close);
        }

        private static void CleanUpOldDoors(Vehicle vehicle)
        {
            if (vehicle == null || CnField == null || AvTickMethod == null) return;
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

        private static void RepairDoorSounds(Vehicle vehicle)
        {
            if (vehicle == null) return;
            var cgInstance = GetActiveCgInstance(vehicle);
            if (cgInstance == null || MethodLeftOpen == null || MethodLeftClose == null ||
                MethodRightOpen == null || MethodRightClose == null) return;

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

        private static void EnsureFbDoorArraySize(Vehicle vehicle)
        {
            if (vehicle == null || !(vehicle.Src is f vehicleSrc)) return;
            var bfInstance = vehicleSrc.h();
            if (bfInstance == null) return;
            var fbInstance = GetFbFromBf(bfInstance);
            if (fbInstance == null) return;

            var leftCount = vehicle.Doors.GetSide(DoorSide.Left).CarDoors.Count;
            var rightCount = vehicle.Doors.GetSide(DoorSide.Right).CarDoors.Count;

            if (fbInstance.v != null && fbInstance.v.Length < leftCount)
            {
                Array.Resize(ref fbInstance.v, leftCount);
            }

            if (fbInstance.w != null && fbInstance.w.Length < rightCount)
            {
                Array.Resize(ref fbInstance.w, rightCount);
            }
        }

        private static fb GetFbFromBf(bf bfInstance)
        {
            for (var i = 0; i < BfFields.Length; i++)
            {
                if (BfFields[i].FieldType == typeof(fb))
                {
                    return BfFields[i].GetValue(bfInstance) as fb;
                }
            }

            return null;
        }

        private static cg GetActiveCgInstance(Vehicle vehicle)
        {
            if (vehicle == null || vehicle.Src == null) return null;
            var vehicleSrc = vehicle.Src;

            if (vehicleSrc == _lastVehicleSrc && _cachedCgInstance != null)
                return (cg)_cachedCgInstance;

            _lastVehicleSrc = vehicleSrc;
            _cachedCgInstance = null;

            var candidates = new List<object> { vehicleSrc };
            var vehicleFields = vehicleSrc.GetType().GetFields(Flags);
            candidates.AddRange(vehicleFields.Select(field => field.GetValue(vehicleSrc)).Where(val => val != null));

            foreach (var obj in candidates)
            {
                var objFields = obj.GetType().GetFields(Flags);
                foreach (var fld in objFields)
                {
                    if (!typeof(Delegate).IsAssignableFrom(fld.FieldType)) continue;
                    if (!(fld.GetValue(obj) is Delegate del)) continue;
                    var invocationList = del.GetInvocationList();
                    foreach (var d in invocationList)
                    {
                        if (d.Target is cg cgInstance)
                        {
                            _cachedCgInstance = cgInstance;
                            return cgInstance;
                        }
                    }
                }
            }

            return null;
        }
    }
}