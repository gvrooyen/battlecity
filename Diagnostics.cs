using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace battlecity
{
    class Diagnostics
    {
        public static class Sync
        {
            public static int dupedTicks = 0;
            public static int missedTicks = 0;
            public static double avgTickPeriod = 0.0;

            private static int ticksCounted = 0;
            public static void addTickPeriod(double tickPeriod)
            {
                avgTickPeriod = (avgTickPeriod * ticksCounted + tickPeriod) / (ticksCounted + 1);
                ticksCounted += 1;
            }

        }
    }
}
