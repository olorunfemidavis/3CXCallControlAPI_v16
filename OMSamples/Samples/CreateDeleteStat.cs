using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using System.Threading;

namespace OMSamples.Samples
{
    [SampleCode("create_delete_stat")]
    [SampleDescription("This sample shows how to delete and create Statistics object. \nStatistics 'MYSTAT' should be initialized before runing this sample. \n(use update_stat sample)")]
    class CreateDeleteStatSample : ISample
    {
        public void Run(params string[] args)
        {
            Statistics myStat = PhoneSystem.Root.CreateStatistics("MYSTAT");
            Dictionary<string, string> original = myStat.Content;
            System.Console.WriteLine("Current:");
            foreach (KeyValuePair<string, string> kv in original)
            {
                System.Console.WriteLine(kv.Key + "=" + kv.Value);
            }
            System.Console.WriteLine(myStat.ToString());
            myStat.Delete();
            System.Console.WriteLine("After deleted:");
            Dictionary<string, string> a = myStat.Content;
            foreach (KeyValuePair<string, string> kv in a)
            {
                System.Console.WriteLine(kv.Key + "=" + kv.Value);
            }
            System.Console.WriteLine(myStat.ToString());
            Thread.Sleep(5000);
            myStat.Content = original;
            System.Console.WriteLine("After assigning content:");
            a = myStat.Content;
            foreach (KeyValuePair<string, string> kv in a)
            {
                System.Console.WriteLine(kv.Key + "=" + kv.Value);
            }
            System.Console.WriteLine(myStat.ToString());
            Thread.Sleep(5000);
            myStat.update();
            System.Console.WriteLine("After update:");
            a = myStat.Content;
            foreach (KeyValuePair<string, string> kv in a)
            {
                System.Console.WriteLine(kv.Key + "=" + kv.Value);
            }
            System.Console.WriteLine(myStat.ToString());
        }
    }
}