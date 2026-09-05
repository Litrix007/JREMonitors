using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BveTypes.ClassWrappers;

namespace JREMonitors.BveEx.Utils
{
    /// <summary>
    ///     编组辆数迁移辅助类。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         热重载改变辆数后，用反射补齐 BVE 内部按"旧车门数"定长的状态，
    ///         使车门系统、关门状态机、开/关门音效、门状态数组与编组总长整体迁移到新辆数。
    ///     </para>
    ///     <para>
    ///         BVE 的车门数重建涉及多处内部状态：<see cref="cn" /> 的全局 Tick 事件、按侧重建的车门列表
    ///         <see cref="cg" /> 挂在首门上的开/关音效回调、<see cref="fb" /> 的
    ///         <c>v/w</c> 逐车门开闭状态数组、以及 <see cref="fg" /> 的编组总长。任何一处遗漏都会
    ///         造成越界、状态残留或静音门。
    ///     </para>
    ///     <para>
    ///         本类按 <see cref="SetCarCount" /> 的执行顺序依次完成：解除旧门 Tick 订阅
    ///         （<see cref="CleanUpOldDoors" />）→ 按侧重建车门并恢复开/关状态（<see cref="SetSideCarCount" />）→
    ///         将 <see cref="cg" /> 四个开/关音效回调重新挂到新首门（<see cref="RepairDoorSounds" />，经反射与
    ///         InvocationList 定位 <see cref="cg" /> 实例）→ 按新辆数重建 <see cref="fb" />
    ///         的 <c>v/w</c> 数组（<see cref="EnsureFbDoorArraySize" />）→ 更新 <see cref="fg" /> 编组总长为
    ///         辆数×车长（<see cref="UpdateFgLength" />）。
    ///     </para>
    /// </remarks>
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

            if (carLength <= 0 && vehicle.Dynamics.CarLength > 0) carLength = vehicle.Dynamics.CarLength;

            if (carLength <= 0) carLength = 20.0;

            if (leftSideDoors.CarDoors.Count != count || rightSideDoors.CarDoors.Count != count)
            {
                CleanUpOldDoors(vehicle);
                SetSideCarCount(leftSideDoors, count);
                SetSideCarCount(rightSideDoors, count);
                RepairDoorSounds(vehicle);
                EnsureFbDoorArraySize(vehicle);
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
            var wasOpen = doorSet.IsOpen;
            var wasClosing = doorSet.CarDoors.Any(d => d.IsOpen && d.State == DoorState.Close);
            doorSet.SetCarLength(length);
            if (wasClosing)
            {
                doorSet.SetState(DoorState.Open);
                doorSet.CloseDoors(0);
            }
            else if (wasOpen)
            {
                doorSet.SetState(DoorState.Open);
            }
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
            // fb.v/w 在 fb 构造时按当时的车门数一次性定长；热重载改变车门数后必须重新分配为精确长度。
            if (fbInstance.v == null || fbInstance.v.Length != leftCount) fbInstance.v = new double[leftCount];

            if (fbInstance.w == null || fbInstance.w.Length != rightCount) fbInstance.w = new double[rightCount];
        }

        private static fb GetFbFromBf(bf bfInstance)
        {
            for (var i = 0; i < BfFields.Length; i++)
                if (BfFields[i].FieldType == typeof(fb))
                    return BfFields[i].GetValue(bfInstance) as fb;

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
                        if (d.Target is cg cgInstance)
                        {
                            _cachedCgInstance = cgInstance;
                            return cgInstance;
                        }
                }
            }

            return null;
        }
    }
}