using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("add_phone_model")]
    [SampleDescription("Creates PhoneModel object which describes capability for specific user agent")]
    class AddPhoneModelSample : ISample
    {
        public void Run(params string[] args)
        {
            PhoneModel a = PhoneSystem.Root.CreatePhoneModel();
            a.CanBlankSDP = true;
            a.CanReceiveOnly = true;
            a.CanReinvite = true;
            a.CanReplaces = true;
            a.Manufacturer = "MyPhone";
            a.ModelName = "MyModel";
            a.Revision = "123";
            a.UserAgentIdentifier = "my user agent";
            a.Save();
        }
    }
}