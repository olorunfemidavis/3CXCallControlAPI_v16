using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("outboundrule")]
    [SampleParam("arg1", "show      | create      | update    | delete | gateways")]
    [SampleParam("arg2", "[name]    | parameters  | id        | id     |")]
    [SampleParam("arg3", "          |             | parameters|        ")]
    [SampleDescription("Working with OurboundRule.\n list_of_parameters is sequence of space separated strings (taken in quotes if required):\n" +
        "    NAME=<name>. checked for uniqueness"+
        "    PREFIX=<commaseparated list of prefixes>\n" +
        "    PRIORITY=<priority> - no check.\n" +
        "    NUMBERLENGTH=<comma separated list of length ranges>\n" +
        "    DNRANGES=<comma separated list of dn ranges>\n" +
        "    GROUPS=<comma separated list of groups>\n" +
        "    ROUTE<N>=<strip>.<prepend>.<gatewayid>\n"
        )
        ]
    class OutboundRuleSample : ISample
    {
        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            int showid = 0;
            switch (args[1])
            {
                case "create":
                case "update":
                    {
                        OutboundRule rule = args[1] == "create" ? ps.GetTenant().CreateOutboundRule() : ps.GetByID<OutboundRule>(int.Parse(args[2]));
                        foreach (var paramdata in args.Skip(3).Select(x => x.Split('=')))
                        {
                            var paramname = paramdata[0];
                            var paramvalue = paramdata[1];
                            switch (paramname)
                            {
                                case "PREFIX":
                                    rule.Prefix = paramvalue;
                                    break;
                                case "PRIORITY":
                                    rule.Priority = int.Parse(paramvalue);
                                    break;
                                case "NUMBERLENGTH":
                                    rule.NumberLengthRanges = paramvalue;
                                    break;
                                case "DNRENGES":
                                    rule.DNRanges = paramvalue.Split(',').Select(x => x.Split('-')).Select(x => { var range = rule.CreateDNRange(); range.From = x[0]; range.To = x.Length > 1 ? x[1] : x[0]; return range; }).ToArray();
                                    break;
                                case "GROUPS":
                                    rule.DNGroups = paramvalue.Split(',').Where(x=>!string.IsNullOrEmpty(x)).Select(x => ps.GetGroupByName(x)).ToArray();
                                    break;
                                case "NAME":
                                    using (var existing = ps.GetAll<OutboundRule>().GetDisposer(x => x.Name == paramvalue).ExtractFirstOrDefault())
                                    {
                                        if (existing != null)
                                        {
                                            throw new ArgumentException($"Outbound rule where NAME='{paramvalue}' already exists - {existing}");
                                        }
                                    }
                                    rule.Name = paramvalue;
                                    break;
                                default:
                                    if (paramname.StartsWith("ROUTE"))
                                    {
                                        var routenumber = int.Parse(paramname.Substring(5));
                                        if (routenumber < 1 || routenumber > 5)
                                            throw new ArgumentOutOfRangeException("Only 5 routes [1..5] are allowed");
                                        if (routenumber > rule.NumberOfRoutes)
                                        {
                                            rule.NumberOfRoutes = routenumber;
                                        }
                                        var routeindex = routenumber - 1;
                                        var routeParam = paramvalue.Split('.');

                                        rule[routeindex].StripDigits = byte.Parse(routeParam[0]);
                                        rule[routeindex].Prepend = routeParam[1];
                                        var gwid = routeParam.Skip(2).FirstOrDefault();
                                        rule[routeindex].Gateway = string.IsNullOrEmpty(gwid) ? null : (ps.GetByID<Gateway>(int.Parse(gwid))??throw new ArgumentException($"Gateway.{gwid} is not found"));
                                    }
                                    else
                                        throw new ArgumentException($"Unknown parameter {paramname}");
                                    break;
                            }
                        }
                        rule.Save();
                    }
                    break;
                case "delete":
                    ps.GetByID<OutboundRule>(int.Parse(args[2])).Delete();
                    return;
                case "gateways":
                    Console.WriteLine($"{string.Join("\n", ps.GetAll<Gateway>().Select(x => $"{x}"))}");
                    return;
                case "show":
                    break;
                default:
                    throw new ArgumentException("Invalid action name");
            }
            using (var outboundrules = (showid!=0 ? new OutboundRule[] { ps.GetByID<OutboundRule>(showid) } : ps.GetAll<OutboundRule>().ToArray()).GetDisposer())
            {
                var first = outboundrules.First();
                foreach (var or in outboundrules)
                {
                    Console.WriteLine($"{or}");
                    Console.WriteLine($"\tNAME={or.Name}");
                    Console.WriteLine($"\tPREFIX={or.Prefix}");
                    Console.WriteLine($"\tPRIORITY={or.Priority}");
                    Console.WriteLine($"\tNUMBERLENGTH={or.NumberLengthRanges}");
                    Console.WriteLine($"\tGROUPS={string.Join(",", or.DNGroups.Select(x => x.Name))}");
                    Console.WriteLine($"\tDNRANGES={string.Join(",", or.DNRanges.Select(x => $"{x.From }-{x.To}"))}");
                    int i = 0;
                    Console.WriteLine($"\t{string.Join("\n\t", or.Select(x => $"ROUTE{++i}={x}"))}");
                }
            }
        }
    }
}