using System;
using System.Collections.Generic;
using JREMonitors.Core.State;
using JREMonitors.E233.TIMS;
using JREMonitors.E233.TIMS.ICCard;
using JREMonitors.JRE.Providers;
using Vortice.Mathematics;

namespace JREMonitors.SandBox.Services
{
    public class MockTIMSICCardService : TIMSICCardService<E233SignalSystem>
    {
        public MockTIMSICCardService(DataHub dataHub, IReadOnlyDictionary<string, TIMSFormationSpec> formationSpecs) :
            base(
                dataHub, formationSpecs, "", "変休B16行路", TIMSDisplayMode.EDen, "10+5",
                E233SignalSystem.Datc,
                new[]
                {
                    new TIMSLeg<E233SignalSystem>("2334F", 'P', "10+5", TIMSDisplayMode.EDen,
                        new TIMSDestination("蒲田"),
                        new TIMSNextDuty("824B", new TimeSpan(0, 23, 04, 30), new TimeSpan(0, 23, 05, 15)), 0,
                        new[]
                        {
                            new TIMSStation<E233SignalSystem>("1", "千葉", 0, TIMSMileageDirection.Increment, 300, 305,
                                TIMSStationSwitchMode.MinStopPosition,
                                E233SignalSystem.Datc)
                            {
                                ShowStopText = true,
                                RadioChannel = "D2",
                                StationTask = TIMSStationTask.Adjustment,
                                ArrivalTime = new TimeSpan(0, 7, 30, 15),
                                DepartureTime = new TimeSpan(0, 7, 30, 45),
                                TrackName = "京4",
                                StationBlockStartOffset = 200,
                                SpeedLimitArrival = "45",
                                SpeedLimitDeparture = "/45",
                                StandardOperatingSpeed = "20"
                            },
                            new TIMSStation<E233SignalSystem>("2", "本千葉", 3000, TIMSMileageDirection.Decrement, 700,
                                705,
                                TIMSStationSwitchMode.MinStopPosition,
                                E233SignalSystem.Datc)
                            {
                                ShowStopText = true,
                                StopDuration = 1,
                                StationBlockStartOffset = 200,
                                // LineStrokeWidth = 5,
                                Color = new Color3(1, 0, 0),
                                LineColor = new Color3(0, 0, 1),
                                LineStrokeWidth = 2,
                                ArrivalTime = new TimeSpan(0, 8, 30, 46),
                                DepartureTime = new TimeSpan(0, 8, 30, 48)
                            },
                            new TIMSStation<E233SignalSystem>("2", "蘇我", null, TIMSMileageDirection.Increment, 1100,
                                1105,
                                TIMSStationSwitchMode.MinStopPosition,
                                E233SignalSystem.Datc)
                            {
                                // ShowStopText = true,
                                IsTimingStation = true,
                                // StopType = TIMSStopType.Pass, 
                                LineStrokeWidth = 3,
                                StationBlockStartOffset = 200,
                                Color = new Color3(0, 1, 0),
                                LineColor = new Color3(0, 1, 0),
                                ArrivalTime = new TimeSpan(0, 8, 35, 30),
                                DepartureTime = new TimeSpan(0, 8, 36, 0)
                            },
                            new TIMSStation<E233SignalSystem>("2", "浜野", 1000, TIMSMileageDirection.Decrement, 1500,
                                1505,
                                TIMSStationSwitchMode.MinStopPosition,
                                E233SignalSystem.Datc)
                            {
                                // ShowStopText = true,
                                Color = new Color3(0, 0, 1),
                                LineColor = new Color3(1, 0, 0),
                                StationBlockStartOffset = 200,
                                // IsTimingStation = true,
                                ArrivalTime = new TimeSpan(0, 8, 36, 30),
                                DepartureTime = new TimeSpan(0, 8, 37, 0)
                            },
                            new TIMSStation<E233SignalSystem>("2", "八幡宿", null, TIMSMileageDirection.Increment, 1900,
                                1905,
                                TIMSStationSwitchMode.MinStopPosition,
                                E233SignalSystem.Datc)
                            {
                                // ShowStopText = true,
                                TrackName = "9",
                                IsTimingStation = true,
                                StationBlockStartOffset = 200,
                                ArrivalTime = new TimeSpan(0, 8, 37, 30),
                                DepartureTime = new TimeSpan(0, 8, 38, 0)
                            },
                            new TIMSStation<E233SignalSystem>("2", "五井", 2000, TIMSMileageDirection.Increment, 2300,
                                2305,
                                TIMSStationSwitchMode.MinStopPosition,
                                E233SignalSystem.Datc)
                            {
                                // ShowStopText = true,
                                TrackName = "\u3000\u30009",
                                IsTimingStation = true,
                                ArrivalTime = new TimeSpan(0, 8, 38, 30),
                                DepartureTime = new TimeSpan(0, 8, 39, 0)
                            }
                        }, Array.Empty<TIMSSignalSystemChangePoint<E233SignalSystem>>(),
                        new[]
                        {
                            new TIMSMileageCorrectionPoint(500, 10000, TIMSMileageDirection.Decrement)
                        }, new[] { (300, 1200, 45) })
                    // new TIMSLeg<E233SignalSystem>("3335F", 'P', "5", TIMSDisplayMode.EDen,
                    //     new TIMSDestination("蒲田"),
                    //     new TIMSNextDuty("824B", new TimeSpan(0, 23, 04, 30), new TimeSpan(0, 23, 05, 15)), 0,
                    //     new[]
                    //     {
                    //       
                    //     }, Array.Empty<TIMSSignalSystemChangePoint<E233SignalSystem>>(),
                    //     new[]
                    //     {
                    //         new TIMSMileageCorrectionPoint(500, 10000, TIMSMileageDirection.Decrement)
                    //     }),
                }
            )
        {
            // Inserted = false;
        }

        public override bool Inserted { get; protected set; } = true;
    }
}