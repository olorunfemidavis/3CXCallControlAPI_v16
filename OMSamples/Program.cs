using System;
using System.Collections.Generic;
using System.Text;
using TCX.Configuration;
using TCX.PBXAPI;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Linq;


namespace OMSamples
{
    class Program
    {
        static Dictionary<string, Dictionary<string, string>> iniContent =
            new Dictionary<string, Dictionary<string, string>>(
                StringComparer.InvariantCultureIgnoreCase);

        public static bool Stop { get; private set; }
        static void ReadConfiguration(string filePath)
        {
            var content = File.ReadAllLines(filePath);
            Dictionary<string, string> CurrentSection = null;
            string CurrentSectionName = null;
            for (int i = 1; i < content.Length + 1; i++)
            {
                var s = content[i - 1].Trim();
                if (s.StartsWith("["))
                {
                    CurrentSectionName = s.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    CurrentSection = iniContent[CurrentSectionName] = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                }
                else if (CurrentSection != null && !string.IsNullOrWhiteSpace(s) && !s.StartsWith("#") && !s.StartsWith(";"))
                {
                    var res = s.Split("=").Select(x => x.Trim()).ToArray();
                    CurrentSection[res[0]] = res[1];
                }
                else
                {
                    //Console.WriteLine($"Ignore Line {i} in section '{CurrentSectionName}': '{s}' ");
                }
            }
            instanceBinPath = Path.Combine(iniContent["General"]["AppPath"], "Bin");
        }
        static void Bootstrap(string[] args)
        {
            PhoneSystem.CfgServerHost = "127.0.0.1";
            PhoneSystem.CfgServerPort = int.Parse(iniContent["ConfService"]["ConfPort"]);
            PhoneSystem.CfgServerUser = iniContent["ConfService"]["confUser"];
            PhoneSystem.CfgServerPassword = iniContent["ConfService"]["confPass"];
            var ps = PhoneSystem.Reset(
                PhoneSystem.ApplicationName + new Random(Environment.TickCount).Next().ToString(),
                "127.0.0.1",
                int.Parse(iniContent["ConfService"]["ConfPort"]),
                iniContent["ConfService"]["confUser"],
                iniContent["ConfService"]["confPass"]);
            ps.WaitForConnect(TimeSpan.FromSeconds(30));
            try
            {
                SampleStarter.StartSample(args);
            }
            finally
            {
                ps.Disconnect();
            }
        }

        static string instanceBinPath;

        static void Main(string[] args)
        {
            //RoslynCore.EmitDemo.GenerateAssembly();
            Console.OutputEncoding = new UnicodeEncoding();
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            try
            {
                var filePath = @".\3CXPhoneSystem.ini";
                if (!File.Exists(filePath))
                {
                    //this code expects 3CXPhoneSystem.ini in current directory.
                    //it can be taken from the installation folder (find it in Program Files/3CXPhone System/instance1/bin for in premiss installation)
                    //or this application can be run with current directory set to location of 3CXPhoneSystem.ini

                    //v14 (cloud and in premiss) installation has changed folder structure.
                    //3CXPhoneSystem.ini which contains connectio information is located in 
                    //<Program Files>/3CX Phone System/instanceN/Bin folder.
                    //in premiss instance files are located in <Program Files>/3CX Phone System/instance1/Bin
                    throw new Exception("Cannot find 3CXPhoneSystem.ini");
                }
                ReadConfiguration(filePath);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                Bootstrap(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            //
            args.Cancel = true;
            Stop = true;
        }
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            try
            {
                return Assembly.LoadFrom(Path.Combine(instanceBinPath, name + ".dll"));
            }
            catch
            {
                return null;
            }
        }
    }
}
