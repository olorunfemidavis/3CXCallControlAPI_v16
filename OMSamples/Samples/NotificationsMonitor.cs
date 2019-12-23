using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using System.Threading;

namespace OMSamples.Samples
{
    [SampleCode("notifications")]
    [SampleParam("arg1", "Object type name")]
    [SampleDescription("Shows update notifications of specified data class. All notifications will be shown if arg1 is not specified")]
    class NotificationsMonitorSample : ISample
    {
        class MyListener : PsArgsEventListener
        {
            string UpdateInfo(string action, NotificationEventArgs update)
            {
                var type = update?.ConfObject.GetType();
                if (type != null)
                {
                    type = TCX.Configuration.OMClassSerializationData.Create(update?.ConfObject.GetType())?.MainInterface;
                }
                return $"{action}: UpdateRef={update?.EntityName}.{update?.RecID} - ConfObject={(type?.Name)??null}.{((IOMSnapshot)update?.ConfObject)?.ID}\n{update?.ConfObject}";
            }

            public MyListener(HashSet<string> EntityNames)
            {
                SetArgsHandler(
                                    //updated
                                    (x) => Console.WriteLine(UpdateInfo("UPDATED", x)),
                                    //inserted
                                    (x) => Console.WriteLine(UpdateInfo("INSERTED", x)),
                                    //deleted
                                    (x) => Console.WriteLine(UpdateInfo("DELETED", x)),
                                    (x)=> !EntityNames.Any()||EntityNames.Contains(x.EntityName), null);
            }
            
        }
        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            using (var listener = new MyListener(new HashSet<string>(args.Skip(1))))
            {
                while (!Program.Stop)
                {
                    Thread.Sleep(5000);
                }
            }
        }
    }
}