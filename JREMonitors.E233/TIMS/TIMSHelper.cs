using System;
using System.Text.RegularExpressions;
using JREMonitors.Core.Utils;

namespace JREMonitors.E233.TIMS
{
    public static class TIMSHelper
    {
        public static string GetHoursAndMinutes(TimeSpan? prevTime, TimeSpan? time)
        {
            if (!time.HasValue) return "";
            string hours;
            if (!prevTime.HasValue || prevTime.Value.Hours != time.Value.Hours)
                hours = time.Value.Hours.ToString().PadLeft(2).ToFullWidth() + ':';
            else
                hours = "\u3000\u3000 ";
            var minutes = time.Value.Minutes.ToString().PadLeft(2, '0').ToFullWidth();
            return hours + minutes;
        }

        public static string GetSeconds(TimeSpan? time, bool fullWidth, bool hideWhenZero)
        {
            string seconds;
            if (!time.HasValue || time.Value.Seconds == 0)
                seconds = hideWhenZero ? "  " : "00";
            else
                seconds = time.Value.Seconds.ToString().PadLeft(2, '0');

            if (fullWidth) seconds = seconds.ToFullWidth();

            return seconds;
        }

        public static string ParseHorizontalStationName(string rawStationName)
        {
            if (string.IsNullOrWhiteSpace(rawStationName)) return "";
            var stationName = rawStationName;
            if (rawStationName.Length == 1)
                stationName = $" {rawStationName} ";
            else if (rawStationName.Length == 2) stationName = $"{rawStationName[0]} {rawStationName[1]}";

            return stationName.ToFullWidth();
        }

        public static string ParseVerticalStationName(string rawStationName)
        {
            if (string.IsNullOrWhiteSpace(rawStationName)) return "";
            var stationName = rawStationName.Replace("\n", "");
            if (stationName.Length > 6) stationName = stationName.Substring(0, 6);

            switch (stationName.Length)
            {
                case 1:
                    stationName = $"\u3000{rawStationName}";
                    break;
                case 2:
                    stationName = $"{rawStationName[0]}\u3000{rawStationName[1]}";
                    break;
                case 4:
                    stationName = $"{rawStationName[0]}{rawStationName[1]}\n{rawStationName[2]}{rawStationName[3]}";
                    break;
                case 5:
                    stationName =
                        $"{rawStationName.Substring(0, 3)}\n{rawStationName.Substring(3, 2)}";
                    break;
                case 6:
                    stationName =
                        $"{rawStationName.Substring(0, 3)}\n{rawStationName.Substring(3, 3)}";
                    break;
            }

            return stationName.ToFullWidth();
        }

        public static string FormatRadioChannel(string rawRadioChannel)
        {
            return (rawRadioChannel ?? string.Empty).PadLeft(2);
        }

        public static int GetNotchValue(int power, int brake)
        {
            if (brake > 0) return -brake;
            if (power > 0) return power;
            return 0;
        }

        public static int GetImpreciseValue(float value, int precision = 1)
        {
            if (precision == 0)
                throw new ArgumentOutOfRangeException(nameof(precision));
            var scaled = (double)value / precision;
            var rounded = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
            return rounded * precision;
        }

        public static string GetNotchText(int notch, int tascBrake)
        {
            var tascBrakeText = string.Empty;
            var tascMainBrake = 0;
            if (tascBrake > 0)
            {
                int tascSubBrake;
                if (tascBrake <= 3)
                {
                    tascMainBrake = 1;
                    tascSubBrake = tascBrake;
                }
                else
                {
                    var adjusted = tascBrake - 3;
                    tascMainBrake = (adjusted - 1) / 4 + 2;
                    tascSubBrake = (adjusted - 1) % 4;
                }

                tascBrakeText = "B" + tascMainBrake + "-" + tascSubBrake;
            }

            string handleBrakeText;
            if (notch < 0)
                handleBrakeText = notch >= -8 ? "B" + -notch : "EB";
            else if (notch > 0)
                handleBrakeText = "P" + notch;
            else
                handleBrakeText = string.Empty;

            if (tascBrake > 0)
                if (notch >= 0 || tascMainBrake >= -notch)
                    return tascBrakeText;

            return handleBrakeText;
        }

        public static bool IsBlink(long tickCount)
        {
            return tickCount % 2 == 0;
        }

        public static string FormatTrainNumber(string input, char? numberpadChar, bool padType)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var match = Regex.Match(input, @"^(?<prefix>\D*)(?<number>\d+)(?<suffix>.*)$");
            if (!match.Success) return input;
            var prefix = match.Groups["prefix"].Value;
            if (padType) prefix = prefix.PadLeft(2, ' ');

            var rawNumber = match.Groups["number"].Value;
            var number = numberpadChar.HasValue ? rawNumber.PadLeft(4, numberpadChar.Value) : rawNumber;
            var rawSuffix = match.Groups["suffix"].Value;
            var finalSuffix = " ";
            if (!string.IsNullOrEmpty(rawSuffix) && StringHelper.IsAsciiLetter(rawSuffix[0]))
                finalSuffix = rawSuffix[0].ToString();

            return prefix + number + finalSuffix;
        }
    }
}