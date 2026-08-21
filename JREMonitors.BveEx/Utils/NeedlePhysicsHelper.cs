using System.Reflection;
using BveTypes.ClassWrappers;

namespace JREMonitors.BveEx.Utils
{
    public static class NeedlePhysicsHelper
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo FieldA = typeof(cb).GetField("a", Flags);

        public static void InjectPhysicsSolver(Needle needle, Scenario scenario)
        {
            if (!(needle.Src is cb needleSrc)) return;
            if (FieldA.GetValue(needleSrc) != null) return;
            if (!(scenario.TimeManager.Src is cn timeManagerSrc)) return;
            var uInstance = new u(timeManagerSrc);
            FieldA.SetValue(needleSrc, uInstance);
        }
    }
}