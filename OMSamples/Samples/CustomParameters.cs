using System;
using System.Collections.Generic;
using System.Linq;
using TCX.Configuration;
namespace OMSamples.Samples
{
    [SampleCode("parameter")]
    [SampleParam("arg1", "show           | set    | delete |")]
    [SampleParam("arg2", "[partialname]  | name   | name   |")]
    [SampleParam("arg3", "               | value  |        |")]
    [SampleDescription("Updates or sets parameter value")]
    class CustomParameters : ISample
    {
        public void Run(params string[] args)
        {
            var ps = PhoneSystem.Root;
            switch (args[1])
            {
                case "set":
                    {
                        try
                        {
                            if (args.Length != 4) //more then expected
                                throw new InvalidOperationException($"Invalid command line parameters for 'set' action");
                            var paramname = args[2].ToUpperInvariant();
                            var paramvalue = args[3];
                            var previous_value = PhoneSystem.Root.GetParameterValue(args[2].ToUpperInvariant());
                            if (previous_value != null)
                            {
                                System.Console.ForegroundColor = System.ConsoleColor.Red;
                                System.Console.WriteLine($"Parameter already exist:\n{paramname}={previous_value}\n");
                                System.Console.ForegroundColor = System.ConsoleColor.Yellow;
                                System.Console.Write("Are you sure you want to update it?(y/N)");
                                var confirmation = System.Console.ReadLine();
                                System.Console.ResetColor();
                                if (confirmation != "Y" && confirmation != "y")
                                {
                                    System.Console.WriteLine($"Cancelled update of parameter {paramname}");
                                    return;
                                }
                            }
                            ps.SetParameter(paramname, paramvalue);
                            if (previous_value != null)
                                Console.WriteLine($"Parameter {paramname} has been changed from {previous_value} to {paramvalue}");
                            else
                                Console.WriteLine($"New parameter {paramname}={paramvalue} has been added");
                        }
                        finally
                        {
                            Console.ResetColor();
                        }
                    }
                    break;
                case "delete":
                    {
                        try
                        {
                            if (args.Length != 3) //more then expected
                                throw new InvalidOperationException($"Invalid command line parameters for 'delete' action");
                            var paramname = args[2].ToUpperInvariant();
                            var previous_value = PhoneSystem.Root.GetParameterValue(args[2].ToUpperInvariant());
                            if (previous_value != null)
                            {
                                System.Console.ForegroundColor = System.ConsoleColor.Yellow;
                                System.Console.Write($"Are you sure you want to delete {paramname}={previous_value}?(y/N)");
                                var confirmation = System.Console.ReadLine();
                                System.Console.ResetColor();
                                if (confirmation != "Y" && confirmation != "y")
                                {
                                    System.Console.WriteLine($"Cancelled delete action on {paramname}={previous_value}");
                                }
                                else
                                {
                                    ps.DeleteParameter(paramname);
                                    System.Console.WriteLine($"Deleted {paramname}={previous_value}");
                                }
                            }
                            else
                            {
                                System.Console.ForegroundColor = System.ConsoleColor.Yellow;
                                Console.WriteLine($"{paramname} is not found");
                            }
                        }
                        finally
                        {
                            Console.ResetColor();
                        }
                    }
                    break;
                case "notify":
                    {
                        var paramname = args[2].ToUpperInvariant();
                        var paramvalue = (args.Length >3)?args[3]:null;
                        ps.NotifyParameterUpdate(paramname, paramvalue);
                    }
                    break;
                case "show":
                    {
                        if (args.Length>3) //more then expected
                            throw new InvalidOperationException($"Invalid command line parameters for 'show' action");
                        var paramname = args.Length>2?args[2].ToUpperInvariant():null;
                        using (var paramset = ps.GetParameters().GetDisposer(x => paramname==null || x.Name.Contains(paramname)))
                        {
                            foreach (var p in paramset)
                                Console.WriteLine($"{p.Name}={p.Value} \n    DESCRIPTION:{new string(p.Description.Take(50).ToArray())}");
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown action {args[1]}");
            }
        }
    }
}
