using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("park_orbit_monitor")]
    [SampleDescription("Monitors activity on Parking Orbit")]
    class ParkOrbitMonitorSample : ISample
    {
        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            for (; ; )
            {
                ParkExtension orbit = ps.GetDNByNumber(ps.GetParameterByName("PARK").Value) as ParkExtension;
                ParkExtension unorbit = ps.GetDNByNumber(ps.GetParameterByName("UNPARK").Value) as ParkExtension;
                ActiveConnection[] parkedCalls = orbit.GetActiveConnections();

                foreach (ActiveConnection ac in parkedCalls)
                {
                    if (ac.InternalParty != null)
                    {
                        System.Console.WriteLine("Call(" + ac.CallID + "): parked on " + orbit.Number + " remoteParty is " + ac.InternalParty + "(" + ac.ExternalParty + ")");
                    }
                }
                ActiveConnection[] unparkedCalls = unorbit.GetActiveConnections();
                foreach (ActiveConnection ac in unparkedCalls)
                {
                    if (ac.InternalParty != null)
                    {
                        System.Console.WriteLine("Call(" + ac.CallID + "): parked on " + orbit.Number + " remoteParty is " + ac.InternalParty + "(" + ac.ExternalParty + ")");
                    }
                }
                Thread.Sleep(5000);
            }
        }
    }
}