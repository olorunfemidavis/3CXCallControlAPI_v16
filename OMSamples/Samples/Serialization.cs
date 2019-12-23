using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TCX.Configuration;

namespace OMSamples.Samples
{
    [SampleCode("serialization")]
    [SampleWarning("Deserialization modifies configuration (adds or modify objects)")]
    [SampleDescription("Shows how to use serialization subsystem of Object model")]
    [SampleParam("arg1", "serialize    | deserialize         ")]
    [SampleParam("arg2", "xmlfile      | xmlfile             ")]
    [SampleParam("arg3", "<objtype>    |                     ")]
    class Serialization : ISample
    {
        public bool AdjustMethodSerialize(XElement objectNode, IOMSnapshot theObject, IOMSnapshot theOwner, PropertyInfo ownerProperty)
        {
            Console.WriteLine($"{objectNode.Name} - Owner:{theOwner} - Object:{theObject}.{ownerProperty?.Name}");
            return true;
        }
        public bool AdjustMethodDeserialize(XElement objectNode, IOMSnapshot theObject, IOMSnapshot theOwner, PropertyInfo ownerProperty)
        {
            Console.WriteLine($"{objectNode.Name} - Owner:{theOwner} - Object:{theObject}.{ownerProperty?.Name}");
            return true;
        }
        public void Run(params string[] args)
        {
            var context = new SerializationExtension.Context();
            switch (args[1])
            {
                case "serialize":
                    {
                        var collectionobject = typeof(PhoneSystem).GetMethod("GetAll").MakeGenericMethod(typeof(PhoneSystem).Assembly.GetType("TCX.Configuration." + args[3])).Invoke(PhoneSystem.Root, null);
                        context.AdjustHandler = AdjustMethodSerialize;
                        new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                            ((IEnumerable<IOMSnapshot>)collectionobject).SerializeObjectArray($"{args[3]}s", context)).Save(args[2]);
                     }
                    break;
                case "deserialize":
                default:
                    throw new ArgumentException($"{args[1]} option is not implemented for {args[0]} sample");
            }
        }
    }
}
