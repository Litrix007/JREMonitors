namespace JREMonitors.Core.Utils
{
    public class ArrayHelper
    {
        public static bool AreSequenceReferencesEqual<T>(T[] left, T[] right) where T : class
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            if (left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++)
                if (!ReferenceEquals(left[i], right[i]))
                    return false;

            return true;
        }
    }
}