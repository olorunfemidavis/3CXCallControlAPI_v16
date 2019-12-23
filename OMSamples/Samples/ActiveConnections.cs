using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("connection")]
    [SampleWarning("")]
    [SampleDescription("shows how to work with ActiveConnection objects")]
    [SampleParam("arg1", "dnregs                |answer|ondn |all|drop  |pickup |divertvm|divert |bargein |listen |whisper|record         |transfer|join   |makecall|callservice      |attacheddata")]
    [SampleParam("arg2", "numstartswith or [all]|achash|dnnum|   |achash|achash |achash  |achash |achash  |achash |achash |achash         |achash  |achash |reghash |servicename      |achach")]
    [SampleParam("arg3", "additional-keys       |      |     |   |      |destnum|        |destnum|reghash |reghash|reghash|RecordingAction|destnum |achash2|destnum |list of key=value|empty or [list of key=value]")]
    class ActiveConnections : ISample
    {
        string connectionAsString(ActiveConnection ac)
        {
            return $"ID={ac.ID}:CCID={ac.CallConnectionID}:S={ac.Status}:DN={ac.DN.Number}:EP={ac.ExternalParty}:REC={ac.RecordingState}";
        }

        void PrintAllCalls()
        {
            foreach (var c in PhoneSystem.Root.GetActiveConnectionsByCallID())
            {
                Console.ResetColor();
                Console.WriteLine($"Call {c.Key}:");
                foreach (var ac in c.Value.OrderBy(x => x.CallConnectionID))
                {
                    Console.WriteLine($"    {connectionAsString(ac)}");
                }
            }
        }

        void PrintDNCall(Dictionary<ActiveConnection, ActiveConnection[]> ownertoparties)
        {
            try
            {
                foreach (var kv in ownertoparties)
                {
                    Console.WriteLine($"Call {kv.Key.CallID}:");
                    var owner = kv.Key;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"    {connectionAsString(owner)}");
                    Console.ResetColor();
                    foreach (var party in kv.Value)
                    {
                        Console.WriteLine($"    {connectionAsString(party)}");
                    }
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }
        public void Run(params string[] args)
        {
            var ps = PhoneSystem.Root;
            //var calls = PhoneSystem.Root.GetActiveConnectionsByCallID();
            switch (args[1])
            {
                case "dnregs":
                    {
                        foreach (var dn in PhoneSystem.Root.GetDN().GetDisposer(x => x.Number.StartsWith(args[2]=="all"?"": args[2])))
                        {
                            foreach (var r in dn.GetRegistrarContactsEx())
                            {
                                Console.WriteLine($"{r.ID}-{r.Contact}-{r.UserAgent}-{string.Join("", args.Skip(3).Select(x => $"\n\t{x}={r[x]}"))}");
                            }
                        }
                    }
                    break;
                case "ondn":
                    {
                        using (var dn = PhoneSystem.Root.GetDNByNumber(args[2]))
                        {
                            using (var connections = dn.GetActiveConnections().GetDisposer())
                            {
                                var alltakenconnections = connections.ToDictionary(x => x, y => y.OtherCallParties);
                                PrintDNCall(alltakenconnections);
                                foreach (var a in alltakenconnections.Values)
                                {
                                    a.GetDisposer().Dispose();
                                }
                            }
                        }
                    }
                    break;
                case "all":
                    {
                        PrintAllCalls();
                    }
                    break;
                case "drop":
                    PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).Drop();
                    break;
                case "answer":
                    PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).Answer();
                    break;
                case "pickup":
                    PhoneSystem.Root.PickupCall(args[3], PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])));
                    break;
                case "divertvm":
                    {
                        var ac = PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2]));
                        ac.Divert(ac.DN.Number, true);
                    }
                    break;
                case "divert":
                    PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).Divert(args[3], false);
                    break;
                case "bargein":
                    PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).Bargein(PhoneSystem.Root.GetByID<RegistrarRecord>(int.Parse(args[3])), TCX.PBXAPI.PBXConnection.BargeInMode.BargeIn);
                    break;
                case "listen":
                    PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).Bargein(PhoneSystem.Root.GetByID<RegistrarRecord>(int.Parse(args[3])), TCX.PBXAPI.PBXConnection.BargeInMode.Listen);
                    break;
                case "whisper":
                    PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).Bargein(PhoneSystem.Root.GetByID<RegistrarRecord>(int.Parse(args[3])), TCX.PBXAPI.PBXConnection.BargeInMode.Whisper);
                    break;
                case "record":
                    {
                        if (Enum.TryParse(args[3], out RecordingAction ra))
                            PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).ChangeRecordingState(ra);
                        else
                            throw new ArgumentOutOfRangeException("Invalid record action");
                    }
                    break;
                case "transfer":
                    PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).ReplaceWith(args[3]);
                    break;
                case "join":
                    {
                        PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).ReplaceWithPartyOf(
                            PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[3])));
                    }
                    break;
                case "makecall":
                    {
                        using (var ev = new AutoResetEvent(false))
                        using (var listener = new PsTypeEventListener<ActiveConnection>())
                        using (var registrarRecord = PhoneSystem.Root.GetByID<RegistrarRecord>(int.Parse(args[2])))
                        {
                            listener.SetTypeHandler(null, (x) => ev.Set(), null, (x) => x["devcontact"].Equals(registrarRecord.Contact), (x) => ev.WaitOne(x));
                            PhoneSystem.Root.MakeCall(registrarRecord, args[3]);
                            try
                            {
                                if (listener.Wait(5000))
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Call initiated");
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Call is not initiated in 5 seconds");
                                }
                            }
                            finally
                            {
                                Console.ResetColor();
                            }
                        }
                    }
                    break;
                case "attacheddata":
                    {
                        var ac = PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2]));
                        Console.WriteLine("AttachedData:");
                        Console.WriteLine(string.Join("\n    ", PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).AttachedData.Select(x => x.Key + "=" + x.Value).ToArray()));
                        var data = args.Skip(3).Select(x => x.Split('=')).Where(x => x[0].StartsWith("public_")).ToDictionary(x => x[0], x => string.Join("=", x.Skip(1)));
                        if (data.Any())
                        {
                            Console.WriteLine("----------");
                            Console.WriteLine("Attaching:");
                            Console.WriteLine(string.Join("\n    ", data.Select(x => x.Key + "=" + x.Value).ToArray()));
                            using (var ev = new AutoResetEvent(false))
                            using (var listener = new PsTypeEventListener<ActiveConnection>())
                            {
                                Console.Write("Wait for update...");
                                listener.SetTypeHandler((x) => { ac = x; ev.Set(); }, null, null, (x) => x.Equals(ac), (x) => ev.WaitOne(x));
                                ac.AttachConnectionData(data);
                                try
                                {
                                    if (listener.Wait(5000))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("Updated:");
                                        Console.WriteLine(string.Join("\n    ", PhoneSystem.Root.GetByID<ActiveConnection>(int.Parse(args[2])).AttachedData.Select(x => x.Key + "=" + x.Value).ToArray()));
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("No update notifications received.");
                                    }
                                }
                                finally
                                {
                                    Console.ResetColor();
                                }
                            }
                        }

                    }
                    break;
                case "callservice":
                    {
                        PhoneSystem.Root.ServiceCall(args[2], args.Skip(3).Select(x => x.Split('=')).ToDictionary(x => x[0], x => string.Join("=", x.Skip(1))));
                    }
                    break;
                default:
                    throw new NotImplementedException("action is not implemented");
            }
        }
    }
}
