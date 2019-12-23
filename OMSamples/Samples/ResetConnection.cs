using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("resetconnection")]
    class ResetConnection : ISample
    {


        public void Run(params string[] args)
        {
            for(int i=0; !Program.Stop; i++)
            {
                Console.WriteLine($"Iteration - {i}");
                var b = i;
                var root = PhoneSystem.Reset(PhoneSystem.ApplicationName, PhoneSystem.CfgServerHost, PhoneSystem.CfgServerPort, PhoneSystem.CfgServerUser, PhoneSystem.CfgServerPassword,
                    (x, y) => Console.WriteLine($"Inserted_{b}-{y.EntityName}.{y.RecID}"), (x, y) => Console.WriteLine($"Updated_{b}-{y.EntityName}.{y.RecID}"), (x, y) => Console.WriteLine($"Deleted_{b}-{y.EntityName}.{y.RecID}"));
                root.Inserted += (x, y) => Console.WriteLine($"Inserted_{b}-{y.EntityName}.{y.RecID}_custom");
                root.Updated += (x, y) => Console.WriteLine($"Updated_{b}-{y.EntityName}.{y.RecID}_custom");
                root.Deleted += (x, y) => Console.WriteLine($"Deleted_{b}-{y.EntityName}.{y.RecID}_custom");

                Thread.Sleep(30000);
            }
        }
    }
}
