using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("fax")]
    [SampleParam("arg1", "show      |create    | update    |delete  ")]
    [SampleParam("arg2", "[dnnumber]|dnnumber  | dnnumber  |dnnumber")]
    [SampleParam("arg3", "          |parameters| parameters|       ")]
    [SampleParam("arg4...argN", "parameters:\n"+
        "    AUTHID=<alfanumeric_string> - \n" +
        "    AUTHPASS=<alfanumeric_string> - alfanumeric string\n" +
        "    OUTBOUND_CALLER_ID=<numeric string> - outbound called if for calls originated by the device" +
        "    prop.<NAME>=<value> - set DN property with naem <NAME> to the <value>"
        )]
    [SampleDescription("Shows how to work with FaxExtension object")]
    class FaxExtensionSample: ISample
    {
        public void Run(params string[] args)
        {
            var ps = PhoneSystem.Root;
            switch (args[1])
            {
                case "create":
                case "update":
                    {
                        bool isNew = args[1] == "create";
                        var fax = isNew ? ps.GetTenant().CreateFaxExtension(args[2]) : (ps.GetDNByNumber(args[2]) as FaxExtension);
                        var param_set = args.Skip(3).Select(x => x.Split('=')).ToDictionary(x => x.First(), x => string.Join("=", x.Skip(1).ToArray()));
                        foreach (var paramdata in param_set)
                        {
                            var paramname = paramdata.Key;
                            var paramvalue = paramdata.Value;
                            switch (paramname)

                            {
                                case "AUTHID":
                                    fax.AuthID = paramvalue;
                                    break;
                                case "AUTHPASS":
                                    fax.AuthID = paramvalue;
                                    break;
                                case "OUTBOUND_CALLER_ID":
                                    if (paramvalue.All(x => char.IsDigit(x)))
                                        fax.OutboundCallerId = paramvalue;
                                    else
                                        throw new ArgumentOutOfRangeException($"{paramname}={paramvalue} is not valid. Expected numeric string");
                                    break;
                                default:
                                    if (paramname.StartsWith("prop."))
                                    {
                                        fax.SetProperty(paramname.Substring(5), paramvalue);
                                    }
                                    else
                                        throw new InvalidOperationException($"Unknown parameter {paramname}={paramvalue}");
                                    break;

                            }
                            fax.Save();
                        }
                    }
                    break;
                case "delete":
                    {
                        var fax = (ps.GetDNByNumber(args[2]) as FaxExtension);
                        if (ps.GetParameterValue("FAXOVEREMAILGATEWAY") == fax.Number)
                        {
                            throw new InvalidOperationException($"Fax extension {fax.Number} is the fax to email gateway.");
                        }
                        fax.Delete();
                    }
                    break;
            }

            //show result
            {
                using (var faxes = (args.Length > 2 ? new FaxExtension[] { ps.GetDNByNumber(args[2]) as FaxExtension} : ps.GetAll<FaxExtension>().ToArray()).GetDisposer())
                {
                    var first = faxes.First(); //exeption is there are no such extension
                    foreach (var fax in faxes)
                    {
                        Console.WriteLine($"FaxExtension - {fax.Number}:");
                        Console.WriteLine($"    AUTHID={fax.AuthID}");
                        Console.WriteLine($"    AUTHPASS={fax.AuthPassword}");
                        Console.WriteLine($"    OUTBOUND_CALLER_ID={fax.OutboundCallerId}");
                        Console.WriteLine($"    DNProperties:");
                        foreach (var p in fax.GetProperties())
                        {
                            var name = p.Name;
                            var value = p.Value.Length > 50 ? p.Value.Take(50) + "..." : p.Value;
                            Console.WriteLine($"        prop.{name}={value}");
                        }
                    }
                }
            }
        }
    }
}