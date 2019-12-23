using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("ringgroup")]
    [SampleParam("arg1", "show      | create      | update    | delete  ")]
    [SampleParam("arg2", "[rgnumber]| rgrnumber   | rgnumber  | rgnumber ")]
    [SampleParam("arg3", "          | parameters  | parameters|         ")]
    [SampleDescription("Working with RingGroup.\n list_of_parameters is sequence of space separated strings (taken in quotes if required):\n" +
        "    NAME=<queue name> - name of the queue\n" +
        "    STRATEGY=<RingGroup.StrategyType> - polling strategy as named in Queue.PollingStrategyType\n" +
        "    AGENTS=<dnnumber>[,<dnnumber>] - list of riggroup\n" +
        "    RINGTIME=<seconds> - ring timeout.\n" +
        "    NOANSWERDEST=<DestinationType>.[<dnnumber>].[<externalnumber>] - timeout action - same as for options\n" +
        "    prop.<NAME>=<value> - set DN property with naem <NAME> to the <value>\n\n"+
        "    NOTE: RingGroup with Paging strategy can be configured to use multicast transport instead of making calls to each of members.\n"+
        "          To set/reset usage of multicast, set/reset following DN properties of the Paging ringgroup:\n"+
        "            MULTICASTADDR=<muilticatIP>\n" +
        "            MULTICASTPORT=<multocastport>\n" +
        "            MULTICASTCODEC=<multicastcodec>\n" +
        "            MULTICASTPTIME=<codecptime>\n")
        ]
    class RingGroupSampel : ISample
    {
        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            switch (args[1])
            {
                case "create":
                case "update":
                    {
                        bool isNew = args[1] == "create";
                        var rg = isNew ? ps.GetTenant().CreateRingGroup(args[2]) : (ps.GetDNByNumber(args[2]) as RingGroup);
                        var param_set = args.Skip(3).Select(x => x.Split('=')).ToDictionary(x => x.First(), x => string.Join("=", x.Skip(1).ToArray()));
                        bool assignForward = isNew; //flag which trigger assignmnet of IVRForward collection
                        var riggroupAgents = rg.Members;

                        foreach (var paramdata in param_set)
                        {
                            var paramname = paramdata.Key;
                            var paramvalue = paramdata.Value;
                            switch (paramname)
                            {
                                case "NAME":
                                    rg.Name = paramvalue;
                                    break;
                                case "STRATEGY":
                                    {
                                        StrategyType strategyType;
                                        if (Enum.TryParse(paramvalue, out strategyType))
                                        {
                                            rg.RingStrategy = strategyType;
                                        }
                                        else
                                            throw new InvalidCastException("Undefined ring group strategy type");
                                    }
                                    break;
                                case "AGENTS":
                                    {
                                        var collection =
                                            paramvalue.Split(',')
                                            .Select(x => ps.GetDNByNumber(x) as Extension)
                                            .Where(x => x != null)
                                            .Distinct();
                                        rg.Members = collection.ToArray();
                                    }
                                    break;
                                case "RINGTIME":
                                    rg.RingTime = ushort.Parse(paramvalue);
                                    break;
                                case "NOANSWERDEST":
                                    {
                                        var data = paramvalue.Split('.');
                                        DestinationType destinationType;
                                        if (Enum.TryParse(data[0], out destinationType))
                                        {
                                            new DestinationStruct(
                                                destinationType,
                                                ps.GetDNByNumber(data[1]),
                                                data[2])
                                           .CopyTo(rg.ForwardNoAnswer);
                                        }
                                        else
                                            throw new InvalidCastException("Unknown NoAnswer destination type");
                                    }
                                    break;
                                default:
                                    {
                                        if (paramname.StartsWith("prop."))
                                        {
                                            rg.SetProperty(paramname.Substring(5), paramvalue);
                                        }
                                        else
                                            throw new InvalidOperationException($"Unknown parameter {paramname}={paramvalue}");
                                        break;
                                    }
                            }
                        }
                        rg.Save();
                    }
                    break;
                case "delete":
                    {
                        (ps.GetDNByNumber(args[2]) as RingGroup).Delete();
                        Console.WriteLine($"Deleted RingGroup {args[2]}");
                        return;
                    }
                case "show":
                    //simply display results
                    break;
                default:
                    throw new ArgumentException("Invalid action name");
            }
            //show result
            {
                using (var ringgroups = (args.Length > 2 ? new RingGroup[] { ps.GetDNByNumber(args[2]) as RingGroup } : ps.GetAll<RingGroup>().ToArray()).GetDisposer())
                {
                    var first = ringgroups.First(); //exeption is there are no such extension
                    foreach (var rg in ringgroups)
                    {
                        Console.WriteLine($"RingGroup - {rg.Number}:");
                        Console.WriteLine($"    NAME={rg.Name}");
                        Console.WriteLine($"    STRATEGY={rg.RingStrategy}");
                        Console.WriteLine($"    AGENTS={string.Join(",", rg.Members.Select(x => x.Number))}");
                        Console.WriteLine($"    RINGTIME={rg.RingTime}");
                        Console.WriteLine($"    NOANSWERDEST={rg.ForwardNoAnswer.To}.{rg.ForwardNoAnswer.Internal?.Number ?? rg.ForwardNoAnswer.External}");
                        Console.WriteLine($"    DNProperties:");
                        foreach (var p in rg.GetProperties())
                        {
                            var name = p.Name;
                            var value = p.Value.Length > 50 ? new string(p.Value.Take(50).ToArray()) + "..." : p.Value;
                            Console.WriteLine($"        prop.{name}={value}");
                        }
                    }
                }
            }
        }
    }
}

