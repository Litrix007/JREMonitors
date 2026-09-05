using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BveTypes.ClassWrappers;

namespace JREMonitors.BveEx.Utils
{
    /// <summary>
    ///     车辆参数加载器，解析 BVE 车辆参数文件并应用。
    /// </summary>
    public static class VehicleParametersLoader
    {
        private static readonly FieldInfo B2FieldM =
            typeof(b2).GetField("m", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo B2FieldQ =
            typeof(b2).GetField("q", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly string[] SupportedVersions =
        {
            "bvets vehicle parameters 0.04",
            "bvets vehicle parameters 1.00",
            "bvets vehicle parameters 1.01",
            "bvets vehicle parameters 2.00",
            "bvets vehicle parameters 2.01"
        };

        public static void LoadAndApply(Vehicle vehicle, string parameterFilePath, int? motorCarCount = null,
            int? trailerCarCount = null)
        {
            if (vehicle == null || !(vehicle.Src is f vehicleSrc)) return;
            if (!File.Exists(parameterFilePath))
                throw new FileNotFoundException($"Parameter file not found: {parameterFilePath}");

            var applyActions = new List<Action<f>>();
            var baseVolumeRatio = 20.0; // 默认容积比 20:1，对应 az 的 fr.e 初值 0.05
            applyActions.Add(ResetAllParametersToDefaults);

            using (var reader = new e8(parameterFilePath, SupportedVersions))
            {
                if (reader.b().c() == SupportedVersions[0] ||
                    reader.b().c() == SupportedVersions[1] ||
                    reader.b().c() == SupportedVersions[2])
                    reader.a(new[] { ';' });

                while (reader.k())
                    if (string.IsNullOrEmpty(reader.a()))
                    {
                        var lowerSection = reader.n().ToLower();
                        switch (lowerSection)
                        {
                            case "[cl]":
                                applyActions.Add(v => v.f().b().a(v.f().b().c())); break;
                            case "[smee]":
                                applyActions.Add(v =>
                                {
                                    v.f().b().a(v.f().b().h());
                                    v.f().b().g().g(0.94);
                                    v.f().b().d().g(0.94);
                                    v.f().b().g().i(30000.0);
                                    v.f().b().d().i(30000.0);
                                }); break;
                            case "[ecb]":
                                applyActions.Add(v => v.f().b().a(v.f().b().f())); break;
                            case "[airsupplement]":
                                applyActions.Add(v => v.f().b().a(v.f().b().g())); break;
                            case "[lockoutvalve]":
                                applyActions.Add(v => v.f().b().a(v.f().b().d())); break;
                            case "[bcservo]":
                                applyActions.Add(v =>
                                {
                                    v.f().b().l().d().a(true);
                                    v.f().b().m().d().a(true);
                                }); break;
                            case "[bc]":
                                applyActions.Add(v =>
                                {
                                    v.f().b().l().d().a(false);
                                    v.f().b().m().d().a(false);
                                }); break;
                            case "[brakereadhesion]":
                                applyActions.Add(v =>
                                {
                                    v.f().b().l().c().a(true);
                                    v.f().b().m().c().a(true);
                                }); break;
                            case "[powerreadhesion]":
                                applyActions.Add(v => v.f().a().l().a(true)); break;
                            case "[onelevercab]":
                            case "[cab]":
                            case "[viewpoint]":
                                break;
                        }
                    }
                    else
                    {
                        var lowerSection = reader.n().ToLower();
                        var lowerKey = reader.a().ToLower();

                        var handled = true;

                        switch (lowerSection)
                        {
                            case "[constantspeedcontrol]":
                                if (lowerKey == "power")
                                {
                                    double val = reader.m();
                                    applyActions.Add(v => v.f().c().a((int)val));
                                }
                                else if (lowerKey == "brake")
                                {
                                    double val = reader.m();
                                    applyActions.Add(v => v.f().c().b((int)val));
                                }
                                else if (lowerKey == "neutral")
                                {
                                    double val = reader.m();
                                    applyActions.Add(v => v.f().c().c((int)val));
                                }

                                break;

                            case "[sap]":
                                if (lowerKey == "applyspeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().h().a().g(val));
                                }
                                else if (lowerKey == "releasespeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().h().a().j(val));
                                }
                                else if (lowerKey == "volumeratio")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().h().a().i(1.0 / val));
                                }

                                break;

                            case "[dynamics]":
                                if (lowerKey == "motorcarweight")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.g().j().g(val));
                                }
                                else if (lowerKey == "trailerweight")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.g().d().g(val));
                                }
                                else if (lowerKey == "motorcarcount")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.g().j().a(val));
                                }
                                else if (lowerKey == "trailercount")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.g().d().a(val));
                                }
                                else if (lowerKey == "motorcarinertiafactor" || lowerKey == "motorcarinatiafactor")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.g().j().h(val));
                                }
                                else if (lowerKey == "trailerinertiafactor" || lowerKey == "trailerinatiafactor")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.g().d().h(val));
                                }
                                else if (lowerKey == "carlength")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.g().e(val));
                                }
                                else if (lowerKey == "curveresistancefactor")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.g().g(val));
                                }
                                else if (lowerKey == "runningresistancefactor")
                                {
                                    var val = reader.l();
                                    applyActions.Add(v =>
                                    {
                                        v.g().c(val[2]);
                                        v.g().a(val[1] * 3.6);
                                        v.g().d(val[0] * 12.96);
                                    });
                                }
                                else if (lowerKey == "capacity")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.a().b(val));
                                }

                                break;

                            case "[passengers]":
                                if (lowerKey == "capacity")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.a().b(val));
                                }
                                else if (lowerKey == "bodyweight")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.a().c(val));
                                }
                                else if (lowerKey == "boardingspeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.a().a(val));
                                }
                                else if (lowerKey == "alightingspeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.a().d(val));
                                }

                                break;

                            case "[brake]":
                                if (lowerKey == "pistonarea")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().l().b().b(val);
                                        v.f().b().m().b().b(val);
                                        v.f().b().g().a(val);
                                    });
                                }
                                else if (lowerKey == "shoefriction")
                                {
                                    var val = reader.l();
                                    applyActions.Add(v =>
                                    {
                                        v.g().j().e(val[2] * 3.6);
                                        v.g().d().e(val[2] * 3.6);
                                        v.g().j().f(val[1] * 3.6);
                                        v.g().d().f(val[1] * 3.6);
                                        v.g().j().c(val[0]);
                                        v.g().d().c(val[0]);
                                    });
                                }

                                break;

                            case "[er]":
                                if (lowerKey == "applyspeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().c().b().g(val));
                                }
                                else if (lowerKey == "releasespeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().c().b().j(val));
                                }
                                else if (lowerKey == "volumeratio")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().c().b().i(1.0 / val));
                                }
                                else if (lowerKey == "rapidreleasespeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().c().b().a(val));
                                }

                                break;

                            case "[bp]":
                                if (lowerKey == "applyspeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().h().c().g(val);
                                        v.f().b().c().a().g(val);
                                    });
                                }
                                else if (lowerKey == "releasespeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().h().c().j(val);
                                        v.f().b().c().a().j(val);
                                    });
                                }
                                else if (lowerKey == "volumeratio")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().h().c().i(1.0 / val);
                                        v.f().b().c().a().i(1.0 / val);
                                    });
                                }
                                else if (lowerKey == "rapidreleasespeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().h().c().a(val);
                                        v.f().b().c().a().a(val);
                                    });
                                }

                                break;

                            case "[door]":
                                if (lowerKey == "closetime")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.b().b((int)(1000.0 * val)));
                                }

                                break;

                            case "[brakereadhesion]":
                                if (lowerKey == "bcreleasespeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().l().d().a(val);
                                        v.f().b().m().d().a(val);
                                    });
                                }
                                else if (lowerKey == "bcapplyspeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().l().d().b(val);
                                        v.f().b().m().d().b(val);
                                    });
                                }
                                else if (lowerKey == "balancedeceleration")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().l().c().b(val / 3.6);
                                        v.f().b().m().c().b(val / 3.6);
                                    });
                                }
                                else if (lowerKey == "slipdeceleration")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().l().c().a(val / 3.6);
                                        v.f().b().m().c().a(val / 3.6);
                                    });
                                }
                                else if (lowerKey == "slipvelocity")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().l().c().e(val / 3.6);
                                        v.f().b().m().c().e(val / 3.6);
                                    });
                                }
                                else if (lowerKey == "referencedeceleration")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().l().c().d(val / 3.6);
                                        v.f().b().m().c().d(val / 3.6);
                                    });
                                }
                                else if (lowerKey == "holdingtime")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().l().c().f(val);
                                        v.f().b().m().c().f(val);
                                    });
                                }

                                break;

                            case "[compressor]":
                                if (lowerKey == "lowerpressure")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().e().c(val));
                                }
                                else if (lowerKey == "upperpressure")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().e().d(val));
                                }
                                else if (lowerKey == "compressionspeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().e().a(val));
                                }
                                else if (lowerKey == "leakspeed")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().e().b(val));
                                }

                                break;

                            case "[powerreadhesion]":
                                if (lowerKey == "slipacceleration")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().a().l().g(val / 3.6);
                                        v.f().a().l().a(val / 3.6);
                                    });
                                }
                                else if (lowerKey == "currentdecrease")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().a().a(val));
                                }
                                else if (lowerKey == "currentincrease")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().a().b(val));
                                }
                                else if (lowerKey == "slipvelocity")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().l().e(val / 3.6));
                                }
                                else if (lowerKey == "balanceacceleration")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().l().b(val / 3.6));
                                }
                                else if (lowerKey == "referencedeceleration")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().l().d(val / 3.6));
                                }
                                else if (lowerKey == "referenceacceleration")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().l().d(val / 3.6));
                                }
                                else if (lowerKey == "holdingtime")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().l().f(val));
                                }

                                break;

                            case "[airsupplement]":
                            case "[lockoutvalve]":
                                if (lowerKey == "maximumpressure")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().g().f(val);
                                        v.f().b().d().f(val);
                                    });
                                }
                                else if (lowerKey == "shoefriction")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().g().b(val));
                                }
                                else if (lowerKey == "initialpressure")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().g().e(val);
                                        v.f().b().d().e(val);
                                    });
                                }
                                else if (lowerKey == "regenerationlimit")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().b(val / 3.6));
                                }

                                break;

                            case "[ecb]":
                            case "[smee]":
                            case "[cl]":
                                if (lowerKey == "maximumpressure")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().f().b(val);
                                        v.f().b().h().b(val);
                                        v.f().b().c().b(val);
                                    });
                                }
                                else if (lowerKey == "pressurerates")
                                {
                                    var val = reader.l();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().f().a(val);
                                        v.f().b().h().a(val);
                                        v.f().b().c().a(val);
                                    });
                                }
                                else if (lowerKey == "sapbcratio")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().g().g(val);
                                        v.f().b().d().g(val);
                                    });
                                }
                                else if (lowerKey == "sapbcoffset")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().g().i(val);
                                        v.f().b().d().i(val);
                                    });
                                }
                                else if (lowerKey == "bpinitialpressure")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v =>
                                    {
                                        v.f().b().h().a(val);
                                        v.f().b().c().a(val);
                                    });
                                }
                                else if (lowerKey == "leverdelay")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().b().a().a(val));
                                }

                                break;

                            case "[linebreaker]":
                            case "[maincircuit]":
                                if (lowerKey == "regenerationlimit")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().k().a(Math.Max(val / 3.6, 0.01)));
                                }
                                else if (lowerKey == "slipvelocitycoefficient")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().a(val / 3.6));
                                }
                                else if (lowerKey == "regenerationstartlimit")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().k().b(val / 3.6));
                                }
                                else if (lowerKey == "notchdelay" || lowerKey == "powernotchdelay" ||
                                         lowerKey == "leverdelay")
                                {
                                    var val = reader.f();
                                    applyActions.Add(v => v.f().a().d().a(val));
                                }

                                break;

                            case "":
                                if (lowerKey == "loadcompensating")
                                {
                                    var val = reader.e().ToLower() == "true";
                                    applyActions.Add(v => v.f().d().a(val));
                                }
                                else if (lowerKey == "firstcar")
                                {
                                    var flag = reader.e().ToLower() != "t";
                                    applyActions.Add(v =>
                                    {
                                        v.g().a(flag ? v.g().j() : v.g().d());
                                        v.f().b().a(flag ? v.f().b().l() : v.f().b().m());
                                    });
                                }

                                break;

                            case "[bcservo]":
                            case "[bc]":
                                handled = false;
                                break;
                        }

                        if (!handled || lowerSection == "[bcservo]" || lowerSection == "[bc]")
                        {
                            if (lowerKey == "releasestop")
                            {
                                var val = reader.f();
                                applyActions.Add(v =>
                                {
                                    v.f().b().m().d().c(val);
                                    v.f().b().l().d().c(val);
                                });
                            }
                            else if (lowerKey == "applystart")
                            {
                                var val = reader.f();
                                applyActions.Add(v =>
                                {
                                    v.f().b().m().d().f(val);
                                    v.f().b().l().d().f(val);
                                });
                            }
                            else if (lowerKey == "releasestart")
                            {
                                var val = reader.f();
                                applyActions.Add(v =>
                                {
                                    v.f().b().m().d().e(val);
                                    v.f().b().l().d().e(val);
                                });
                            }
                            else if (lowerKey == "pistonarea")
                            {
                                var val = reader.f();
                                applyActions.Add(v =>
                                {
                                    v.f().b().l().b().b(val);
                                    v.f().b().m().b().b(val);
                                    v.f().b().g().a(val);
                                });
                            }
                            else if (lowerKey == "volumeratio")
                            {
                                var val = reader.f();
                                baseVolumeRatio = val;
                            }
                            else if (lowerKey == "applyspeed")
                            {
                                var val = reader.f();
                                applyActions.Add(v =>
                                {
                                    v.f().b().m().d().g(val);
                                    v.f().b().l().d().g(val);
                                });
                            }
                            else if (lowerKey == "applystop")
                            {
                                var val = reader.f();
                                applyActions.Add(v =>
                                {
                                    v.f().b().m().d().d(val);
                                    v.f().b().l().d().d(val);
                                });
                            }
                            else if (lowerKey == "releasespeed")
                            {
                                var val = reader.f();
                                applyActions.Add(v =>
                                {
                                    v.f().b().m().d().j(val);
                                    v.f().b().l().d().j(val);
                                });
                            }
                        }
                    }
            }

            var mrTank = vehicleSrc.f().b().e().g();
            var currentMrPressure = mrTank.aj();
            foreach (var action in applyActions) action(vehicleSrc);

            var dynamics = vehicle.Dynamics;
            if (motorCarCount.HasValue) dynamics.MotorCar.Count = motorCarCount.Value;

            if (trailerCarCount.HasValue) dynamics.TrailerCar.Count = trailerCarCount.Value;

            var newUpperPressure = vehicleSrc.f().b().e().d();
            mrTank.ak(Math.Min(currentMrPressure, newUpperPressure));
            var totalCarCount = (int)(vehicleSrc.g().j().f() + vehicleSrc.g().d().f());
            CarCountHelper.SetCarCount(vehicle, totalCarCount);
            if (totalCarCount > 0)
            {
                var motorRatio = vehicleSrc.g().j().f() / totalCarCount;
                // airSupplement
                vehicleSrc.f().b().g().c(motorRatio);
                var baseVol = 1.0 / Math.Max(baseVolumeRatio, 0.0001);
                var azTrailer = vehicleSrc.f().b().m().d();
                azTrailer.i(baseVol * (1.0 - motorRatio));
                var azMotor = vehicleSrc.f().b().l().d();
                azMotor.i(baseVol * motorRatio);
            }

            // 重置力行与制动手柄的延迟环形缓冲区指针防止越界
            vehicleSrc.f().a().d().n();
            vehicleSrc.f().b().a().n();
            // 更新载重
            var passenger = vehicle.Passenger;
            var passengerLoad = passenger.Load;
            // 先写 0 再写实际值：强制触发载重事件链（bs.ak 同值会短路，导致 bd.y 等按辆数派生的量残留旧值）
            passengerLoad.Value = 0;
            passengerLoad.Value = passenger.Count * passenger.BodyWeight;
            dynamics.Setup();
            if (vehicleSrc.f().a().k().c() < 0.0) vehicleSrc.f().a().k().b(vehicleSrc.f().a().k().b() + 25.0 / 18.0);
        }

        /// <summary>
        ///     重置各参数为默认值防止残留
        /// </summary>
        private static void ResetAllParametersToDefaults(f vehicleSrc)
        {
            // 应空载重与首车编组
            vehicleSrc.f().d().a(true); // LoadCompensating 默认为 true
            vehicleSrc.g().a(vehicleSrc.g().d()); // FirstCar 默认 T车
            vehicleSrc.f().b().a(vehicleSrc.f().b().m()); // FirstCar 默认 T车制动

            // 重置走行阻力与曲线阻力自动计算标志
            var dynamicsSrc = vehicleSrc.g();
            if (dynamicsSrc != null)
            {
                B2FieldM?.SetValue(dynamicsSrc, true);
                B2FieldQ?.SetValue(dynamicsSrc, true);
            }

            vehicleSrc.f().b().a(vehicleSrc.f().b().f());
            vehicleSrc.f().b().a(vehicleSrc.f().b().g());
            var defaultRates = new[] { 0.0, 0.133, 0.267, 0.4, 0.533, 0.667, 0.8, 0.9, 1.0, 1.0 };
            var smee = vehicleSrc.f().b().h();
            if (smee != null)
            {
                smee.b(440000.0);
                smee.a(defaultRates);
            }

            var cl = vehicleSrc.f().b().c();
            if (cl != null)
            {
                cl.b(440000.0);
                cl.a(defaultRates);
            }

            var ecb = vehicleSrc.f().b().f();
            if (ecb != null)
            {
                ecb.b(440000.0);
                ecb.a(defaultRates);
            }

            vehicleSrc.f().b().a().a(0.5);

            // 重置 AirSupplement (c5)
            var airSupp = vehicleSrc.f().b().g();
            if (airSupp != null)
            {
                airSupp.b(0.18); // ShoeFriction 0.18
                airSupp.a(1.0); // PistonArea 1.0
                airSupp.e(40000.0); // InitialPressure 40kPa
                airSupp.f(440000.0); // MaximumPressure 440kPa
                airSupp.g(1.0); // SapBcRatio 1.0
                airSupp.i(0.0); // SapBcOffset 0.0
            }

            // 重置 LockoutValve (dg)
            var lockout = vehicleSrc.f().b().d();
            if (lockout != null)
            {
                lockout.e(40000.0);
                lockout.f(440000.0);
                lockout.g(1.0);
                lockout.i(0.0);
            }

            // 重置 SAP, ER, BP 各阀门的进排气与容积 (VolumeRatio 默认 20，取倒数 1/20)
            const double vol20 = 1.0 / 20.0;

            // SAP
            vehicleSrc.f().b().h().a().g(500.0); // ApplySpeed
            vehicleSrc.f().b().h().a().j(500.0); // ReleaseSpeed
            vehicleSrc.f().b().h().a().i(vol20); // VolumeRatio

            // ER
            vehicleSrc.f().b().c().b().g(500.0);
            vehicleSrc.f().b().c().b().j(500.0);
            vehicleSrc.f().b().c().b().a(1000.0); // RapidReleaseSpeed
            vehicleSrc.f().b().c().b().i(vol20);

            // BP
            vehicleSrc.f().b().h().c().g(500.0);
            vehicleSrc.f().b().c().a().g(500.0);
            vehicleSrc.f().b().h().c().j(500.0);
            vehicleSrc.f().b().c().a().j(500.0);
            vehicleSrc.f().b().h().c().a(1000.0);
            vehicleSrc.f().b().c().a().a(1000.0);
            vehicleSrc.f().b().h().c().i(vol20);
            vehicleSrc.f().b().c().a().i(vol20);

            // BP 初始压力 (ce.c / dv.c 初值 490000)
            vehicleSrc.f().b().h().a(490000.0);
            vehicleSrc.f().b().c().a(490000.0);

            // 重置 Compressor (e7)
            var compressor = vehicleSrc.f().b().e();
            if (compressor != null)
            {
                compressor.c(700000.0); // LowerPressure 700kPa
                compressor.d(800000.0); // UpperPressure 800kPa
                compressor.a(5000.0); // CompressionSpeed 5000 Pa/s
                compressor.b(100.0); // LeakSpeed 100 Pa/s
            }

            // 重置动拖车 CarInfo (bd) 与 BcServo (az) / ReAdhesion
            vehicleSrc.f().b().l().b().b(0.4); // 动车 PistonArea
            vehicleSrc.f().b().m().b().b(0.4); // 拖车 PistonArea

            // 还原动车
            var motorBd = vehicleSrc.g().j();
            var motorBrake = vehicleSrc.f().b().l();
            if (motorBd != null && motorBrake != null)
            {
                motorBd.g(31500.0); // Weight 31500kg
                motorBd.a(5); // Count 5辆
                motorBd.c(0.26); // ShoeFrictionA
                motorBd.f(0.036); // ShoeFrictionB
                motorBd.e(0.09); // ShoeFrictionC
                motorBd.h(0.1); // MotorcarInertiaFactor 0.1

                motorBrake.d().a(true); // BcServo 默认开启 (az.f 初值 true)
                motorBrake.c().a(false); // BrakeReAdhesion 默认关闭 (d8.i 初值 false)

                var az = motorBrake.d();
                az.c(0.0); // ReleaseStop (az.d 初值 0)
                az.e(20000.0); // ReleaseStart (az.b 初值 20000)
                az.d(0.0); // ApplyStop (az.e 初值 0)
                az.f(20000.0); // ApplyStart (az.c 初值 20000)
                az.g(500.0); // ApplySpeed (fr.c 初值 500)
                az.j(500.0); // ReleaseSpeed (fr.d 初值 500)
                az.a(0.0); // RapidReleaseSpeed (az.g 初值 0)
                az.b(0.0); // RapidApplySpeed (az.h 初值 0)

                var readhesion = motorBrake.c();
                readhesion.e(25.0 / 9.0); // SlipVelocity (d6 覆盖 25/9)
                readhesion.a(50.0 / 9.0); // c 字段 (d6 覆盖 50/9)
                readhesion.g(2500.0 / 9.0); // SlipDeceleration (d6 覆盖 2500/9)
                readhesion.b(-5.0 / 9.0); // BalanceDeceleration (未覆盖 -5/9)
                readhesion.d(25.0 / 18.0); // ReferenceDeceleration (未覆盖 25/18)
                readhesion.c(2500.0 / 9.0); // g 字段 (d6 覆盖 2500/9)
                readhesion.f(0.0); // HoldingTime (d6 覆盖 0)
            }

            // 还原拖车
            var trailerBd = vehicleSrc.g().d();
            var trailerBrake = vehicleSrc.f().b().m();
            if (trailerBd != null && trailerBrake != null)
            {
                trailerBd.g(31500.0);
                trailerBd.a(5);
                trailerBd.c(0.26);
                trailerBd.f(0.036);
                trailerBd.e(0.09);
                trailerBd.h(0.05); // TrailerInertiaFactor 0.05

                trailerBrake.d().a(true); // BcServo 默认开启 (az.f 初值 true)
                trailerBrake.c().a(false); // BrakeReAdhesion 默认关闭 (d8.i 初值 false)

                var az = trailerBrake.d();
                az.c(0.0); // ReleaseStop (az.d 初值 0)
                az.e(20000.0); // ReleaseStart (az.b 初值 20000)
                az.d(0.0); // ApplyStop (az.e 初值 0)
                az.f(20000.0); // ApplyStart (az.c 初值 20000)
                az.g(500.0); // ApplySpeed (fr.c 初值 500)
                az.j(500.0); // ReleaseSpeed (fr.d 初值 500)
                az.a(0.0); // RapidReleaseSpeed (az.g 初值 0)
                az.b(0.0); // RapidApplySpeed (az.h 初值 0)

                var readhesion = trailerBrake.c();
                readhesion.e(25.0 / 9.0); // SlipVelocity (d6 覆盖 25/9)
                readhesion.a(50.0 / 9.0); // c 字段 (d6 覆盖 50/9)
                readhesion.g(2500.0 / 9.0); // SlipDeceleration (d6 覆盖 2500/9)
                readhesion.b(-5.0 / 9.0); // BalanceDeceleration (未覆盖 -5/9)
                readhesion.d(25.0 / 18.0); // ReferenceDeceleration (未覆盖 25/18)
                readhesion.c(2500.0 / 9.0); // g 字段 (d6 覆盖 2500/9)
                readhesion.f(0.0); // HoldingTime (d6 覆盖 0)
            }

            // 还原全车长度与乘客
            vehicleSrc.g().e(20.0); // CarLength 20m
            vehicleSrc.a().b(150.0); // Capacity 150
            vehicleSrc.a().c(65.0); // BodyWeight 65kg
            vehicleSrc.a().a(3.15); // BoardingSpeed 3.15
            vehicleSrc.a().d(6.3); // AlightingSpeed 6.3
            // 重置牵引系统、主电路与 PowerReAdhesion
            vehicleSrc.f().a().l().a(false); // 关闭 PowerReAdhesion
            vehicleSrc.f().a().a().a(0.0); // CurrentDecrease 0.0
            vehicleSrc.f().a().a().b(0.0); // CurrentIncrease 0.0
            vehicleSrc.f().a().k().a(5.0 / 3.6); // RegenerationLimit 5km/h
            vehicleSrc.f().a().b(5.0 / 3.6);
            vehicleSrc.f().a().k().b(25.0 / 9.0); // RegenerationStartLimit (at.f 初值 10km/h)
            vehicleSrc.f().a().a(0.0); // SlipVelocityCoefficient 0.0
            vehicleSrc.f().a().d().a(0.5); // LeverDelay 0.5s

            var powerAdhesion = vehicleSrc.f().a().l();
            powerAdhesion.e(5.0 / 9.0); // SlipVelocity (d8 初值 5/9)
            powerAdhesion.g(25.0 / 9.0); // SlipDeceleration (d8 初值 25/9)
            powerAdhesion.a(25.0 / 9.0); // c 字段 (d8 初值 25/9)
            powerAdhesion.b(-5.0 / 9.0); // BalanceDeceleration (d8 初值 -5/9)
            powerAdhesion.d(25.0 / 18.0); // ReferenceDeceleration (d8 初值 25/18)
            powerAdhesion.c(10.0 / 9.0); // g 字段 (d8 初值 10/9)
            powerAdhesion.f(2.0); // HoldingTime (d8.e 初值 2.0)

            // 重置定速控制档位
            var csc = vehicleSrc.f().c();
            if (csc != null)
            {
                csc.a(0); // Power
                csc.b(0); // Brake
                csc.c(0); // Neutral
            }

            // 重置车门
            vehicleSrc.b().b(5000);
        }
    }
}