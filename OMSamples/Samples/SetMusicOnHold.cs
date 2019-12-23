using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using System.IO;
namespace OMSamples.Samples
{
    [SampleCode("musiconhold")]
    [SampleParam("arg1...argN", "music on hold source in form ENTITY=source where ENTITY is PARAMETER name where source is stored (see array of names in code) or DN.Number of the queue. if no parametest provided - shows full list of configured objects and checks validity of the source")]
    [SampleWarning("modifies configuration.")]
    [SampleDescription("shows how to change music on hold settings")]
    class MusicOnHold : ISample
    {
        string[] MOHParameters =
        {
        "MUSICONHOLDFILE",
        "MUSICONHOLDFILE1",
        "MUSICONHOLDFILE2",
        "MUSICONHOLDFILE3",
        "MUSICONHOLDFILE4",
        "MUSICONHOLDFILE5",
        "MUSICONHOLDFILE6",
        "MUSICONHOLDFILE7",
        "MUSICONHOLDFILE8",
        "MUSICONHOLDFILE9",
        "CONFPLACE_MOH_SOURCE",
        "IVR_MOH_SOURCE",
        "PARK_MOH_SOURCE"
        };

        public void Run(params string[] args_in)
        {
            var args = args_in.Skip(1).ToArray();
            if (args.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var allParams = args.Select(x =>
                {
                    var stringset = x.Split(new char[] { '=' });
                    var value = string.Join("=", stringset.Skip(1).ToArray());
                    value = (value != String.Empty || x.EndsWith("=")) ? value : null;
                    if (value == null)
                    {
                        Console.WriteLine($"Malformed argument {x} should be <ENTITY>=<VALUE>");
                    }
                    return new KeyValuePair<string, string>(stringset.First().Trim(), value);
                }).Where(y => y.Value != null);
                Console.ResetColor();
                var FilesFolder = PhoneSystem.Root.GetParameterValue("IVRPROMPTPATH"); //base folder for files
                var PlaylistFolder = Path.Combine(FilesFolder, "Playlist"); //base folder for playlists
                foreach (var a in allParams)
                {
                    var name = a.Key;
                    var value = a.Value; //can be folder of configured playlist or the path to the file
                    if (value != string.Empty)
                    {
                        if (!File.Exists(Path.Combine(FilesFolder, value)))
                        {
                            AudioFeed playlist = null;
                            if (!Directory.Exists(Path.Combine(PlaylistFolder, value)) || (playlist = PhoneSystem.Root.GetAllAudioFeeds().GetDisposer(x => x.Source == value).FirstOrDefault()) == null) //not found even playlist
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Source {value} is not found to update {name}");
                                Console.ResetColor();
                                continue;
                            }
                            //we need to set pipe reference
                            Console.WriteLine($"Source for {name} is the playlist {value} ({playlist.Name})");
                            value = @"\\.\pipe\" + playlist.Name;
                        }
                        else
                        {
                            value = Path.Combine(FilesFolder, value);
                        }
                    }
                    var q = PhoneSystem.Root.GetDNByNumber(name) as Queue;
                    var p = PhoneSystem.Root.GetParameterValue(name);
                    if (p != null && MOHParameters.Contains(name)) //parameter
                    {
                        PhoneSystem.Root.SetParameter(name, value);
                        Console.WriteLine($"updated PARAM.{name}={value}");
                    }
                    else if (q != null && p == null)
                    {
                        try
                        {
                            q.OnHoldFile = value;
                            q.Save();
                            Console.WriteLine($"updated queue music on hold QUEUE.{name}={value}");
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Failed to update music on hold on QUEUE.{name}");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Invalid entity {name}. Should be name of a custom parameter or queue number");
                        Console.ResetColor();
                    }
                }
            }
            Console.WriteLine("All MOH users:");
            var allMOHSources = MOHParameters.Select(x => new KeyValuePair<string, string>("PARAM." + x, PhoneSystem.Root.GetParameterValue(x))).Where(z => z.Value != null)
                .Concat(PhoneSystem.Root.GetQueues().Select(y => new KeyValuePair<string, string>("QUEUE." + y.Number, y.OnHoldFile)));
            foreach (var a in allMOHSources)
            {
                if (a.Value.StartsWith(@"\\.\pipe\")) // it should be AudioFeed reference
                {
                    var res = PhoneSystem.Root.GetAllAudioFeeds().FirstOrDefault(x => x.Name == a.Value.Substring(9));
                    if (res == null) //not found
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{a.Key}={a.Value} UNDEFINED PLAYLIST REFERENCE");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{a.Key}=PL[{res.Source}]({res.Name})");
                    }
                }
                else
                {
                    bool exists = File.Exists(a.Value);
                    if (!exists)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else
                        Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{a.Key}=FILE[{a.Value}]"+(exists?"":" NOT EXIST"));
                }
                Console.ResetColor();
            }
        }
    }
}