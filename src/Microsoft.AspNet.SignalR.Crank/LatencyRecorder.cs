using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Crank
{
    class LatencyRecorder
    {
        static readonly int ROTATE = 20;

        static int _state = 0;
        static long[] totalTicks = new long[ROTATE];
        static int[] samples = new int[ROTATE];

        private static readonly HighFrequencyTimer _timerInstance = new HighFrequencyTimer(1,
                _ =>
                {
                    var current = Interlocked.CompareExchange(ref _state, (_state + 1) % ROTATE, _state);
                    if (samples[current] > 0)
                    {
                        long latencyMs = totalTicks[current] / samples[current] / 10000;
                        Console.WriteLine("Current:{0}, Samples: {1}, Latency:{2}", current.ToString(), samples[current].ToString(), latencyMs.ToString());
                        Latency.SendRequestLatencyPC(latencyMs);
                        totalTicks[current] = 0;
                        samples[current] = 0;
                    }
                }
            );

        static LatencyRecorder()
        {
            _timerInstance.Start();
        }

        public static void UpdateLatency(string ticks, long maxTimeoutMs = 60 * 1000)
        {
            try
            {
                long now = DateTime.UtcNow.Ticks;
                long clientTicks = 0;
                long serverTicks = 0;
                //Console.WriteLine("N" + now.ToString() + "|" + ticks);
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
                if (requestLatency > 0 && clientTicks > 0)
                {
                    var current = _state;
                    Interlocked.Add(ref totalTicks[current], requestLatency);
                    Interlocked.Increment(ref samples[current]);
                }
            }
            catch { }
        }
    }
}
