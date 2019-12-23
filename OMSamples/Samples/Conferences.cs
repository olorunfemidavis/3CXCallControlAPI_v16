using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.Configuration;
namespace OMSamples.Samples
{
    [SampleCode("conference")]
    [SampleWarning("")]
    [SampleDescription("Commands:\n" +
        "    active - list of active audio conferences (including joined)\n" +
        "    startadhoc - (not supported) starts adhoc conference. number - add number initial member. pin - if specified as *<extnumber> - private conference of <extnumber> will call the <extnumber> in addition to the specified number" +
        "    scheduled - all scheduled meetings\n" +
        "    joinaudio - (not supported) replaces video only schedule with joined conference where viseo and audio conferences can communicate to each other.\n" +
        "    removeschedule - deleted schedule of conference. Active conference will continue as ad-hoc audio conference.\n" +
        "    dropall - drop all calls in active audio conference. Scheduled conference will be left available until end of schedule. ad-hoc will be terminated\n" +
        "    destroy - terminate all calls, delete schedule (if defined). Active conference will become inavailable\n" +
        "    add - adds call to specified number to the active conference\n" +
        "    hold - put member call on hold\n" +
        "    resume - resume member's call\n" +
        "    mute - mute incoming stream from member\n" +
        "    unmute - remove mute from incoming stream of member\n" +
        "    drop - disconenct member of audio conference\n"+
        "    resetbridge - reset parameters of the web meeting bridge"
        )]
    [SampleParam("arg1", "active|startadhoc|scheduled|joinaudio   |removeschedule|dropall  | destroy |add           | hold    |resume   |mute     |unmute   |drop     |resetbridge")]
    [SampleParam("arg2", "[id]  |pin       |[id]     |schedule_id |schedule_id   |active_id|active_id|active_id     |active_id|active_id|active_id|active_id|active_id|")]
    [SampleParam("arg3", "      |number    |         |            |              |         |         |call_to_number|member_id|member_id|member_id|member_id|member_id|")]
    class Conferences : ISample
    {
        void PrintStat(Statistics s, int max = int.MaxValue)
        {
            Console.WriteLine($"ID={s.ID}:");
            foreach (var a in s.Content)
            {
                Console.WriteLine($"\t{a.Key}={new string(a.Value.Take(max).ToArray())}");
            }
        }

        void PrintStats(IEnumerable<Statistics> cs)
        {
            foreach (var s in cs)
            {
                PrintStat(s, 50);
            }
        }

        void PrintParticipants(Statistics s)
        {
            Console.WriteLine($"\tCurrent participants:");
            foreach (var a in s.GetArray("participants"))
            {
                Console.WriteLine($"\t\t{a}");
            }
            Console.WriteLine($"\tLeft Conference:");
            foreach (var a in s.GetArray("disconnected"))
            {
                Console.WriteLine($"\t\t{a}");
            }
        }

