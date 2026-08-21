using System.Collections.Generic;
using System.Linq;
using BveTypes.ClassWrappers;
using BveTypes.ClassWrappers.Extensions;

namespace JREMonitors.BveEx.Utils
{
    public static class BveListHelper
    {
        public static void SortElements(this VehiclePanel panel)
        {
            // HACK 直接对Elements进行ToList会导致StackOverflowException，原因未知
            var count = panel.Elements.Count;
            var list = new List<VehiclePanelElement>(count);
            for (var i = 0; i < count; i++) list.Add(panel.Elements[i]);
            var sortedList = list.OrderBy(element => element.Layer).ToList();
            for (var i = 0; i < count; i++) panel.Elements[i] = sortedList[i];
        }

        public static List<T> ToListSafe<T>(this WrappedList<T> wrappedList)
        {
            var list = new List<T>(wrappedList.Count);
            for (var i = 0; i < wrappedList.Count; i++)
            {
                var item = wrappedList[i];
                list.Add(item);
            }

            return list;
        }

        public static List<KeyValuePair<TKey, TValue>> ToListSafe<TKey, TValue>(
            this WrappedSortedList<TKey, TValue> wrappedSortedList)
        {
            var list = new List<KeyValuePair<TKey, TValue>>(wrappedSortedList.Count);
            foreach (var pair in wrappedSortedList) list.Add(pair);
            return list;
        }
    }
}