using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using System.Threading;

namespace OMSamples.Samples
{
    [SampleCode("update_stat")]
    [SampleDescription("This sample creates and continuously update Statistic object named 'MYSTAT'. After running this sample statistics 'MYSTAT' will be available for create_delete_stat sample")]
    class UpdateStatSample : ISample
    {
        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            String[] strs = { "stat_value_1", "stat_value_2" };
            Statistics myStat;
            myStat = ps.CreateStatistics("S_TEST");
            bool swap = false;
            String filter = null;
            if (args.Length > 1)
                filter = args[1];
            using (var listener = new StatListener("STATISTICS"))
            {
                int i = 0;
                while (true)
                {
                    if ((++i % 5) == 0)
                        myStat.clearall();
                    else
                    {
                        myStat["s1"] = strs[swap ? 1 : 0];
                        myStat["s2"] = strs[swap ? 0 : 1];
                    }
                    swap = !swap;
                    try
                    {
                        myStat.update();
                        System.Console.WriteLine("(" + i.ToString() + ") NewStat=" + myStat.ToString() + "\n------------");
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine("Exception catched" + e.ToString());
                    }
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
