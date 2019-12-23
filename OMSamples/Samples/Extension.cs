using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("extension")]
    [SampleParam("arg1", "show       | create   | delete    | update            |lookupemail")]
    [SampleParam("arg2", "[extnumber]| extnumber| extnumber | extnumber         |email")]
    [SampleParam("arg3", "           | list_of_parameters   | list_of_parameters|")]
    [SampleDescription("Working with Extension. Partial configuration\n list_of_parameters is sequence of space separated strings (taken in quotes if required):\n" +
        "    FIRST_NAME=<string> - first name\n" +
        "    LAST_NAME=<string> - last name\n" +
        "    EMAIL=<string> - email\n" +
        "    MOBILE=<numric string> - mobile number\n" +
        "    OUTBOUND_CALLER_ID=<numeric string> - mobile number\n" +
        "    profile.<AvailableProfileNAME>=AV(NA:<DestinationType>.[<number>].[<externalnumber>],[+|-]NAI:<DestinationType>.[<number>].[<externalnumber>],BUSY:<DestinationType>.[<number>].[<externalnumber>],[+|-]BUSYI:<DestinationType>.[<number>].[<externalnumber>])\n" +
        "    profile.<AwayProfileNameNAME>=AW(IALL:<DestinationType>.[<dnnumber>].[<externalnumber>],[+|-]IOOO:<DestinationType>.[<number>].[<externalnumber>],EALL:<DestinationType>.[<number>].[<externalnumber>],[+|-]EOOO:<DestinationType>.[<number>].[<externalnumber>])\n" +
        "    CURRENT_STATUS=<profilename> - name of the current profile\n" +
        "    prop.<NAME>=<value> - set DN property with name <NAME> to the <value>\n"+
        "    OVERRIDE_STATUS=<profilename>,<timespan>\n" +
        "    BINDTOMS=true|false\n" +
        "    REINVITES=true|false\n" +
        "    REPLACES=true|false\n" +
        "    RECORDCALLS=true|false\n"+
        "    SRTP=true|false\n"+
        "    Extension.<extension_simple_property>=<propval>\n"+
        "    AGENTLOGIN=<listofqueues>"+
        "    AGENTLOGOUT=<listofqueues>"
        )]
    class ExtensionSample : ISample
    {
        public enum PasswordGenerationOptions
        {
            Digits,
            DigitsLettersAnyCase,
            DigitsLettersLowerCase,
            DigitsLettersUpperCase,
            DigitsLettersPunctuation
        };

        string DestToString(Destination x)
        {
            return $"{x?.To}.{x?.Internal?.Number}.{x?.External}";
        }

        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            var emaillookup = PhoneSystem.Root.CreateLookup(()=>PhoneSystem.Root.GetExtensions(), y=>y.EmailAddress, "DN");
            var random = new Random((int)DateTime.Now.Ticks);
            switch (args[1])
            {
                case "lookupemail":
                    //stright to list
                    break;
                case "create":
                case "update":
                    {
                        bool isNew = args[1] == "create";
                        var extension = isNew ? ps.GetTenant().CreateExtension(args[2]) : (ps.GetDNByNumber(args[2]) as Extension);
                        var param_set = args.Skip(3).Select(x => x.Split('=')).ToDictionary(x => x.First(), x => string.Join("=", x.Skip(1).ToArray()));
                        string overrideProfileName=null;
                        DateTime overrideExpiresAt = DateTime.UtcNow; //will not be used if there is no OVERRIDE_STATUS option.

                        foreach (var paramdata in param_set)
                        {
                            var paramname = paramdata.Key;
                            var paramvalue = paramdata.Value;
                            switch (paramname)
                            {
                                case "AGENTLOGOUT":
                                case "AGENTLOGIN":
                                    {
                                        var data = paramvalue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                        var queues = new HashSet<string>(data);
                                        foreach(var agent in extension.QueueMembership.Where(x=>queues.Contains(x.Queue.Number)))
                                        {
                                            agent.QueueStatus = paramname == "AGENTLOGIN" ? QueueStatusType.LoggedIn : QueueStatusType.LoggedOut;
                                        }
                                    }
                                    break;
                                case "OVERRIDE_STATUS":
                                    {
                                        var data = paramvalue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                        var profile = extension.FwdProfiles.First(x => x.Name == data[0]);
                                        var expiresAt = DateTime.UtcNow + TimeSpan.Parse(data[1]);
                                        if (profile.ID != 0)//profile is in persistent storage we can set override
                                        {
                                            extension.OverrideExpiresAt = expiresAt;
                                            extension.CurrentProfileOverride = profile;
                                        }
                                        else
                                        {
                                            overrideProfileName = data[0];
                                            overrideExpiresAt = expiresAt;
                                        }

                                    }
                                    break;
                                case "GROUPS":
                                    {
                                        extension.GroupMembers = paramvalue.Split(',').Union(new string[] { "__DEFAULT__" }).Distinct().Select(x=>PhoneSystem.Root.GetGroupByName(x).CreateGroupMember(extension)).ToArray();
                                    }
                                    break;
                                case "FIRST_NAME":
                                    extension.FirstName = paramvalue;
                                    break;
                                case "LAST_NAME":
                                    extension.LastName = paramvalue;
                                    break;
                                case "EMAIL":
                                    extension.EmailAddress = paramvalue;
                                    break;
                                case "MOBILE":
                                    extension.SetProperty("MOBILENUMBER", paramvalue);
                                    break;
                                case "OUTBOUND_CALLER_ID":
                                    extension.OutboundCallerID = paramvalue;
                                    break;
                                case "BINDTOMS":
                                    extension.DeliverAudio = bool.Parse(paramvalue);
                                    break;
                                case "REINVITES":
                                    extension.SupportReinvite = bool.Parse(paramvalue);
                                    break;
                                case "REPLACES":
                                    extension.SupportReplaces = bool.Parse(paramvalue);
                                    break;
                                case "RECORDCALLS":
                                    extension.RecordCalls = bool.Parse(paramvalue);
                                    break;
                                case "SRTP":
                                    extension.EnableSRTP = bool.Parse(paramvalue);
                                    break;
                                case "CURRENT_STATUS":
                                    extension.CurrentProfile = extension.FwdProfiles.Where(x => x.Name == paramvalue).First();
                                    break;
                                case "AUTHID":
                                    if (!string.IsNullOrWhiteSpace(paramvalue) && paramvalue.All(x => Char.IsLetterOrDigit(x)) && Encoding.UTF8.GetBytes(paramvalue).Length == paramvalue.Length)
                                    {
                                        extension.AuthID = paramvalue;
                                    }
                                    else
                                        throw new ArgumentOutOfRangeException("AUTHID should be alphanumeric ASCII");
                                    break;
                                case "AUTHPASS":
                                    if (!string.IsNullOrWhiteSpace(paramvalue)&&paramvalue.All(x=>Char.IsLetterOrDigit(x))&&Encoding.UTF8.GetBytes(paramvalue).Length==paramvalue.Length)
                                    {
                                        extension.AuthPassword = paramvalue;
                                    }
                                    else
                                        throw new ArgumentOutOfRangeException("AUTHPASS should be alphanumeric ASCII");
                                    break;
                                default: //options and TODEST
                                    {
                                        if (paramname.StartsWith("prop."))
                                        {
                                            extension.SetProperty(paramname.Replace("prop.", ""), paramvalue);
                                            break;
                                        }
                                        else if (paramname.StartsWith("profile."))
                                        {
                                            var profilename = paramname.Replace("profile.", "");
                                            var profile = extension.FwdProfiles.Where(x => x.Name == profilename).First();
                                            var options = new string(paramvalue.Skip(3).ToArray()).Trim(')').Split(',').Select(x => x.Split(':')).ToDictionary(x => x[0], x => x[1]);
                                            if (paramvalue.StartsWith("AV(") && paramvalue.EndsWith(")")) //"Available route"
                                            {
                                                var route = profile.AvailableRoute;
                                                foreach (var o in options)
                                                {
                                                    var thekey = o.Key;
                                                    if (thekey.StartsWith("+") || thekey.StartsWith("-"))
                                                    {
                                                        switch (thekey)
                                                        {
                                                            case "+NAI":
                                                                route.NoAnswer.InternalInactive = false;
                                                                break;
                                                            case "-NAI":
                                                                route.NoAnswer.InternalInactive = true;
                                                                break;
                                                            case "+BUSYI":
                                                                route.Busy.InternalInactive = route.NotRegistered.InternalInactive = false;
                                                                break;
                                                            case "-BUSYI":
                                                                route.Busy.InternalInactive = route.NotRegistered.InternalInactive = true;
                                                                break;
                                                        }
                                                        if (o.Value == "")
                                                        {
                                                            //just switch activity.
                                                            continue;
                                                        }
                                                        else
                                                        {
                                                            thekey = thekey.Substring(1);
                                                        }
                                                    }
                                                    var data = o.Value.Split('.');
                                                    if (Enum.TryParse(data[0], out DestinationType dt))
                                                    {
                                                        var dest = new DestinationStruct(dt, ps.GetDNByNumber(data[1]), data[2]);
                                                        switch (thekey)
                                                        {
                                                            case "NA":
                                                                route.NoAnswer.AllCalls = dest;
                                                                break;
                                                            case "NAI":
                                                                route.NoAnswer.Internal = dest;
                                                                break;
                                                            case "BUSY":
                                                                route.Busy.AllCalls = route.NotRegistered.AllCalls = dest;
                                                                break;
                                                            case "BUSYI":
                                                                route.Busy.Internal = route.NotRegistered.Internal = dest;
                                                                break;
                                                        }
                                                    }
                                                    else
                                                        throw new ArgumentOutOfRangeException($"Unexpected destination definition{o.Key}<->{o.Value}");
                                                }
                                            }
                                            if (paramvalue.StartsWith("AW(") && paramvalue.EndsWith(")")) //"Available route"
                                            {
                                                var route = profile.AwayRoute;
                                                var external = profile.AwayRoute.External;
                                                foreach (var o in options)
                                                {
                                                    var thekey = o.Key;
                                                    if (thekey.StartsWith("+") || thekey.StartsWith("-"))
                                                    {
                                                        switch (thekey)
                                                        {
                                                            case "+EOOO":
                                                                route.External.OutOfHoursInactive = false;
                                                                break;
                                                            case "-EOOO":
                                                                route.External.OutOfHoursInactive = true;
                                                                break;
                                                            case "+IOOO":
                                                                route.Internal.OutOfHoursInactive = false;
                                                                break;
                                                            case "-IOOO":
                                                                route.Internal.OutOfHoursInactive = true;
                                                                break;
                                                        }
                                                        if (o.Value == "")
                                                        {
                                                            //just switch activity.
                                                            continue;
                                                        }
                                                        else
                                                        {
                                                            thekey = thekey.Substring(1);
                                                        }
                                                    }
                                                    var data = o.Value.Split('.');
                                                    if (Enum.TryParse(data[0], out DestinationType dt))
                                                    {
                                                        var dest = new DestinationStruct(dt, ps.GetDNByNumber(data[1]), data[2]);
                                                        switch (thekey)
                                                        {
                                                            case "IALL":
                                                                route.Internal.AllHours = dest;
                                                                break;
                                                            case "IOOO":
                                                                route.Internal.OutOfOfficeHours = dest;
                                                                break;
                                                            case "EALL":
                                                                route.External.AllHours = dest;
                                                                break;
                                                            case "EOOO":
                                                                route.External.OutOfOfficeHours = dest;
                                                                break;
                                                        }
                                                    }
                                                    else
                                                        throw new ArgumentOutOfRangeException($"Unexpected destination definition{o.Key}<->{o.Value}");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            throw new InvalidOperationException($"Unknown patameter{paramname}={paramvalue}");
                                        }
                                    }
                                    break;
                            }

                        }
                        extension.Save();
                        if (overrideProfileName != null) //desired override profile was not in persistent storage (new extension)
                        {
                            extension.CurrentProfileOverride = extension.FwdProfiles.First(x => x.Name == overrideProfileName);
                            extension.OverrideExpiresAt = overrideExpiresAt;
                            extension.Save();
                        }
                    }
                    break;
                case "delete":
                    {
                        (ps.GetDNByNumber(args[2]) as Extension).Delete();
                        Console.WriteLine($"Deleted Extension {args[2]}");
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
                using (var extensions = 
                    (args.Length > 2 ? (args[1]=="lookupemail"? emaillookup.Lookup(args[2]).ToArray(): new Extension[] { ps.GetDNByNumber(args[2]) as Extension }) : ps.GetAll<Extension>().ToArray()).GetDisposer())
                {
                    var first = extensions.First(); //exeption is there are no such extension
                    foreach (var extension in extensions)
                    {
                        Console.WriteLine($"Extension - {extension.Number}:");
                        Console.WriteLine($"  RECORDCALLS={extension.RecordCalls}");
                        Console.WriteLine($"  BINDTOMS={extension.DeliverAudio}");
                        Console.WriteLine($"  REINVITES={extension.SupportReinvite}");
                        Console.WriteLine($"  REPLACES={extension.SupportReplaces}");
                        Console.WriteLine($"  SRTP={extension.EnableSRTP}");
                        Console.WriteLine($"  AUTHID={extension.AuthID}");
                        Console.WriteLine($"  AUTHPASS={extension.AuthPassword}");
                        Console.WriteLine($"  ENABLED={extension.Enabled}");
                        Console.WriteLine($"    FIRST_NAME={extension.FirstName}");
                        Console.WriteLine($"    LAST_NAME={extension.LastName}");
                        Console.WriteLine($"    EMAIL={extension.EmailAddress}");
                        Console.WriteLine($"    MOBILE={extension.GetPropertyValue("MOBILENUMBER")}");
                        Console.WriteLine($"    OUTBOUND_CALLER_ID={extension.OutboundCallerID}");
                        Console.WriteLine($"    CURRENT_STATUS={extension.CurrentProfile?.Name}");
                        Console.WriteLine($"    GROUPS={string.Join(",", extension.GroupMembers.Select(x=>x.Group.Name))}");
                        Console.WriteLine($"    RIGHTS=\n        {string.Join("\n        ", extension.GroupMembers.Select(x => x.Group.Name+"="+x.RoleTag))}");
                        Console.WriteLine($"    QUEUES=\n        {string.Join("\n        ", extension.QueueMembership.Select(x => x.Queue.Number + "=" + x.QueueStatus.ToString()))}");
                        foreach (var fp in extension.FwdProfiles)
                        {
                            switch (fp.TypeOfRouting)
                            {
                                case RoutingType.Available:
                                    {
                                        var route = fp.AvailableRoute;
                                        var na = route.NoAnswer.AllCalls;
                                        var nai = route.NoAnswer.Internal;
                                        var b = route.Busy.AllCalls;
                                        var bi = route.Busy.Internal;
                                        var nasign = route.NoAnswer.InternalInactive ? "-" : "+";
                                        var bsign = route.Busy.InternalInactive ? "-" : "+";
                                        Console.WriteLine($"    profile.{fp.Name}=AV(NA:{DestToString(na)},{nasign}NAI:{DestToString(nai)},BUSY:{DestToString(b)},{bsign}BUSYI:{DestToString(bi)})");
                                    }
                                    break;
                                case RoutingType.Away:
                                    {
                                        var route = fp.AwayRoute;
                                        var eall = route.External.AllHours;
                                        var eooo = route.External.OutOfOfficeHours;
                                        var iall = route.Internal.AllHours;
                                        var iooo = route.Internal.OutOfOfficeHours;
                                        var eosign = route.External.OutOfHoursInactive? "-" : "+";
                                        var iosign = route.Internal.OutOfHoursInactive? "-" : "+";
                                        Console.WriteLine($"    profile.{fp.Name}=AW(IALL:{DestToString(iall)},{iosign}IOOO:{DestToString(iooo)},EALL:{DestToString(eall)},{eosign}EOOO:{DestToString(eooo)})");
                                    }
                                    break;
                                default:
                                    Console.WriteLine($"profile.{fp.Name}=!!!Invalid route type");
                                    break;
                            }
                        }
                        Console.WriteLine("    DNProperties:");
                        foreach (var p in extension.GetProperties())
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
