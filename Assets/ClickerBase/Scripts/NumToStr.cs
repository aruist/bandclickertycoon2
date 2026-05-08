using UnityEngine;
using System.Collections;
using System;
using System.Globalization;

public class NumToStr {
    public static string annaAikaTickseista(long aika)
    {
        string aikaJono = "";
        TimeSpan timeSpan = TimeSpan.FromTicks(aika);
        if (timeSpan.TotalSeconds <= 0)
        {
            aikaJono = "0 s";
        }
        else if (timeSpan.TotalHours > 60)
        {
            aikaJono = "> 60 hours";
        }
        else
        {
            if (timeSpan.TotalSeconds < 60)
            {
                aikaJono = timeSpan.Seconds.ToString() + " s";
            }
            else if (timeSpan.TotalMinutes <= 60)
            {
                aikaJono = timeSpan.Minutes.ToString() + "m " + timeSpan.Seconds.ToString() + "s";
            }
            else
            {
                aikaJono = timeSpan.TotalHours.ToString() + "h " + timeSpan.Minutes.ToString() + "m";
            }
        }
        return aikaJono;
    }

    public static string GetTimeStr(float time)
    {
        if (time < 60f)
        {
            return time.ToString("F2", CultureInfo.InvariantCulture) + " sec";
        }
        float mins = time / 60f;
        if (mins < 60f)
            return mins.ToString("F2", CultureInfo.InvariantCulture) + " min";
        float hours = mins / 60f;
        return hours.ToString("F2", CultureInfo.InvariantCulture) + " hours";
    }

    public static string GetTimeStr(double time)
    {
        if (time < 60f)
        {
            return time.ToString("F2", CultureInfo.InvariantCulture) + " sec";
        }
        double mins = time / 60f;
        if (mins < 60f)
            return mins.ToString("F2", CultureInfo.InvariantCulture) + " min";
        double hours = mins / 60f;
        return hours.ToString("F2", CultureInfo.InvariantCulture) + " hours";
    }

    private static readonly string[] Suffixes =
    {
        "",     // 10^0
        "K",    // 10^3
        "M",    // 10^6
        "B",    // 10^9
        "T",    // 10^12

        "Qa",   // 10^15  Quadrillion
        "Qi",   // 10^18  Quintillion
        "Sx",   // 10^21  Sextillion
        "Sp",   // 10^24  Septillion
        "Oc",   // 10^27  Octillion
        "No",   // 10^30  Nonillion

        "Dc",   // 10^33  Decillion
        "Ud",   // 10^36  Undecillion
        "Dd",   // 10^39  Duodecillion
        "Td",   // 10^42  Tredecillion
        "Qad",  // 10^45  Quattuordecillion
        "Qid",  // 10^48  Quindecillion
        "Sxd",  // 10^51  Sexdecillion
        "Spd",  // 10^54  Septendecillion
        "Ocd",  // 10^57  Octodecillion
        "Nod",  // 10^60  Novemdecillion

        "Vg",   // 10^63  Vigintillion
        "Uvg",  // 10^66
        "Dvg",  // 10^69
        "Tvg",  // 10^72
        "Qavg", // 10^75
        "Qivg", // 10^78
        "Sxvg", // 10^81
        "Spvg", // 10^84
        "Ocvg", // 10^87
        "Novg", // 10^90

        "Tg",   // 10^93  Trigintillion
        "Utg",  // 10^96
        "Dtg",  // 10^99
        "Ttg",  // 10^102
        "Qatg", // 10^105
        "Qitg", // 10^108
        "Sxtg", // 10^111
        "Sptg", // 10^114
        "Octg", // 10^117
        "Notg", // 10^120
    };

    public static void GetNumStr(double num, out string suffix, out string numstr)
    {
        if (double.IsNaN(num))
        {
            suffix = "";
            numstr = "0";
            return;
        }
        if (double.IsInfinity(num))
        {
            numstr = "∞";
            suffix = "";
            return;
        }

        double abs = Math.Abs(num);
        suffix = "";

        if (abs < 1000.0)
        {
            numstr = num.ToString("F2", CultureInfo.InvariantCulture);
            return;
        }

        int suffixIndex = 0;

        while (abs >= 1000.0 && suffixIndex < Suffixes.Length - 1)
        {
            num /= 1000.0;
            abs /= 1000.0;
            suffixIndex++;
        }

        string format;
        double shownAbs = Math.Abs(num);

        if (shownAbs < 10.0)
            format = "F2";
        else if (shownAbs < 100.0)
            format = "F1";
        else
            format = "F0";
        numstr = num.ToString(format, CultureInfo.InvariantCulture);
        suffix = Suffixes[suffixIndex];
    }

    public static string GetNumStr(double num)
    {
        if (double.IsNaN(num))
            return "0";

        if (double.IsInfinity(num))
            return "∞";

        double abs = Math.Abs(num);

        if (abs < 1000.0)
            return num.ToString("F2", CultureInfo.InvariantCulture);
        int suffixIndex = 0;

        while (abs >= 1000.0 && suffixIndex < Suffixes.Length - 1)
        {
            num /= 1000.0;
            abs /= 1000.0;
            suffixIndex++;
        }

        string format;
        double shownAbs = Math.Abs(num);

        if (shownAbs < 10.0)
            format = "F2";
        else if (shownAbs < 100.0)
            format = "F1";
        else
            format = "F0";
        return num.ToString(format, CultureInfo.InvariantCulture) + Suffixes[suffixIndex];
    }

}
