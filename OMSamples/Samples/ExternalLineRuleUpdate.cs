using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using System.Threading;

namespace OMSamples.Samples
{
    [SampleCode("ext_line_rule_update")]
    [SampleParam("arg1", "Virtual extension number of the line")]
    [SampleWarning("This sample will modify destination of existing rules. Line should be recreated after this test")]
    [SampleDescription("This sample shows how to change destination of ExternalLineRule")]
    class ExternalLineRuleUpdateSample : ISample
    {
        public void Run(params string[] args)
        {
            ExternalLine _extLine = PhoneSystem.Root.GetDNByNumber(args[1]) as ExternalLine;
            if (_extLine != null)
            {
                bool bEndCall = false;
                for (; ; )
                {
                    var lineRule = _extLine.RoutingRules.Last();
                    if(lineRule.Conditions.Condition.Type == RuleConditionType.ForwardAll)
                    {
                        lineRule.ForwardDestinations.OfficeHoursDestination = new DestinationStruct(DestinationType.Extension, PhoneSystem.Root.GetExtensions()[0], null);
                        lineRule.Hours.HoursType = PhoneSystem.Root.GetRuleHourTypeByType(RuleHoursType.AllHours);
                    }
                    _extLine.Save();
                    bEndCall = !bEndCall;
                    Thread.Sleep(5000);
                }
            }
            else
            {
                Console.WriteLine(args[1] + " is not ExternalLine");
            }
        }
    }
}
