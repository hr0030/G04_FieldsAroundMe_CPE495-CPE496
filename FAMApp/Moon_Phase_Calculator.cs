using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Code taken from https://github.com/signetica/MoonPhase/blob/master/MoonPhase.cpp
public class MoonPhaseData
    {
        public double JulianDate { get; set; }
        public double Phase { get; set; }          // normalized [0-1]
        public double Age { get; set; }            // days since new moon
        public double Illumination { get; set; }   // 0 to 1
        public double Distance { get; set; }       // in Earth radii
        public double Latitude { get; set; }       // ecliptic latitude
        public double Longitude { get; set; }      // ecliptic longitude
        public string PhaseName { get; set; }
        public string ZodiacName { get; set; }
    }

    public static class Moon_Phase_Calculator
    {
        // Constants from the C++ code
        private const double MOON_SYNODIC_PERIOD = 29.53058867;
        private const double MOON_SYNODIC_OFFSET = 2451550.1;     // Jan 6, 2000 18:14 UTC

        private const double MOON_DISTANCE_PERIOD = 27.55454988;
        private const double MOON_DISTANCE_OFFSET = 2451562.2;

        private const double MOON_LATITUDE_PERIOD = 27.212220817;
        private const double MOON_LATITUDE_OFFSET = 2451565.2;

        private const double MOON_LONGITUDE_PERIOD = 27.321582241;
        private const double MOON_LONGITUDE_OFFSET = 2451555.8;

        private static readonly string[] PhaseNames = {
        "New", "Evening Crescent", "First Quarter", "Waxing Gibbous",
        "Full", "Waning Gibbous", "Last Quarter", "Morning Crescent"
    };

        private static readonly string[] ZodiacNames = {
        "Pisces", "Aries", "Taurus", "Gemini", "Cancer",
        "Leo", "Virgo", "Libra", "Scorpio", "Sagittarius",
        "Capricorn", "Aquarius"
    };

        private static readonly double[] ZodiacAngles = {
        33.18, 51.16, 93.44, 119.48, 135.30, 173.34,
        224.17, 242.57, 271.26, 302.49, 311.72, 348.58
    };

        public static MoonPhaseData Calculate(DateTime dateTime)
        {
            var jd = ToJulianDate(dateTime.ToUniversalTime());
            double phase = Normalize((jd - MOON_SYNODIC_OFFSET) / MOON_SYNODIC_PERIOD);
            double age = phase * MOON_SYNODIC_PERIOD;
            double illumination = (1.0 - Math.Cos(2 * Math.PI * phase)) / 2.0;

            string phaseName = PhaseNames[(int)(phase * 8 + 0.5) % 8];

            // Distance calculation
            double distancePhase = Normalize((jd - MOON_DISTANCE_OFFSET) / MOON_DISTANCE_PERIOD);
            double distance = 60.4
                              - 3.3 * Math.Cos(2 * Math.PI * distancePhase)
                              - 0.6 * Math.Cos(4 * Math.PI * phase - 2 * Math.PI * distancePhase)
                              - 0.5 * Math.Cos(4 * Math.PI * phase);

            // Latitude calculation
            double latPhase = Normalize((jd - MOON_LATITUDE_OFFSET) / MOON_LATITUDE_PERIOD);
            double latitude = 5.1 * Math.Sin(2 * Math.PI * latPhase);

            // Longitude and Zodiac sign
            double longPhase = Normalize((jd - MOON_LONGITUDE_OFFSET) / MOON_LONGITUDE_PERIOD);
            double longitude = 360 * longPhase
                              + 6.3 * Math.Sin(2 * Math.PI * distancePhase)
                              + 1.3 * Math.Sin(4 * Math.PI * phase - 2 * Math.PI * distancePhase)
                              + 0.7 * Math.Sin(4 * Math.PI * phase);

            if (longitude > 360) longitude -= 360;

            string zodiac = "Pisces";  // Default
            for (int i = 0; i < ZodiacAngles.Length; i++)
            {
                if (longitude < ZodiacAngles[i])
                {
                    zodiac = ZodiacNames[i];
                    break;
                }
            }

            return new MoonPhaseData
            {
                JulianDate = jd,
                Phase = phase,
                Age = age,
                Illumination = illumination,
                Distance = distance,
                Latitude = latitude,
                Longitude = longitude,
                PhaseName = phaseName,
                ZodiacName = zodiac
            };
        }

        private static double ToJulianDate(DateTime utc)
        {
            return utc.Subtract(new DateTime(1970, 1, 1)).TotalSeconds / 86400.0 + 2440587.5;
        }

        private static double Normalize(double x)
        {
            x %= 1.0;
            return x < 0 ? x + 1.0 : x;
        }
    }