        Dictionary<string, string> source = new Dictionary<string, string>
        {
            { "ContactUser", "$LineID" }
        };
        Dictionary<string, string> inbound = new Dictionary<string, string>
        {
            { "FromUserPart", "$CallerNum" },
            { "ContactUser", "$LineID" },
            { "FromDisplayName", "$CallerDispName" },
            { "ToDisplayName", "$CalledName" },
            { "ToUserPart","$CalledNum" },
        };
        Dictionary<string, string> inbound2 = new Dictionary<string, string>
        {
            { "FromDisplayName","$CallerName" }
        };
        Dictionary<string, string> outbound = new Dictionary<string, string>
        {
            {"RequestLineURIUser","$LineID"},
            {"ToDisplayName","$CalledName"},
            {"ToUserPart", "$CalledNum"},
            {"FromDisplayName", "$CallerName"},
            {"FromUserPart", "$CallerNum"}
        };
        GatewayParameterBinding[] ToBinding(Gateway gw, Dictionary<string, string> map)
        {
            return map.Select(x => gw.CreateGatewayParameterBinding(PhoneSystem.Root.GetGatewayParameterByName(x.Key), PhoneSystem.Root.GetGatewayParameterValueByName(x.Value))).ToArray();
        }
        void WebMeetingBridgeUpdate()
        {
            using (var lines = PhoneSystem.Root.GetExternalLines().GetDisposer(x => x.GetPropertyValue("WEBRTC_ACTIVE_BRIDGE") == "1"))
            {
                foreach(var l in lines)
                {
                    var gw = l.Gateway;
                    gw.SourceIdentification = ToBinding(gw, source);
                    gw.InboundParams = ToBinding(gw, inbound).Concat(ToBinding(gw, inbound2)).ToArray();
                    gw.OutboundParams = ToBinding(gw, outbound);
                    l.Save();
                }
            }
        }
        public void Run(params string[] args)
        {
            using (var confstates = PhoneSystem.Root.InitializeStatistics("S_CONFERENCESTATE").GetDisposer())
            using (var schedconf = PhoneSystem.Root.InitializeStatistics("S_SCHEDULEDCONF").GetDisposer())
            {
                var command = args[1]; //command
                var id = 0;
                int.TryParse(args.Length > 2 ? args[2] : "0", out id);
                var member_id = args.Length > 3 ? args[3] : null;
                switch (command)
                {
                    case "resetbridge":
                        {
                            WebMeetingBridgeUpdate();
                        }
                        break;
                    case "active":
                    case "scheduled":
                        if (id == 0)
                            PrintStats(command == "active" ? confstates : schedconf);
                        else
                        {
                            var conf = PhoneSystem.Root.GetByID(command == "active" ? "S_CONFERENCESTATE" : "S_SCHEDULEDCONF", id);
                            PrintStat(conf);
                            int schedule_id = 0;
                            if (command == "active")
                            {
                                PrintParticipants(conf);
                                if (int.TryParse(conf["sheduleid"], out schedule_id) && schedule_id != 0)
                                {
                                    PrintStat(PhoneSystem.Root.GetByID("S_SCHEDULEDCONF", schedule_id));
                                }
                                else
                                {
                                    Console.WriteLine("NO SCHEDULE");
                                }
                            }
                        }
                        break;
                    //case "joinaudio":
                    //    {
                    //        var stat = PhoneSystem.Root.GetByID("S_SCHEDULEDCONF", id);
                    //        if (stat["pv_meeting_type"] == "1")
                    //        {
                    //            var newconf = PhoneSystem.Root.CreateStatistics("S_SCHEDULEDCONF", new Random((int)DateTime.Now.Ticks).Next(100000, 999999).ToString());
                    //            newconf.Content = stat.Content;
                    //            newconf["pv_meeting_type"] = "2";
                    //            newconf["pin"] = newconf.GetName();
                    //            stat.Delete();
                    //            newconf.update();
                    //        }
                    //    }
                    //    break;
                    case "removeschedule":
                        {
                            using (var schedule = PhoneSystem.Root.GetByID("S_SCHEDULEDCONF", id))
                            {
                                schedule.Delete();
                            }
                        }
                        break;
                    default:
                        {
                            using (var confExt = PhoneSystem.Root.GetAll<ConferencePlaceExtension>().GetDisposer().First())
                            {
                                Dictionary<string, string> dict = new Dictionary<string, string>();
                                switch (command)
                                {
                                    //case "startadhoc":
                                    //    {
                                    //        dict["pin"] = args[2];
                                    //        dict["method"] = "add";
                                    //        dict["member"] = args[3];
                                    //    }
                                    //    break;
                                    case "destroy":
                                    case "dropall":
                                    case "drop":
                                    case "hold":
                                    case "resume":
                                    case "mute":
                                    case "unmute":
                                    case "add": // call to number which is stored in 
                                        {
                                            using (var activeconf = PhoneSystem.Root.GetByID("S_CONFERENCESTATE", id))
                                            {
                                                dict["pin"] = activeconf["pin"];
                                                if (command == "destroy" || command == "dropall")
                                                {
                                                    dict["method"] = "delete";
                                                    dict["hold"] = command == "dropall" ? "1" : "0";
                                                    dict["member"] = activeconf["pin"];
                                                }
                                                else
                                                {
                                                    dict["member"] = member_id;
                                                    switch (command)
                                                    {
                                                        case "hold":
                                                        case "resume":
                                                            dict["method"] = "hold";
                                                            dict["hold"] = command == "hold" ? "1" : "0";
                                                            break;
                                                        case "mute":
                                                        case "unmute":
                                                            dict["method"] = "mute";
                                                            dict["mute"] = command == "mute" ? "1" : "0";
                                                            break;
                                                        case "drop":
                                                        case "add":
                                                            dict["method"] = command;
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    default:
                                        throw new NotImplementedException($"{command} option is not implemented");
                                }
                                PhoneSystem.Root.ServiceCall(confExt.Number, dict);
                            }
                        }
                        break;
                }
            }
        }
    }
}
