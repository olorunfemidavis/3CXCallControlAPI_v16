using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("omlookup")]
    [SampleDescription("Shows how to use OMLookup to make own lookup collection of objects. sample is for Extension.EmailAddress")]
    class OMLookup : ISample
    {
        public void Run(params string[] args)
        {
            var emaillookup = PhoneSystem.Root.CreateLookup(() => PhoneSystem.Root.GetExtensions(), y => y.EmailAddress, "DN");
            PhoneSystem ps = PhoneSystem.Root;
            while (!Program.Stop)
            {
                Console.WriteLine("s <string> - search for email");
                Console.WriteLine("keys - print all keys with counter of the objects");
                Console.Write("Enter cammand:");
                var command = Console.ReadLine();
                if(command.StartsWith("s "))
                {
                    foreach (var k in emaillookup.Lookup(string.Join(" ", command.Split(' ').Skip(1))))
                    {
                        Console.WriteLine($"{k}");
                    }
                }
                else if(command == "keys")
                {
                    foreach (var k in emaillookup.Keys)
                    {
                        Console.WriteLine($"'{k}'={emaillookup.Lookup(k).Count()}");
                    }
                }

            }
        }

    }
}
