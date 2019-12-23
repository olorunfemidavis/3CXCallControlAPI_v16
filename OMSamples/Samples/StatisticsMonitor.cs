using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TCX.Configuration;

namespace OMSamples.Samples
{
    class StatListener : PsTypeEventListener<Statistics>
    {
        string StatisticsClass;
        string StatisticsInfo(Statistics s)
        {
            return $"{StatisticsClass}.{s?.GetName()}.{s?.ID}: {string.Join("\r\n", s.Content.Select(x=>$"{x.Key}={x.Value}"))}";
        }

        public StatListener(string statClass)
            : base(statClass)
        {
            StatisticsClass = statClass;
            PhoneSystem.Root.InitializeStatistics(StatisticsClass);
            SetTypeHandler(
                (x) => Console.WriteLine($"UPDATED {StatisticsInfo(x)}"),
                (x) => Console.WriteLine($"INSERTED {StatisticsInfo(x)}"),
                (x) => Console.WriteLine($"DELETED {StatisticsInfo(x)}"),
                null, null);
        }
        public override void Dispose()
        {
            PhoneSystem.Root.DeinitializeStatistics(StatisticsClass);
            base.Dispose();
        }
    }

    [SampleCode("statmonitor")]
    [SampleParam("arg1..agrN", "Statistics ")]
    [SampleDescription("Shows notificatins for specific statistics object.")]
    class StatisticsMonitorSample : ISample
    {
        public void Run(params string[] args)
        {
            using (var listeners = args.Skip(1).Select(x =>
                {
                    //print statistics content
                    Console.WriteLine($"{x}={{\n{string.Join("\n    ", PhoneSystem.Root.InitializeStatistics(x).Select(y => $"{y.GetName()}.{y.ID}"))}\n}}");
                    return new StatListener(x);
                }
                ).ToArray().GetDisposer())
            {
                while (!Program.Stop)
                {
                    Thread.Sleep(5000);
                }
            }
        }
    }
}