using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Win32ThreadPoolTimerApiSet;
using System.Runtime.InteropServices;

namespace MyTimer
{
    class Program
    {
        const uint _ticks_per_ms = 10_000;


        class TimerCtx
        {
            public List<long> ProcessingTimestampsPre;
            public List<long> ProcessingTimestampsPost;
            public uint Msperiod;
        }
        
        static int Main(string[] args)
        {
            uint msperiod_in = 1000;

            Console.WriteLine($"Timer Evaluation in C#\n" +
                              $" OS: {RuntimeInformation.OSDescription}\n" +
                              $" Framework: {RuntimeInformation.FrameworkDescription}");
            DateTime curDateTimeUtc = DateTime.Now;
            
            var timestampsPre = new List<long>();
            var timestampsPost = new List<long>();
            TimerCtx timerCtx = new TimerCtx          /* timer context */
            {
                    ProcessingTimestampsPre = timestampsPre,
                    ProcessingTimestampsPost = timestampsPost,
                    Msperiod = msperiod_in,
            };

            Console.WriteLine($"Timer starts with period (ms): {msperiod_in}\n" + "Press ESC-key to exit");
            ThreadPoolTimer TpTimer = ThreadPoolTimer.StartRelativeTimer(MyCallback, timerCtx, /*initial delay*/0, msperiod_in);
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;      /* wait for ESC-key */
            TpTimer.Dispose();
            Console.WriteLine("done");
            return 0;
        }

        static void MyCallback(object timerCtx)
        {
            ((TimerCtx)timerCtx).ProcessingTimestampsPre.Add(DateTime.UtcNow.Ticks);
            Console.WriteLine("do something");
            ((TimerCtx)timerCtx).ProcessingTimestampsPost.Add(DateTime.UtcNow.Ticks);
        }
    }
}
