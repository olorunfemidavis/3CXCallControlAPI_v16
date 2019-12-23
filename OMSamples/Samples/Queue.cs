using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("queue")]
    [SampleParam("arg1", "show     | create     | update    | delete  ")]
    [SampleParam("arg2", "[qnumber]| qnumber    | qnumber   | qnumber ")]
    [SampleParam("arg3", "         | parameters | parameters|         ")]
    [SampleDescription("Working with Queues.\n parameters is sequence of space separated strings (taken in quotes if required):\n" +
        "    NAME=<queue name> - name of the queue\n" +
        "    PSTRATEGY=<Queue.PollingStrategyType> - polling strategy as named in Queue.PollingStrategyType\n" +
        "    POLLINGTIME=<seconds> - ringing time for polling callss\n" +
        "    INTRO=filename - intro prompt of the queue - the file which is loacted in the directory specified by IVRPROMPTPATH parameter.\n" +
        "    MOH=filename - Music On Hold for calls which are waiting in the queue\n" +
        "    AGENTS=<dnnumber>[,<dnnumber>] - list of queue agents\n" +
        "    MANAGERS=<dnnumber>[,<dnnumber>] - list of queue managers\n" +
        "    MAXWAIT=<seconds> - maximal time of waiting in the queue.\n" +
        "    NOANSWERDEST=<DestinationType>.[<dnnumber>].[<externalnumber>] - timeout action - same as for options\n" +
        "    LOGIN=<dnnumber>[,<dnnumber>] - Agents to login into the queue\n" +
        "    LOGOUT=<dnnumber>[,<dnnumber>] - Agents to logout from the queue\n" +
        "    prop.<NAME>=<value> - set DN property with naem <NAME> to the <value>")
        ]
    class QueueSample : ISample
    {

        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            switch (args[1])
            {
                case "create":
                case "update":
                    {
                        var queue = args[1] == "create" ? ps.GetTenant().CreateQueue(args[2]) : (ps.GetDNByNumber(args[2]) as Queue);
                        var param_set = args.Skip(3).Select(x => x.Split('=')).ToDictionary(x => x.First(), x => string.Join("=", x.Skip(1).ToArray()));
                        IEnumerable<QueueAgent> queueAgents = queue.QueueAgents;
                        IEnumerable<QueueManager> queueManagers = queue.QueueManagers;

                        foreach (var paramdata in param_set)
                        {
                            var paramname = paramdata.Key;
                            var paramvalue = paramdata.Value;
                            switch (paramname)
                            {
                                case "NAME":
                                    queue.Name = paramvalue;
                                    break;
                                case "PSTRATEGY":
                                    {
                                        PollingStrategyType strategyType;
                                        if (Enum.TryParse(paramvalue, out strategyType))
                                        {
                                            queue.PollingStrategy = strategyType;
                                        }
                                        else
                                            throw new InvalidCastException("Undefined Polling strategy type");
                                    }
                                    break;
                                case "POLLINGTIME":
                                    {
                                        queue.RingTimeout = ushort.Parse(paramvalue);
                                    }
                                    break;
                                case "INTRO":
                                    queue.IntroFile = paramvalue;
                                    break;
                                case "MOH":
                                    queue.OnHoldFile = paramvalue;
                                    break;
                                case "AGENTS":
                                case "MANAGERS":
                                    {
                                        var collection = 
                                            paramvalue.Split(',')
                                            .Select(x => ps.GetDNByNumber(x) as Extension)
                                            .Where(x => x != null)
                                            .Distinct();

                                        if(paramname=="AGENTS")
                                            queue.QueueAgents=collection.Select(x => queue.CreateAgent(x)).ToArray();
                                        else
                                            queue.QueueManagers=collection.Select(x => queue.CreateManager(x)).ToArray();
                                    }
                                    break;
                                case "LOGIN":
                                case "LOGOUT":
                                    {
                                        //those parameters are intersect with qlogin sample (see QueueLogin implementation)
                                        var expectedStatus = paramname == "LOGIN" ? QueueStatusType.LoggedIn : QueueStatusType.LoggedOut;
                                        var hs = new HashSet<string>(paramvalue.Split(','));
                                        //queue.QueueAgents = queue.QueueAgents.Select(x=> { if (x.QueueStatus != expectedStatus) x.QueueStatus = expectedStatus; return x; });
                                        //queue.Save();
                                        var currentSet = queue.QueueAgents.Where(x=>hs.Contains(x.DN.Number));
                                        foreach (var a in currentSet)
                                        {
                                            var ext = a.DN as Extension;
                                            //list of logged where to the agent is willing to login when set LoggedIn states of the extension
                                            var login_prop = ext.GetPropertyValue("LOGGED_IN_QUEUES");
                                            if (expectedStatus != ext.QueueStatus)
                                            {
                                                switch (ext.QueueStatus)
                                                {
                                                    case QueueStatusType.LoggedIn:
                                                        //we need to logout (see condition above), so
                                                        //if "login list" is explicitly specified remove this queue from that list
                                                        if (login_prop != null)
                                                        {
                                                            Console.Write($"Set LoggedOut for {ext.Number}");
                                                            ext.SetProperty("LOGGED_IN_QUEUES", string.Join(",", ext.GetPropertyValue("LOGGED_IN_QUEUES").Split(',').Where(x => x != queue.Number)));
                                                        }
                                                        else //otherwise - specify all queues exept updated
                                                        {
                                                            Console.Write($"Set LoggedOut for {ext.Number}");
                                                            ext.SetProperty("LOGGED_IN_QUEUES", string.Join(",", ext.GetQueues().Where(x => x.Number != queue.Number)));
                                                        }
                                                        ext.Save();
                                                        break;
                                                    case QueueStatusType.LoggedOut:
                                                        //we need to login to this queue only when extension in "LoggedOut" state, so
                                                        Console.Write($"Set LoggedIn for {ext.Number}");
                                                        ext.SetProperty("LOGGED_IN_QUEUES", queue.Number);
                                                        ext.QueueStatus = QueueStatusType.LoggedIn;
                                                        ext.Save();
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                switch (ext.QueueStatus)
                                                {
                                                    case QueueStatusType.LoggedIn:
                                                        if (login_prop != null)
                                                        {
                                                            Console.Write($"Set LoggedIn for {ext.Number}");
                                                            //ensure that updated queue is in the list
                                                            ext.SetProperty("LOGGED_IN_QUEUES", string.Join(",", ext.GetPropertyValue("LOGGED_IN_QUEUES").Split(',').Union(new string[] { queue.Number }).Distinct()));
                                                        }
                                                        else
                                                        {
                                                            //nothing to do - already logged in to all queues
                                                        }
                                                        break;
                                                    case QueueStatusType.LoggedOut:
                                                        //already logged out from all queues
                                                        break;
                                                }
                                            }
                                        }

                                    }
                                    break;
                                case "MAXWAIT":
                                    queue.MasterTimeout = ushort.Parse(paramvalue);
                                    break;
                                case "NOANSWERDEST":
                                    {
                                        var data = paramvalue.Split('.');
                                        if (Enum.TryParse(data[0], out DestinationType destinationType))
                                        {
                                            new DestinationStruct(
                                                destinationType, 
                                                ps.GetDNByNumber(data[1]), 
                                                data?[2])
                                           .CopyTo(queue.ForwardNoAnswer);
                                        }
                                        else
                                            throw new InvalidCastException("Unknown NoAnswer destination type");
                                    }
                                    break;
                                default:
                                    {
                                        if (paramname.StartsWith("prop."))
                                        {
                                            queue.SetProperty(paramname.Substring(5), paramvalue);
                                        }
                                        else
                                            throw new InvalidOperationException($"Unknown parameter {paramname}={paramvalue}");
                                        break;
                                    }
                            }
                        }
                        queue.Save();
                    }
                    break;
                case "delete":
                    {
                        (ps.GetDNByNumber(args[2]) as Queue).Delete();
                        Console.WriteLine($"Deleted Queue {args[2]}");
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
                using (var queues = (args.Length > 2 ? new Queue[] { ps.GetDNByNumber(args[2]) as Queue } : ps.GetAll<Queue>().ToArray()).GetDisposer())
                {
                    var first = queues.First(); //exeption is there are no such extension
                    foreach (var q in queues)
                    {
                        Console.WriteLine($"Queue - {q.Number}:");
                        Console.WriteLine($"    NAME={q.Name}");
                        Console.WriteLine($"    PSTRATEGY={q.PollingStrategy}");
                        Console.WriteLine($"    POLLINGTIME={q.RingTimeout}");
                        Console.WriteLine($"    INTRO={q.IntroFile}");
                        Console.WriteLine($"    MOH={q.OnHoldFile}");
                        Console.WriteLine($"    AGENTS={string.Join(",", q.QueueAgents.Select(x => x.DN.Number))}");
                        Console.WriteLine($"    MANAGERS={string.Join(",", q.QueueManagers.Select(x => x.DN.Number))}");
                        Console.WriteLine($"    MAXWAIT={q.MasterTimeout}");
                        Console.WriteLine($"    NOANSWERDEST={q.ForwardNoAnswer.To}.{q.ForwardNoAnswer.Internal?.Number}.{q.ForwardNoAnswer.External}");
                        var agents =
                            q.QueueAgents
                            .Select(x => x.DN as Extension).Where(x => x != null);
                        var loggedin = agents
                            .Where(x => x.QueueStatus == QueueStatusType.LoggedIn)  
                            .Where(x => x.GetPropertyValue("LOGGED_IN_QUEUES")?.Split(',').Contains(q.Number)??true).Select(x=>x.Number).ToArray();
                        var loggedout = string.Join(",", agents.Select(x => x.Number).Except(loggedin));
                        Console.WriteLine($"    LOGGEDIN={string.Join(",", loggedin)}");
                        Console.WriteLine($"    LOGGGEDOUT={string.Join(",", loggedout)}");
                        Console.WriteLine($"    DNProperties:");
                        foreach (var p in q.GetProperties())
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
