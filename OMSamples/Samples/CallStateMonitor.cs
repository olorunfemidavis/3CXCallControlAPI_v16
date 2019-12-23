using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("calls")]
    [SampleWarning("")]
    [SampleDescription("Shows how to use CallMonitor")]
    [SampleParam("arg1", "show            | monitor")]
    [SampleParam("arg2", "callid or 'all' | new or all or callid")]
    class CallStateMonitor : ISample
    {
        HashSet<int> dnfilter = new HashSet<int>();
        bool PrintAllConnections = false;
        string PrintAll(OMCallCollector.CallStateSnapshot state)
        {
            var sb = new StringBuilder();
            if (state != null)
            {
                sb.AppendLine($"################# Call.{state.ID}");
                sb.AppendLine($"{state}");
                if (PrintAllConnections)
                {
                    if (state != null)
                    {
                        foreach (var c in PhoneSystem.Root.GetCallParticipants(state.ID))
                        {
                            if (dnfilter.Count == 0 || dnfilter.Contains(c.DN.ID))
                            {
                                sb.AppendLine($"    @AC.{c.ID}");
                                sb.AppendLine($"    {c}".Replace("\r\n", "\r\n    "));
                                sb.AppendLine($"    @ConnectionState");
                                sb.AppendLine($"        {state.GetConnectionState(c)}".Replace("\r\n", "\r\n        "));
                            }
                        }
                    }
                }
                sb.AppendLine($"-------------------");
            }
            return sb.ToString();
        }
        public void Run(params string[] args)
        {
            bool localend = false;
            var omcache = PhoneSystem.Root.CallStorage;
            {
                //omcache.Reload();
                PrintAllConnections = args[1].EndsWith("_fullstate");
                switch (args[1])
                {
                    case "show":
                    case "show_fullstate":
                        switch (args[2])
                        {
                            case "all":
                                {
                                    foreach (var c in omcache.AllCalls)
                                        System.Console.WriteLine($"{c}");
                                }
                                break;
                            default:
                                System.Console.WriteLine($"{omcache.GetCall(int.Parse(args[2]))}");
                                break;
                        }
                        break;
                    case "monitor":
                    case "monitor_fullstate":
                        switch (args[2])
                        {
                            case "all": //all calls including existing
                                omcache.Updated += (id, state) => Console.WriteLine($"Updated - {id} - {PrintAll(state)}");
                                omcache.Removed += (id, state) => Console.WriteLine($"Ended {id} - {PrintAll(state)}");
                                break;
                            case "new": //only new calls
                                {
                                    var excludeIDs = new HashSet<uint>(PhoneSystem.Root.GetActiveConnectionsByCallID().Keys);
                                    dnfilter = new HashSet<int>(args.Skip(3).Select(x => PhoneSystem.Root.GetDNByNumber(x).ID));
                                    omcache.Updated += (id, state) => { if (!excludeIDs.Contains((uint)id)) Console.WriteLine($"Updated - {id} - {PrintAll(state)}"); };
                                    omcache.Removed += (id, state) => { if (!excludeIDs.Contains((uint)id)) Console.WriteLine($"Removed - {id} - {PrintAll(state)}");};
                                }
                                break;
                            default:
                                {
                                    var idcall = int.Parse(args[2]);
                                    omcache.Updated += (id, state) => { if (id == idcall) Console.WriteLine($"Updated - {id} - {PrintAll(state)}"); };
                                    //we end monitoring when call is finished
                                    omcache.Removed += (id, state) => { if (id == idcall) Console.WriteLine($"Removed - {id} - {PrintAll(state)}"); localend = true; };
                                }
                                break;
                        }

                        while (!Program.Stop&&!localend)
                                {
                                    Thread.Sleep(5000);
                                }
                        break;
                }
            }
        }
    }
}
