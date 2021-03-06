﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// The latency between clocks on each server. Will be calculated automatically.
        /// </summary>
        static Dictionary<string, long> dicClockLatencies = new Dictionary<string, long>();

        static Latency()
        {
            SetupCategory();
            CreateCounters();
        }

        public static void SendRequestLatencyPC(long latencyMs)
        {
            requestlatencyCounter.RawValue = latencyMs;
        }

        public static void UpdateLatency(string ticks, long maxTimeoutMs = 60 * 1000)
        {
            try
            {
                long now = DateTime.UtcNow.Ticks;

                long clientTicks = 0;
                long serverTicks = 0;
                foreach (var tick in ticks.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    char start = tick[0];
                    switch (start)
                    {
                        case 'C': // the timestamp before sending
                            clientTicks = long.Parse(tick.Substring(1));
                            break;
                        case 'E': // the timestamp before SignalR server echo it back
                        case 'B': // the timestamp before SignalR server broadcast it.
                            serverTicks = long.Parse(tick.Substring(1));
                            break;
                        default:
                            break;
                    }
                }

                long requestLatency = now - clientTicks;
                //Console.WriteLine("N" + now.ToString() + "|" + ticks);
                //Console.WriteLine(":::" + clientTicks.ToString() + "; " + serverTicks.ToString());
                //Console.WriteLine("requestLatency=" + requestLatency.ToString());
                if (clientTicks > 0 && requestLatency > 0)
                {
                    Task.Run(() =>
                    {
                        requestlatencyCounter.RawValue = Math.Min((requestLatency) / 10000, maxTimeoutMs);
                        if (serverTicks > 0)
                        {
                            sendLatencyCounter.RawValue = Math.Min((serverTicks - clientTicks) / 10000, maxTimeoutMs); // clock might be unsynchornized.
                        }
                    });
                }
            }
            //catch(Exception e)
            catch
            {
                //Console.WriteLine(e.Message);
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
