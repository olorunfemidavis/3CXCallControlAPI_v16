using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("officehours")]
    [SampleWarning("This sample can modify office hours/breaktime of objects.")]
    [SampleParam("arg1",        "show                         | setofficetime         | setbreaktime          |setholiday           |removeholiday")]
    [SampleParam("arg2",        "[holidays|office|dn]         | \"office\" or dnnumber| \"office\" or dnnumber|nameofholiday        |nameofholiday")]
    [SampleParam("arg3",        "[dnnumber or name of holiday]| RuleHoursType         | RuleHoursType         |yyyy-MM-dd=[hh\\:mm]]|")]
    [SampleParam("arg4...agrN", "                             | [list_of_ranges]      |[list_of_ranges]       |yyyy-MM-dd=[hh\\:mm]]|")]
    [SampleDescription("Shows how to work with Schedule object and holidays.\n" +
        "list_of_ranges is sequence of the strings. Modifications are applied only for specified days. to clear schedule of specific DayOfWeek set it empty{}\n" +
        "DayOfWeek=[start-end][,start-end]...\n"
        )]
    class SetOfficeHoursSample : ISample
    {
        void PrintSchedule(string name, Schedule schedule)
        {
            Console.WriteLine($"    {name} - {schedule.HoursType.Type}");
            foreach (var d in schedule.OrderBy(x=>x.Key))
            {
                Console.WriteLine($"        {d.Key}={string.Join(",", d.Value.Select(x=> $"{x.begin}-{x.end}"))}");
            }
        }
        Schedule UpdateSchedule(Schedule originalschedule, string hourstype, IEnumerable<string> days)
        {
            RuleHoursType rht;
            if (!Enum.TryParse(hourstype, out rht))
            {
                throw new ArgumentOutOfRangeException("Unknown hours type");
            }

            originalschedule.HoursType = PhoneSystem.Root.GetRuleHourTypeByType(rht);

            foreach (var d in days.Select(x => x.Split('=')).ToDictionary(
                x => { DayOfWeek dow; if (!Enum.TryParse(x[0], out dow)) throw new ArgumentOutOfRangeException("Unknown DayOfWeek"); return dow; },
                y => y[1].Split(',').Select(x => x.Split('-')).Where(x=>x.Length>1).Select(x => new Schedule.PeriodOfDay(TimeSpan.Parse(x[0]), TimeSpan.Parse(x[1]))).ToList()))
            {
                originalschedule.Remove(d.Key);
                foreach (var period in d.Value)
                {
                    originalschedule.Add(d.Key, period);
                }
            }
            return originalschedule;
        }

        public void Run(params string[] args)
        {
            IEnumerable<OfficeHoliday> toshowHolidays = null;
            IEnumerable<Tenant> toshowTenant = null;
            IEnumerable<Extension> toshowExtension = null;
            PhoneSystem ps = PhoneSystem.Root;
            switch (args[1])
            {
                case "setofficetime":
                    if (args[2] == "office")
                    {
                        //set tenant data
                        var tenant = ps.GetTenant();
                        tenant.Hours = UpdateSchedule(tenant.Hours, args[3], args.Skip(4));
                        tenant.Save();
                        toshowTenant = new Tenant[] { tenant };
                    }
                    else
                    {
                        var dn = PhoneSystem.Root.GetDNByNumber(args[2]) as Extension; //valid only for extension object
                        dn.Hours = UpdateSchedule(dn.Hours, args[3], args.Skip(4));
                        dn.Save();
                        toshowExtension = new Extension[] { dn };
                    }
                    break;
                case "setbreaktime":
                    if (args[2] == "office")
                    {
                        //set tenant data
                        var tenant = ps.GetTenant();
                        tenant.BreakTime = UpdateSchedule(tenant.BreakTime, args[3], args.Skip(4));
                        tenant.Save();
                        toshowTenant = new Tenant[] { tenant };
                    }
                    else
                    {
                        var dn = PhoneSystem.Root.GetDNByNumber(args[2]) as Extension; //valid only for extension object
                        dn.BreakTime = UpdateSchedule(dn.BreakTime, args[3], args.Skip(4));
                        dn.Save();
                        toshowExtension = new Extension[] { dn };
                    }
                    break;
                case "removeholiday":
                    {
                        using (var holiday = ps.GetTenant().GetOfficeHolidays().GetDisposer(x => x.Name == args[2]).ExtractFirstOrDefault())
                        {
                            holiday.Delete();
                            Console.WriteLine($"{holiday.Name} deleted");
                            return;
                        }
                    }
                case "setholiday":
                    {
                        bool hasendtime = args.Length > 4;
                        var holiday = ps.GetTenant().GetOfficeHolidays().GetDisposer(x => x.Name == args[2]).ExtractFirstOrDefault();
                        var paramsStart = args[3].Split('=');
                        var datestart = paramsStart[0].Split('-');
                        TimeSpan? startspan = paramsStart.Length > 1 ? TimeSpan.Parse(paramsStart[1]) : (TimeSpan?)null;
                        string[] dateend=null;
                        TimeSpan? endspan=null;
                        if (hasendtime)
                        {
                            var paramsEnd = args[4].Split('=');
                            dateend = paramsEnd[0].Split('-');
                            endspan = paramsStart.Length > 1 ? TimeSpan.Parse(paramsEnd[1]) : (TimeSpan?)null;
                        }
                        if (holiday == null)
                        {
                            if (!hasendtime)
                                holiday = ps.GetTenant().CreateOfficeHoliday(
                                    args[2],
                                    byte.Parse(datestart[2]),
                                    byte.Parse(datestart[1]),
                                    ushort.Parse(datestart[0]),
                                    startspan);
                            else
                                holiday = ps.GetTenant().CreateOfficeHoliday(
                                    args[2],
                                    byte.Parse(datestart[2]),
                                    byte.Parse(datestart[1]),
                                    ushort.Parse(datestart[0]),
                                    startspan,
                                    byte.Parse(dateend[2]),
                                    byte.Parse(dateend[1]),
                                    ushort.Parse(dateend[0]),
                                    endspan
                                    );
                        }
                        else
                        {
                            holiday.Day = byte.Parse(datestart[2]);
                            holiday.Month = byte.Parse(datestart[1]);
                            holiday.Year = ushort.Parse(datestart[0]);
                            holiday.TimeOfStartDate = startspan.Value;
                            if (hasendtime)
                            {
                                holiday.DayEnd = byte.Parse(dateend[2]);
                                holiday.MonthEnd = byte.Parse(dateend[1]);
                                holiday.YearEnd = ushort.Parse(dateend[0]);
                                holiday.TimeOfEndDate = endspan.Value;
                            }
                        }
                        holiday.Save();
                        toshowHolidays = new OfficeHoliday[] { holiday };
                    }
                    break;
                case "show":
                    if (args.Length > 2)
                    {
                        switch (args[2])
                        {
                            case "office":
                                toshowTenant = new Tenant[] { ps.GetTenant() };
                                break;
                            case "dn":
                                toshowExtension = args.Length > 3 ? new Extension[] { ps.GetDNByNumber(args[3]) as Extension } : ps.GetExtensions();
                                break;
                            case "holidays":
                                toshowHolidays = args.Length > 3 ? ps.GetTenant().GetOfficeHolidays().GetDisposer(x=>x.Name==args[3]) : ps.GetTenant().GetOfficeHolidays().GetDisposer();
                                break;
                            default:
                                throw new NotImplementedException($"Invalid argument {args[2]} for 'show' action");
                        }
                    }
                    else
                    {
                        toshowTenant = new Tenant[] { ps.GetTenant() };
                        toshowExtension =ps.GetExtensions();
                        toshowHolidays = ps.GetTenant().GetOfficeHolidays().GetDisposer();
                    }
                    break;
                default:
                    throw new NotImplementedException($"Invalid action {args[1]}");

            }
            if (toshowTenant != null)
            {
                foreach (var a in toshowTenant)
                {
                    PrintSchedule("OfficeHours", a.Hours);
                    PrintSchedule("BreakTime", a.BreakTime);
                }
            }

            if (toshowExtension != null)
            {
                foreach (var a in toshowExtension)
                {
                    Console.WriteLine($"Extension - {a.Number}");
                    PrintSchedule("OfficeHours", a.Hours);
                    PrintSchedule("BreakTime", a.BreakTime);
                }
            }

            if (toshowHolidays != null)
            {
                foreach (var a in toshowHolidays)
                {
                    Console.WriteLine($"holiday - {a.Name}");
                    Console.WriteLine($"    start:{a.Year:0000}-{a.Month:00}-{a.Day:00}={a.TimeOfStartDate}");
                    if (a.DayEnd != 0 || a.MonthEnd != 0)
                    {
                        Console.WriteLine($"    end:{a.YearEnd:0000}-{a.MonthEnd:00}-{a.DayEnd:00}={a.TimeOfEndDate}");
                    }
                }
            }
        }
    }
}