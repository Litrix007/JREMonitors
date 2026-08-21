using System.Collections.Generic;

namespace JREMonitors.E233.TIMS
{
    public static class TIMSFormationSpecs
    {
        public static readonly IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs0 =
            new Dictionary<string, TIMSFormationSpec>
            {
                ["4after6"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(7, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, true, false,
                        false),
                    new TIMSCarSpec(9, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(10, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                }),

                ["6"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(5, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(6, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                }),

                ["10"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, true),
                    new TIMSCarSpec(5, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(6, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(7, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(9, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(10, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                }),

                ["10g"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, true, false,
                        false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(5, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(6, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(7, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(9, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(10, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                }),

                ["6+4"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(5, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(6, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(7, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, true, false,
                        false),
                    new TIMSCarSpec(9, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(10, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                }),

                ["4after8"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, true, false,
                        false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                }),

                ["8"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, true, false,
                        false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        false),
                    new TIMSCarSpec(5, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        false),
                    new TIMSCarSpec(6, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(7, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(8, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                }),

                ["12"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, true, false,
                        false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        false),
                    new TIMSCarSpec(5, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        false),
                    new TIMSCarSpec(6, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(7, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(9, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(10, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(11, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(12, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                }),

                ["8+4"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, true, false,
                        false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        false),
                    new TIMSCarSpec(5, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        false),
                    new TIMSCarSpec(6, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(7, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(8, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(9, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(10, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, true, false,
                        false),
                    new TIMSCarSpec(11, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(12, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                })
            };

        public static readonly IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs1000 =
            new Dictionary<string, TIMSFormationSpec>
            {
                ["10"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, false, false),
                    new TIMSCarSpec(5, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(6, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(7, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, true, false, false),
                    new TIMSCarSpec(9, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(10, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                })
            };

        public static readonly IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs3000 =
            new Dictionary<string, TIMSFormationSpec>
            {
                ["5"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, false, true),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, true, false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(4, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(5, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, false, false)
                }),

                ["10"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, false, true),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, true, false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(4, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        false),
                    new TIMSCarSpec(5, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        true),
                    new TIMSCarSpec(6, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(7, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, false, false, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, true, false),
                    new TIMSCarSpec(9, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, false, false, false),
                    new TIMSCarSpec(10, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, false, true)
                }),

                ["10+5"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, false, true),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, true, false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(4, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        false),
                    new TIMSCarSpec(5, TIMSCarType.GreenCar, TIMSCarPantoGraphType.None, 2, false, false,
                        true),
                    new TIMSCarSpec(6, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(7, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, false, false, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, true, false),
                    new TIMSCarSpec(9, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, false, false, false),
                    new TIMSCarSpec(10, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, false, true),
                    new TIMSCarSpec(11, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, false, true),
                    new TIMSCarSpec(12, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, true, false),
                    new TIMSCarSpec(13, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(14, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(15, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, false, false)
                })
            };

        public static readonly IReadOnlyDictionary<string, TIMSFormationSpec> FormationSpecs5000 =
            new Dictionary<string, TIMSFormationSpec>
            {
                ["10"] = new TIMSFormationSpec(new[]
                {
                    new TIMSCarSpec(1, TIMSCarType.FirstCar, TIMSCarPantoGraphType.None, 4, false, true, false),
                    new TIMSCarSpec(2, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(3, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, false, false, false),
                    new TIMSCarSpec(4, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, false, false),
                    new TIMSCarSpec(5, TIMSCarType.MotorCar, TIMSCarPantoGraphType.WithYellowRect, 4, false, false,
                        false),
                    new TIMSCarSpec(6, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(7, TIMSCarType.TrailerCar, TIMSCarPantoGraphType.None, 4, false, false, false),
                    new TIMSCarSpec(8, TIMSCarType.MotorCar, TIMSCarPantoGraphType.None, 4, true, false, false),
                    new TIMSCarSpec(9, TIMSCarType.MotorCar, TIMSCarPantoGraphType.Has, 4, false, false, false),
                    new TIMSCarSpec(10, TIMSCarType.LastCar, TIMSCarPantoGraphType.None, 4, false, true, false)
                })
            };
    }
}