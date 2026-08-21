using System.Collections.Generic;
using System.Linq;

namespace JREMonitors.BveEx.Services.Car
{
    public static class CarDoorIds
    {
        public static string At(int carIndex)
        {
            return $"carDoor{carIndex + 1}";
        }

        public static HashSet<string> All(int carCount)
        {
            return new HashSet<string>(Enumerable.Range(0, carCount).Select(At));
        }
    }
}