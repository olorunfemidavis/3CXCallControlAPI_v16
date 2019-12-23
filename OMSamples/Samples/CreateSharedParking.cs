using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("create_shared_parking")]
    [SampleParam("arg1", "name of shared parking place")]
    [SampleDescription("This sample adds Shared parking place. The name MUST start with 'SP'.")]
    class CreateSharedParkingSample : ISample
    {
        public void Run(params string[] args)
        {
            if (!args[1].StartsWith("SP"))
                throw new ArgumentException("Shared Park Extension must start with 'SP'");

            ParkExtension sp = PhoneSystem.Root.GetTenant().CreateParkExtension(args[1]);
            sp.Save();
        }
    }
}