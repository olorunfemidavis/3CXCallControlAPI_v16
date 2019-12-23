using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TCX.Configuration;

namespace OMSamples.Samples
{

    class DNListener : PsTypeEventListener<DN>
    {
        public static string DNInfo(DN dn)
        {
            if (dn == null)
            {
                return "<NULL>";
            }
            return $"{dn.GetType().Name}.{dn.Number}[ID/Hash={dn.ID}]";
        }

        public DNListener()
            : base("DN")
        {
            SetTypeHandler(
                                //updated
                                (x) => Console.WriteLine($"UPDATED {DNInfo(x)}"),
                                //inserted
                                (x) => Console.WriteLine($"INSERTED {DNInfo(x)}"),
                                //deleted
                                (x) => Console.WriteLine($"DELETED {DNInfo(x)}"),
                                null, null);
        }
    }
    class RegistrationListener : PsTypeEventListener<DN>
    {
        string RegistrationInfo(DN dn)
        {
            if (dn == null)
            {
                return "<NULL>";
            }
            return $"{DNListener.DNInfo(dn)}=Registrar\n[\n{string.Join("\n    ", dn.GetRegistrarContactsEx().Select(x => $"ID/Hash={x.ID} - {x.Contact}"))}\n]";
        }

        public RegistrationListener()
            : base("REGISTRATION")
        {
            SetTypeHandler(
                                //updated
                                (x) => Console.WriteLine($"REGISTRATION UPDATED {RegistrationInfo(x)}"),
                                //inserted
                                (x) => Console.WriteLine($"REGISTRATION INSERTED {RegistrationInfo(x)}"),
                                //deleted
                                (x) => Console.WriteLine($"REGISTRATION DELETED {RegistrationInfo(x)}"),
                                null, null);
        }
    }

    class VoiceMailBoxListener : PsTypeEventListener<DN>
    {
        string VoiceMailInfo(DN dn)
        {
            return $"{DNListener.DNInfo(dn)}=VMBOX({dn?.VoiceMailBox.New}/{dn?.VoiceMailBox.Total})";
        }
        public VoiceMailBoxListener()
            : base("VMBOXINFO")
        {
            SetTypeHandler(
                                //updated
                                (x) => Console.WriteLine($"UPDATED {VoiceMailInfo(x)}"),
                                //inserted
                                (x) => Console.WriteLine($"REGISTRATION INSERTED {VoiceMailInfo(x)}"),
                                //deleted
                                (x) => Console.WriteLine($"REGISTRATION DELETED {VoiceMailInfo(x)}"),
                                null, null);
        }
    }

    [SampleCode("dn_monitor")]
    [SampleDescription("Shows how to listen for DN updates")]
    class DNmonitorSample : ISample
    {
        public void Run(params string[] args)
        {
            using (var disposer = new PsArgsEventListener[] {
            new DNListener(),
            new RegistrationListener(),
            new VoiceMailBoxListener()
            }.GetDisposer())
            {
                while (true)
                    Thread.Sleep(1000);
            }
        }
    }
}