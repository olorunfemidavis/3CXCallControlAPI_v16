using System;
using System.Collections.Generic;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("invoke")]
    [SampleParam("arg1", "command which should be invoked")]
    [SampleParam("arg2, arg3 and so on", "additional parameters for Invoke method - each additional parameter should be set as parameter_name=parameter_value")]
    [SampleDescription("Shows how to use PhoneSystem.Invoke() method")]
    class InvokeSample : ISample
    {
        public void Run(params string[] args)
        {
            string command_name = args[1];
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 2; i < args.Length; i++)
            {
                string[] a = args[i].Split(new char[] { '=' });
                if (a.Length >= 2)
                {
                    parameters.Add(a[0], String.Join("=", a, 1, a.Length - 1));
                }
                else
                {
                    System.Console.WriteLine(args[i] + " ignored");
                }
            }

            try
            {
                PhoneSystem.Root.InvokeCommand(command_name, parameters);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }
    }
}