using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using System.Threading;

namespace OMSamples.Samples
{
    [SampleCode("phonebook")]
    [SampleParam("arg1", "show                      |lookup             | create              | update            | delete ")]
    [SampleParam("arg2", "'all'|'company'|dnnumber  |'company'|dnnumber]| ['company'|dnnumber]| ID                | ID     ")]
    [SampleParam("arg3...agrN", "                          |lookup_parameters  | content_parameters  | content_parameters|        ")]
    [SampleDescription("Working with PhoneBook\n Where: \n    lookup_parameters:" +
        "        <number to lookup> <minmatch>\n" +
        "    content_parameters:" +
        "        [PhoneBookEntry property name]=<string> - contact first name"
        )
        ]

    class PhoneBookSample : ISample
    {
        void Display(IEnumerable<PhoneBookEntry> list)
        {
            Console.WriteLine(
                $"{string.Join("\n", list.Select(x => $"{x}:\n    Properties:{string.Join("\n        ", typeof(PhoneBookEntry).GetProperties().Select(y => $"{y.Name}={y.GetValue(x)}"))}"))}");
        }

        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            switch (args[1])
            {
                case "create":
                case "update":
                    {
                        using (var pbe = args[1] == "update" ? ps.GetByID<PhoneBookEntry>(int.Parse(args[2])) : args[2] == "company" ? ps.GetTenant().CreatePhoneBookEntry() : ps.GetDNByNumber(args[2]).CreatePhoneBookEntry())
                        {
                            foreach (var param in args.Skip(3).Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)).Select(x => new KeyValuePair<string, string>(x[0], string.Join("", x.Skip(1)))))
                            {
                                typeof(PhoneBookEntry).GetProperty(param.Key).SetValue(pbe, param.Value);
                            }
                            pbe.Save();
                            Display(new[] { pbe });
                        }
                    }
                    break;
                case "delete":
                    {
                        using (var pbe = ps.GetByID<PhoneBookEntry>(int.Parse(args[2])))
                        {
                            pbe.Delete();
                            Console.WriteLine("REMOVED:");
                            Display(new[] { pbe });
                        }
                    }
                    break;
                case "lookup":
                    {
                        var minmatch = uint.Parse(args.Skip(4).FirstOrDefault() ?? uint.MaxValue.ToString());
                        using (var result = (args[2] == "company" ? ps.GetTenant().FindContacts(args[3], minmatch) : ps.GetDNByNumber(args[2]).FindContacts(args[3], minmatch)).GetDisposer())
                        {
                            Display(result);
                        }
                    }
                    break;
                case "show":
                    {
                        string a = args.Skip(2).FirstOrDefault();
                        using (var pb = ((a == "all" || a == null) ? ps.GetAll<PhoneBookEntry>().ToArray() : a == "company" ? ps.GetTenant().PhoneBookEntries : ps.GetDNByNumber(a).PhoneBookEntries).GetDisposer())
                        {
                            Display(pb);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid action name");
            }
        }
    }
}