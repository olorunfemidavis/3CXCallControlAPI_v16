using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("ivr")]
    [SampleParam("arg1", "show        | create            | delete    | update           ")]
    [SampleParam("arg2", "[ivr number]| ivrnumber         | ivrnumber | ivrnumber        ")]
    [SampleParam("arg3", "            | list_of_parameters|           | list_of_parameters")]
    [SampleDescription("Working with IVR.\n list_of_parameters is sequence of space separated strings (taken in quotes if required):\n"+
        "    PROMPT=filename|EXT<extnumber> - file which is placed in directory specified by IVRPROMPTPATH parameter or extnumber where from to record new fine with random name\n" +
        "    O<digit>=<IVRForwardType>.[<dnnumber>] - assign specific type of destination to option <digit>. <IVRForward> is from enum IVRForwardType <dnnumber> - local number must be proper for specific number\n"+
        "    TO=<seconds> - number of seconds\n"+
        "    TODEST=<IVRForwardType>.[<dnnumber>] - timeout action - same as for options\n"+
        "    NAME=<ivr name> - name of ivr"+
        "    prop.<NAME>=<value> - set DN property with naem <NAME> to the <value>"
        )
        ]
    class IVRSample : ISample
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
                        var ivr = isNew ? ps.GetTenant().CreateIVR(args[2]) : (ps.GetDNByNumber(args[2]) as IVR);
                        var param_set = args.Skip(3).Select(x => x.Split('=')).ToDictionary(x => x.First(), x => string.Join("=", x.Skip(1).ToArray()));

                        bool assignForward = isNew; //flag which trigger assignmnet of IVRForward collection
                        IEnumerable<IVRForward> ivrForwards = ivr.Forwards;
                        foreach (var paramdata in param_set)
                        {
                            
                            var paramname = paramdata.Key;
                            var paramvalue = paramdata.Value;
                            switch (paramname)
                            {
                                case "PROMPT":
                                    if (paramvalue.StartsWith("EXT"))
                                    {
                                        //record the prompt from the requested extension
                                        ivr.PromptFilename = "OMSamplesTestRecodrdedPromptForIVR" + ivr.Number + ".wav";
                                        var filename = Path.Combine(ps.GetParameterValue("IVRPROMPTPATH"), ivr.PromptFilename);
                                        ivr.PromptFilename = filename;
                                        if (File.Exists(filename))
                                        {
                                            File.Move(filename, filename + ".back");
                                        }
                                        using (var ext = PhoneSystem.Root.GetDNByNumber(paramvalue.Substring(3)) as Extension)
                                        using (var ev = new AutoResetEvent(false))
                                        using (var listener = new PsTypeEventListener<ActiveConnection>())
                                        {
                                            ActiveConnection activeConnection = null;
                                            listener.SetTypeHandler(
                                                (x) => activeConnection = x, 
                                                (x) => activeConnection = x, 
                                                (x) => ev.Set(), 
                                                (x) => x == activeConnection || (activeConnection==null&&x.DN == ext && x.Status == ConnectionStatus.Connected && x.ExternalParty=="RecordFile"), 
                                                (x) => ev.WaitOne(x));
                                            PhoneSystem.Root.ServiceCall("RecordFile",
                                            new Dictionary<string, string>()
                                            {
                                                { "filename", filename},
                                                { "extension", ext.Number }
                                            });
                                            listener.Wait(60000);//wait a minute until recording call is finished.
                                        }
                                        File.Delete(filename + ".back");
                                        if(!File.Exists(filename))
                                        {
                                            throw new FileNotFoundException($"{filename} is not recorded");
                                        }
                                    }
                                    else
                                        ivr.PromptFilename = paramvalue;
                                    break;
                                case "TO":
                                    ivr.Timeout = ushort.Parse(paramvalue);
                                    break;
                                case "NAME":
                                    ivr.Name = paramvalue;
                                    break;
                                default: //options and TODEST
                                    {
                                        if (paramname.StartsWith("prop."))
                                        {
                                            ivr.SetProperty(paramname.Substring(5), paramvalue);
                                            break;
                                        }
                                        var data = paramvalue.Split('.');
                                        var number = data[1]; //must be with . at the end
                                        IVRForwardType fwdtype;
                                        var todelete = !Enum.TryParse(data[0], out fwdtype);
                                        if ("TODEST" == paramname || (paramname.Length == 2 && paramname[0] == 'O' && paramname.Length == 2 && char.IsDigit(paramname, 1)))
                                        {
                                            var dn = ps.GetDNByNumber(number);
                                            if ("TODEST" == paramname)
                                            {
                                                ivr.TimeoutForwardDN = todelete ? null : dn;
                                                ivr.TimeoutForwardType = todelete ? IVRForwardType.EndCall : fwdtype;
                                            }
                                            else
                                            {
                                                var option = (byte)(paramname[1] - '0');
                                                var fwd = ivrForwards.FirstOrDefault(x => x.Number == option);
                                                if (fwd == null)
                                                {
                                                    if (todelete)
                                                        break;
                                                    fwd = ivr.CreateIVRForward();
                                                    ivrForwards = ivrForwards.Union(new IVRForward[] { fwd });
                                                }
                                                if (!todelete)
                                                {
                                                    fwd.Number = option;
                                                    fwd.ForwardDN = dn;
                                                    fwd.ForwardType = fwdtype;
                                                }
                                                else
                                                {
                                                    ivrForwards = ivrForwards.Where(x => x != fwd);
                                                }
                                                assignForward = true;
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
                        if (assignForward)
                        {
                            ivr.Forwards = ivrForwards.ToArray();
                        }
                        ivr.Save();
                    }
                    break;
                case "delete":
                    {
                        (ps.GetDNByNumber(args[2]) as IVR).Delete();
                        Console.WriteLine($"Deleted IVR {args[2]}");
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
                using (var ivrs = (args.Length > 2 ? new IVR[] { ps.GetDNByNumber(args[2]) as IVR } : ps.GetAll<IVR>().ToArray()).GetDisposer())
                {
                    var first = ivrs.First(); //exeption is there are no such extension
                    foreach (var ivr in ivrs)
                    {
                        Console.WriteLine($"IVR - {ivr.Number}:");
                        Console.WriteLine($"    NAME={ivr.Name}");
                        Console.WriteLine($"    PROMPT={ivr.PromptFilename}");
                        Console.WriteLine($"    TO={ivr.Timeout}");
                        Console.WriteLine($"    TODEST={ivr.TimeoutForwardType}.{ivr.TimeoutForwardDN?.Number}");
                        foreach (var f in ivr.Forwards.OrderBy(x => x.Number))
                        {
                            Console.WriteLine($"        O{f.Number}={f.ForwardType}.{f.ForwardDN?.Number}");
                        }
                        Console.WriteLine("    DNProperties:");
                        foreach (var p in ivr.GetProperties())
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
