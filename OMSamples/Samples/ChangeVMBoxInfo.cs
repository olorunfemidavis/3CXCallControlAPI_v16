using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("change_vmbox_info")]
    [SampleParam("arg1", "extension number")]
    [SampleDescription("Sets voicemail box information for the specified extension. \nNumber of messages is hardcoded and set to 1 new message and 2 messages in total.")]
    class ChangeVMBoxInfoSample : ISample
    {
        public void Run(params string[] args)
        {
            DN dn = PhoneSystem.Root.GetDNByNumber(args[1]);
            if (dn !=null&&(dn is Extension))
            {
                VMBInformation vbm = new VMBInformation(2, 1);
                dn.VoiceMailBox = vbm;
            }
            else
            {
                Console.WriteLine(args[1] + " is not an extension");
            }
        }
    }
}