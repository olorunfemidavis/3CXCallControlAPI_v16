using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("change_parkcodes")]
    [SampleWarning("This sample changes global settings of PBX.")]
    [SampleParam("arg1", "dial code to park call from the Parking Orbit")]
    [SampleParam("arg2", "dial code to unpark call from the Parking Orbit")]
    [SampleDescription("Shows how to change dial codes of Parking Orbit")]
    class ChangeParkCodesSample : ISample
    {
        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            String newParkCode = args[1];
            String newUnparkCode = args[2];

            Parameter parkCode = ps.GetParameterByName("PARK");
            Parameter unparkCode = ps.GetParameterByName("UNPARK");
            DN oldPark = null;
            DN oldUnpark = null;
            //check park code
            if (parkCode == null || newParkCode != parkCode.Value)
            {
                DN d = ps.GetDNByNumber(newParkCode);
                if (d != null && !(d is ParkExtension))
                {
                    throw new Exception("park code is allocated to enother entity");
                }
                if (parkCode != null)
                {
                    oldPark = (ps.GetDNByNumber(parkCode.Value) as ParkExtension);
                }
            }
            //check unpark code
            if (unparkCode == null || newParkCode != unparkCode.Value)
            {
                DN d = ps.GetDNByNumber(newUnparkCode);
                if (d != null && !(d is ParkExtension))
                {
                    throw new Exception("unpark code is allocated to enother entity");
                }
                if (unparkCode != null)
                {
                    oldUnpark = (ps.GetDNByNumber(unparkCode.Value) as ParkExtension);
                }
            }
            if (parkCode == null)
            {
                parkCode = ps.CreateParameter();
                parkCode.Type = ParameterType.String;
                parkCode.Name = "PARK";
            }

            if (unparkCode == null)
            {
                unparkCode = ps.CreateParameter();
                unparkCode.Type = ParameterType.String;
                unparkCode.Name = "UNPARK";
            }

            ParkExtension[] pe = ps.GetParkExtensions();
            ParkExtension parkUpdate = null;
            ParkExtension unparkUpdate = null;

            foreach (DN p in pe)
            {
                if (oldPark == p)
                {
                    parkUpdate = oldPark as ParkExtension;
                }
                if (oldUnpark == p)
                {
                    unparkUpdate = oldUnpark as ParkExtension;
                }
            }

            if (parkUpdate == null || unparkUpdate == null)
            {
                foreach (ParkExtension p in pe)
                {
                    p.Delete();
                }
                parkUpdate = ps.GetTenant().CreateParkExtension(newParkCode);
                unparkUpdate = ps.GetTenant().CreateParkExtension(newUnparkCode);
            }
            else
            {
                parkUpdate.Number = newParkCode;
                unparkUpdate.Number = newUnparkCode;
            }

            parkUpdate.Save();
            parkCode.Value = newParkCode;
            parkCode.Save();
            unparkCode.Value = newUnparkCode;
            unparkCode.Save();
            unparkUpdate.Save();
        }
    }
}