using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using TCX.PBXAPI;

namespace OMSamples.Samples
{
    [SampleCode("refresh_line_registration")]
    [SampleParam("arg1", "Virtual extension number of External Line")]
    [SampleDescription("Shows how to refresh registration on VoIP provider Line")]
    class RefreshLineRegistrationSample : ISample
    {
        public void Run(params string[] args)
        {
            //args[1] - DN of external line
            DN dn = PhoneSystem.Root.GetDNByNumber(args[1]);
            if (dn is ExternalLine && (dn as ExternalLine).Gateway is VoipProvider)
            {
                PhoneSystem.Root.RefreshRegistration(dn.Number);
            }
            else
                Console.WriteLine(args[1] + " is not external line or not a VoipProvider line");
        }
    }
}
