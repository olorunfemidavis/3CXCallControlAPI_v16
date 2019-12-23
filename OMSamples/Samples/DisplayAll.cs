using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using System.Threading;

namespace OMSamples.Samples
{
    [SampleCode("display")]
    [SampleDescription("Shows information about all Parameters, Codecs, predefined conditions of the rules, IVRs and Extensions.")]
    class DisplayAllSample : ISample
    {
        string DestinationString(Destination d)
        {
            return $"{d.To}-{d.Internal?.Number}-{d.External}";
        }
        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            var beforeStat = new PhoneSystem.InternalStat();
            var afterStat = new PhoneSystem.InternalStat();
            var intermediateStat = new PhoneSystem.InternalStat();
            int i = 0;
            var start = DateTime.Now;
            while (!Program.Stop)
            {
                PhoneSystem.Root.GetInternalStat(beforeStat);
                i++;
                System.Console.WriteLine("Parameters:");
                foreach (Parameter p in ps.GetAll<Parameter>())
                {
                    System.Console.WriteLine($"\t{p}\n\nValue:{p.Value}");
                }
                System.Console.WriteLine("Codecs:");
                foreach (Codec c in ps.GetAll<Codec>())
                {
                    System.Console.WriteLine($"\t{c}");
                }
                PhoneSystem.Root.GetInternalStat(intermediateStat);
                Console.WriteLine($"SafeHandles: ----> {intermediateStat.ActiveHandles}");
                System.Console.WriteLine("Conditions:");
                foreach (RuleCondition rc in ps.GetRuleConditions())
                {
                    System.Console.WriteLine($"\t{rc}");
                }

                PhoneSystem.Root.GetInternalStat(intermediateStat);
                Console.WriteLine($"SafeHandles: ----> {intermediateStat.ActiveHandles}");

                System.Console.WriteLine("RuleHours:");
                foreach (RuleHours rh in ps.GetRuleHourTypes())
                {
                    System.Console.WriteLine($"\t{rh}");
                }

                PhoneSystem.Root.GetInternalStat(intermediateStat);
                Console.WriteLine($"SafeHandles: ----> {intermediateStat.ActiveHandles}");

                System.Console.WriteLine("RuleCalltype:");
                foreach (RuleCallType rct in ps.GetRuleCallTypes())
                {
                    System.Console.WriteLine($"\t{rct}");
                }

                System.Console.WriteLine("GatewayParameters:");
                foreach (GatewayParameter p in PhoneSystem.Root.GetGatewayParameters())
                {
                    System.Console.WriteLine($"\t{p}:");
                    System.Console.WriteLine("\t\tSourceID:\n\t\t\t", string.Join("\n\t\t\t", p.PossibleValuesAsSourceID.Select(x => $"{x}")));
                    System.Console.WriteLine("\t\tInbound :\n\t\t\t", string.Join("\n\t\t\t", p.PossibleValuesAsInbound.Select(x => $"{x}")));
                    System.Console.WriteLine("\t\tOutbound:\n\t\t\t", string.Join("\n\t\t\t", p.PossibleValuesAsOutbound.Select(x => $"{x}")));
                }

                PhoneSystem.Root.GetInternalStat(intermediateStat);
                Console.WriteLine($"SafeHandles: ----> {intermediateStat.ActiveHandles}");

                System.Console.WriteLine("\tOfficeHours:");
                foreach (var dayOfWeek in PhoneSystem.Root.GetTenant().Hours)
                {
                    System.Console.WriteLine($"\t\t{dayOfWeek.Key}\n");
                    foreach (var period in dayOfWeek.Value)
                    {
                        System.Console.WriteLine($"\t\t\t{period.begin}-{period.end}");
                    }
                }
                PhoneSystem.Root.GetInternalStat(intermediateStat);
                Console.WriteLine($"SafeHandles: ----> {intermediateStat.ActiveHandles}");

                System.Console.WriteLine("\tOfficeBreakTime:");
                foreach (var dayOfWeek in PhoneSystem.Root.GetTenant().BreakTime)
                {
                    System.Console.WriteLine($"\t\t{dayOfWeek.Key.ToString()}\n");
                    foreach (var period in dayOfWeek.Value)
                    {
                        System.Console.WriteLine($"\t\t\t{period.begin}-{period.end}");
                    }
                }
                PhoneSystem.Root.GetInternalStat(intermediateStat);
                Console.WriteLine($"SafeHandles: ----> {intermediateStat.ActiveHandles}");

                var exts = PhoneSystem.Root.GetAll<Extension>();
                {
                    foreach (var e in exts)
                    {
                        System.Console.WriteLine($"{e} - {e.FirstName} {e.LastName}");
                        System.Console.WriteLine("\tProperties:\n\t\t", string.Join("\n\t\t", e.GetProperties().Select(x=>$"{x}\n\t\tValue={x.Value}")));
                        System.Console.WriteLine("\tForwarding:");
                        foreach (FwdProfile extProfile in e.FwdProfiles)
                        {
                            Console.WriteLine($"\t\t{extProfile}");
                            switch (extProfile.TypeOfRouting)
                            {
                                case RoutingType.Available:
                                    {
                                        var route = extProfile.AvailableRoute;
                                        Console.WriteLine($"\t\t\tN/A={DestinationString(route.NoAnswer.AllCalls)}-{DestinationString(route.NoAnswer.Internal)}");
                                        Console.WriteLine($"\t\t\tBusy={DestinationString(route.Busy.AllCalls)}-{DestinationString(route.Busy.Internal)}");
                                        Console.WriteLine($"\t\t\tN/R={DestinationString(route.NotRegistered.AllCalls)}-{DestinationString(route.NotRegistered.Internal)}");
                                    }
                                    break;
                                case RoutingType.Away:
                                    {
                                        var route = extProfile.AwayRoute;
                                        Console.WriteLine($"\t\t\tExternal={DestinationString(route.External.AllHours)}-{DestinationString(route.External.OutOfOfficeHours)}");
                                        Console.WriteLine($"\t\t\tInternal={DestinationString(route.Internal.AllHours)}-{DestinationString(route.Internal.OutOfOfficeHours)}");
                                    }
                                    break;
                            }
                        }
                    }
                }

                var ivrs = PhoneSystem.Root.GetAll<IVR>();
                {
                    foreach (var ivr in ivrs)
                    {
                        Console.WriteLine($"{ivr} - {ivr.PromptFilename} - {ivr.Timeout} - {ivr.TimeoutForwardType} {ivr.TimeoutForwardDN}\n");
                        Console.WriteLine($"\tOptions:\n\t\t{string.Join("\n\t\t", ivr.Forwards.Select(x=>$"{x.Number} - {x.ForwardType} - {x.ForwardDN}"))}");
                    }
                }
                
                PhoneSystem.Root.GetInternalStat(intermediateStat);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //Thread.Sleep(5000);
                PhoneSystem.Root.GetInternalStat(afterStat);
                Console.WriteLine($"SafeHandles: {beforeStat.ActiveHandles} ----> {intermediateStat.ActiveHandles} ----> {afterStat.ActiveHandles}");
                Console.WriteLine($"{i} - {i/(DateTime.Now-start).TotalSeconds} iterations per sec");
            }
        }
    }
}
