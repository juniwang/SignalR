using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Crank
{
    class Latency
    {
        private static readonly string CategoryName = "SignalRLatency";
        private static readonly string CategoryHelp = "A category for SignalR latency";

        private static PerformanceCounter sendLatencyCounter;
        private static readonly string SendLatencyCounterName = "Send Latency MS";

        private static PerformanceCounter requestlatencyCounter;
        private static readonly string RequestLatencyCounterName = "Request Latency MS";

        static Latency()
        {
            SetupCategory();
            CreateCounters();
        }

        public static void UpdateLatency(string ticks)
        {
            try
            {
                long now = DateTime.UtcNow.Ticks;
                //Console.WriteLine("N" + now.ToString() + "|" + ticks);
                long clientTicks = long.Parse(ticks.Substring(21, 18));
                long serverTicks = long.Parse(ticks.Substring(1, 18));

                sendLatencyCounter.RawValue = (serverTicks - clientTicks) / 10000; // clock might be unsynchornized.
                requestlatencyCounter.RawValue = (now - clientTicks) / 10000;
            }
            catch
            {
                // ignore all exceptions
            }
        }

        private static void SetupCategory()
        {
            if (!PerformanceCounterCategory.Exists(CategoryName))
            {
                CounterCreationDataCollection counterCreationDataCollection = new CounterCreationDataCollection();

                CounterCreationData sendLatency = new CounterCreationData();
                sendLatency.CounterType = PerformanceCounterType.NumberOfItems64;
                sendLatency.CounterName = SendLatencyCounterName;

                CounterCreationData requestLatency = new CounterCreationData();
                requestLatency.CounterType = PerformanceCounterType.NumberOfItems64;
                requestLatency.CounterName = RequestLatencyCounterName;

                counterCreationDataCollection.Add(sendLatency);
                counterCreationDataCollection.Add(requestLatency);
                PerformanceCounterCategory.Create(CategoryName, CategoryHelp, PerformanceCounterCategoryType.MultiInstance, counterCreationDataCollection);
            }
            else
                Console.WriteLine("Category {0} exists", CategoryName);
        }

        private static void CreateCounters()
        {
            sendLatencyCounter = new PerformanceCounter(CategoryName, SendLatencyCounterName, "localhost", false);
            sendLatencyCounter.RawValue = 0;

            requestlatencyCounter = new PerformanceCounter(CategoryName, RequestLatencyCounterName, "localhost", false);
            requestlatencyCounter.RawValue = 0;
        }
    }
}
