using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using TCX.Configuration;

namespace OMSamples
{
    public interface ISample
    {
        void Run(params string[] args);
    }

    public static class SampleStarter
    {
        private static readonly Dictionary<string, ISample> samples = new Dictionary<string, ISample>();

        static SampleStarter()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                if (typeof(ISample).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    object[] attrs = type.GetCustomAttributes(typeof(SampleCodeAttribute), true);
                    if (attrs.Length == 0)
                        continue;
                    SampleCodeAttribute code = attrs[0] as SampleCodeAttribute;
                    samples[code.Code] = (ISample)Activator.CreateInstance(type);
                }
            }
        }

        private static string GetDescription(Type type)
        {
            object[] objs = type.GetCustomAttributes(typeof(SampleDescriptionAttribute), true);
            if (objs == null || objs.Length == 0)
                return "no description ";
            return ((SampleDescriptionAttribute)objs[0]).ToString();
        }

        private static string GetWarning(Type type)
        {
            object[] objs = type.GetCustomAttributes(typeof(SampleWarningAttribute), true);
            if (objs == null || objs.Length == 0)
                return "";
            return ((SampleWarningAttribute)objs[0]).ToString();
        }

        private static int CompareParams(SampleParamAttribute x, SampleParamAttribute y)
        {
            return x.Name.CompareTo(y.Name);
        }

        private static string GetParameters(Type type)
        {
            object[] objs = type.GetCustomAttributes(typeof(SampleParamAttribute), true);
            if (objs == null || objs.Length == 0)
                return "";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Parameters:");
            List<SampleParamAttribute> lstPrms = objs.Cast<SampleParamAttribute>().ToList();
            lstPrms.Sort(CompareParams);
            foreach (SampleParamAttribute prm in lstPrms)
            {
                sb.AppendLine("\t" + prm.ToString());
            }
            return sb.ToString();
        }

        public static void StartSample(params string[] args)
        {
            if (args.Length == 0 || args[0] == "/?")
            {
                //TODO print all
                Console.WriteLine("Usage:");
                Console.WriteLine("\tOMSamples [/?]|[SampleName arg1 arg2 ...]");
                Console.WriteLine("List of samples:");
                Console.WriteLine();
                foreach (KeyValuePair<string, ISample> pair in samples)
                {
                    Console.WriteLine("SampleName: " + pair.Key);
                    Console.WriteLine("Implemented in " + pair.Value.GetType());
                    Type t = pair.Value.GetType();
                    string warning = GetWarning(t);
                    if(warning.Length > 0)
                        Console.WriteLine(warning);
                    string prms = GetParameters(t);
                    if(prms.Length > 0)
                        Console.WriteLine(prms);
                    Console.WriteLine(GetDescription(t));
                    Console.WriteLine("--------------------------------------------------------------------------------");
                }
                return;
            }

            string sampleName = args[0].Trim().ToLowerInvariant();
            try
            {
                samples[sampleName].Run(args);
            }
            catch (TCX.Configuration.Exceptions.DNNameIsNotSpecified e)
            {
                //will be thrown if name of DN is not specified while creating DN.
                Console.WriteLine("Exception: " + e);
            }
            catch (TCX.Configuration.Exceptions.ObjectAlreadyExistsException e)
            {
                //will be thrown if object already exists
                Console.WriteLine("Exception: " + e);
            }
            catch (TCX.Configuration.Exceptions.ObjectSavingException e)
            {
                //method Save() or Delete() can throw this exception in case if ObjectModel 
                //can not save or delete object by some reason (constrait violation, or connection 
                //to configuration server is not available)
                Console.WriteLine("Exception: " + e);
            }
            catch (TCX.Configuration.Exceptions.PhoneSystemException e)
            {
                //base exception for all 3CX phone system exceptions
                Console.WriteLine("Exception: " + e);
            }
            catch (Exception e)
            {
                //all other exceptions (runtime exceptions)
                Console.WriteLine("Exception: " + e);
            }
        }
    }

    public class SampleCodeAttribute : Attribute
    {
        private string code;

        public SampleCodeAttribute(string code)
        {
            this.code = code;
        }

        public string Code
        {
            get
            {
                return code;
            }
        }
    }

    public class SampleDescriptionAttribute : Attribute
    {
        private string desc;

        public SampleDescriptionAttribute(string description)
        {
            this.desc = description;
        }

        public string Description
        {
            get
            {
                return desc;
            }
        }

        public override string ToString()
        {
            if (desc == null)
                return "";
            return "Description: " + desc;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class SampleParamAttribute : Attribute
    {
        private string desc;
        private string name;

        public SampleParamAttribute(string name, string description)
        {
            this.desc = description;
            this.name = name;
        }

        public string Description
        {
            get
            {
                return desc;
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
        }
        public override string ToString()
        {
            return name + " - " + desc;
        }
    }

    public class SampleWarningAttribute : Attribute
    {
        private string warning;

        public SampleWarningAttribute(string warning)
        {
            this.warning = warning;
        }

        public string Warning
        {
            get
            {
                return warning;
            }
        }

        public override string ToString()
        {
            if (warning == null)
                return "";
            return "WARNING: " + warning;
        }
    }
}
