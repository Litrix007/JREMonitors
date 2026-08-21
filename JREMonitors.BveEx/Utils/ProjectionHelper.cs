using System;
using System.Reflection;
using BveTypes.ClassWrappers;
using JREMonitors.Core.Constants;
using SlimDX;
using Vortice.Mathematics;
using Vector2 = System.Numerics.Vector2;

namespace JREMonitors.BveEx.Utils
{
    public static class ProjectionHelper
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly FieldInfo FieldK = typeof(f).GetField("k", Flags);
        private static readonly FieldInfo FieldF = typeof(fj).GetField("f", Flags);
        private static readonly FieldInfo FieldA = typeof(al).GetField("a", Flags);

        private static Matrix GetCameraVibrationMatrix(Vehicle vehicle)
        {
            if (!(vehicle.Src is f vehicleSrc) || !(FieldK.GetValue(vehicleSrc) is fj fjInstance) ||
                !(FieldF.GetValue(fjInstance) is al alInstance)) return Matrix.Identity;
            return FieldA.GetValue(alInstance) is Matrix matrix ? matrix : Matrix.Identity;
        }

        public static Vector2 ProjectMouseClickToPanel(
            Vehicle vehicle,
            float xWin,
            float yWin,
            float windowWidth,
            float windowHeight
        )
        {
            var xNorm = xWin / windowWidth;
            var yNorm = yWin / windowHeight;

            var plane = vehicle.CameraLocation.Plane;
            var xPlane = plane.X + xNorm * plane.Width;
            var yPlane = plane.Y + yNorm * plane.Height;

            var vCamLocal = new Vector3(xPlane, yPlane, 1.0f);

            var mRel = vehicle.CameraLocation.TransformFromCameraHomePosition;
            var mInv = Matrix.Invert(mRel);
            var mVib = GetCameraVibrationMatrix(vehicle);
            var mVibInv = Matrix.Invert(mVib);

            var mCorrected = mInv * mVibInv;
            var vHome = Vector3.TransformCoordinate(vCamLocal, mCorrected);
            var originX = vehicle.Panel.Origin.X;
            var originY = vehicle.Panel.Origin.Y;

            if (MathHelper.Abs(vHome.Z) < Epsilons.FloatEpsilon) return new Vector2(originX, originY);
            var xProjected = vHome.X / vHome.Z;
            var yProjected = vHome.Y / vHome.Z;

            var deltaX = xProjected * (float)vehicle.Panel.Resolution;
            var deltaY = -yProjected * (float)vehicle.Panel.Resolution;

            var xPixel = originX + deltaX;
            var yPixel = originY + deltaY;

            return new Vector2(xPixel, yPixel);
        }

        public static Vector2? UnprojectPanelToLocal(Vector2 panelPos, Matrix r3d, Vector2 origin, VehiclePanel panel)
        {
            var rp = (float)(panel.Resolution * panel.Perspective);
            var deltaX = panelPos.X - panel.Origin.X;
            var deltaY = panelPos.Y - panel.Origin.Y;

            var v = new Vector3(deltaX, -deltaY, rp);
            var b = new Vector3(panel.Origin.X, -panel.Origin.Y, -rp);

            var m = r3d * Matrix.Translation(origin.X, -origin.Y, 0f);
            var n = Matrix.Invert(m);

            var vn = Vector3.TransformNormal(v, n);
            var bn = Vector3.TransformCoordinate(b, n);
            if (Math.Abs(vn.Z) < Epsilons.FloatEpsilon) return null;
            var lambda = -bn.Z / vn.Z;
            if (lambda <= 0f || lambda > 100000f) return null;
            var result = bn + lambda * vn;
            return new Vector2(result.X, result.Y);
        }

        public static Vector2 ProjectLocalToPanel(Vector3 localPos, Matrix rotation3D, Vector2 origin,
            VehiclePanel panel)
        {
            var m = rotation3D * Matrix.Translation(origin.X, -origin.Y, 0f);
            var posWorld = Vector3.TransformCoordinate(localPos, m);

            var distanceFromDriver = (float)panel.DistanceFromDriver;
            var perspective = (float)panel.Perspective;
            var scale = distanceFromDriver / (float)panel.Resolution;

            var zWorld = scale * posWorld.Z + distanceFromDriver * perspective;
            if (Math.Abs(zWorld) < Epsilons.FloatEpsilon) zWorld = Epsilons.FloatEpsilon;
            var projFactor = distanceFromDriver * perspective / zWorld;

            var xProj = panel.Origin.X + (posWorld.X - panel.Origin.X) * projFactor;
            var yProj = panel.Origin.Y + (-posWorld.Y - panel.Origin.Y) * projFactor;

            return new Vector2(xProj, yProj);
        }
    }
}