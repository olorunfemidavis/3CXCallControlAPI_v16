using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCX.Configuration;
using System.Net;
using System.Threading;

namespace OMSamples.Samples
{
    public class BlackListEntryHolder
    {
        int _hashCode;
        string _Index;
        int _MaskIndex;
        int _BlockType;
        DateTime _Expires;
        BlackListEntry ble_;
        internal static int GetMaskIndex(byte[] theMask)
        {
            int maskIndex = 0;
            for (int i = 3; i > 0; i--)
            {
                if (theMask[i] == 255)
                    break;
                else if (theMask[i] == 0)
                    maskIndex+=8;
                else
                {
                    for (int b = 0; b < 8; b++)
                    {
                        if ((theMask[i] & ((byte)(0x1 << b))) != 0)
                        {
                            break;
                        }
                        else
                        {
                            maskIndex++;
                        }
                    }
                }
            }
            return maskIndex;
        }

        internal BlackListEntryHolder(BlackListEntry ble)
        {
            _hashCode = ble.GetHashCode();
            byte[] addr = IPAddress.Parse(ble.IPAddr).GetAddressBytes();
            string mask = ble.IPMask;
            if (mask == "")
                mask = "255.255.255.255";
            byte[] maskaddr = IPAddress.Parse(mask).GetAddressBytes();
            if (addr.Length !=4 || addr.Length != maskaddr.Length)
                throw new ArgumentException("Address/mask is not IP v4");
            _MaskIndex = GetMaskIndex(maskaddr);
            for (int i = 0; i < addr.Length; i++ )
            {
                addr[i] &= maskaddr[i];
            }
            _Index = String.Format("{0}.{1}.{2}.{3}", addr[0], addr[1], addr[2], addr[3]);
            _Expires = ble.ExpiresAt;
            _BlockType = ble.BlockType;
            ble_ = ble;
        }

        internal BlackListEntry Obj
        {
            get
            {
                return ble_;
            }
        }

        public override bool Equals(object obj)
        {
            BlackListEntryHolder a = obj as BlackListEntryHolder;
            if (a != null)
                return (_hashCode != 0) && (_hashCode == a._hashCode);
            return false;
        }
        public override int GetHashCode()
        {
            return _hashCode;
        }
        public string Index
        {
            get
            {
                return _Index;
            }
        }

        public int MaskIndex
        {
            get
            {
                return _MaskIndex;
            }
        }
        public DateTime Expires
        {
            get
            {
                return _Expires;
            }
        }
        public int BlockType
        {
            get
            {
                return _BlockType;
            }
        }
    };
    
    public class BlackListChecker
    {
        static public readonly SortedDictionary<int, Dictionary<string, List<BlackListEntryHolder>>> fastmap = new SortedDictionary<int, Dictionary<string, List<BlackListEntryHolder>>>();
        static public readonly byte[][] maskarray = new byte[33][];
        static BlackListChecker()
        {
            byte[] mask = new byte[]{0xff, 0xff, 0xff, 0xff};
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    mask[3 - i] = (byte)(0xFF << j);
                    maskarray[i * 8 + j] = mask.ToArray();
                }
                mask[3 - i] = 0;
            }
            maskarray[32] = mask.ToArray();
        }

        public void Add(BlackListEntryHolder beh)
        {
            Dictionary<string, List<BlackListEntryHolder>> value;
            lock (fastmap)
            {
                if (!fastmap.TryGetValue(beh.MaskIndex, out value))
                {
                    value = new Dictionary<string, List<BlackListEntryHolder>>();
                    fastmap[beh.MaskIndex] = value;
                }
                List<BlackListEntryHolder> value2;
                if (!value.TryGetValue(beh.Index, out value2))
                {
                    value2 = new List<BlackListEntryHolder>();
                    value[beh.Index] = value2;
                }
                if (beh.BlockType==1)
                    value2.Insert(0, beh);
                else
                    value2.Add(beh);
            }
        }

        public void Remove(BlackListEntryHolder beh)
        {
            lock (fastmap)
            {
                Dictionary<string, List<BlackListEntryHolder>> value;
                List<BlackListEntryHolder> value2;
                if (fastmap.TryGetValue(beh.MaskIndex, out value) && value.TryGetValue(beh.Index, out value2))
                {
                    value2.Remove(beh);
                    if (value2.Count == 0)
                    {
                        value.Remove(beh.Index);
                        if (value.Count == 0)
                            fastmap.Remove(beh.MaskIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Report status of the IP in blacklist
        /// </summary>
        /// <param name="theAddress">IP address</param>
        /// <returns>
        /// -1 - status is not defined<br>
        /// any other value is the status of the IP or subnet<br>
        /// Currently defined:
        /// 0 - this address is in black list
        /// 1 - this address is in white list should not be blocked
        /// </returns>
        public int IsBlackListed(string address)
        {
            return IsBlackListed(IPAddress.Parse(address).GetAddressBytes());
        }

        /// <summary>
        /// Report status of the IP in blacklist
        /// </summary>
        /// <param name="theAddress">IP address</param>
        /// <returns>
        /// -1 - status is not defined<br>
        /// any other value is the status of the IP or subnet<br>
        /// Currently defined:
        /// 0 - this address is in black list
        /// 1 - this address is in white list should not be blocked
        /// </returns>
        public int IsBlackListed(IPAddress theaddr)
        {
            return IsBlackListed(theaddr.GetAddressBytes());
        }
        /// <summary>
        /// Report status of the IP in blacklist
        /// </summary>
        /// <param name="theAddress">IP address</param>
        /// <returns>
        /// -1 - status is not defined<br>
        /// any other value is the status of the IP or subnet<br>
        /// Currently defined:
        /// 0 - this address is in black list
        /// 1 - this address is in white list should not be blocked
        /// </returns>
        public int IsBlackListed(byte[] theAddress)
        {
            DateTime tocheck = DateTime.UtcNow;
            lock(fastmap)
            {
                foreach (var mask in fastmap)
                {
                    byte[] addr = theAddress.ToArray();
                    if (mask.Key > 0)
                    {
                        byte[] themask = maskarray[mask.Key];
                        for (int i = 0; i < theAddress.Length; i++ )
                        {
                            addr[i] &= themask[i];
                        }
                    }
                    List<BlackListEntryHolder> theList;
                    if (mask.Value.TryGetValue(String.Format("{0}.{1}.{2}.{3}", addr[0], addr[1], addr[2], addr[3]), out theList))
                    {
                        foreach(var d in theList)
                        {
                            if(d.BlockType==1)
                                return 1;
                            if(tocheck<d.Expires&&d.BlockType==0)
                            {
                                return 0;
                            }
                        }
                    }
                }
            }
            return -1;
        }

        public BlackListEntry InsertOrUpdateEntry(string theaddr, string themask, TimeSpan blockingtime, string description)
        {
            DateTime newDate = DateTime.UtcNow + blockingtime;
            BlackListEntryHolder theObj = null;
            byte[] theMask = IPAddress.Parse(themask).GetAddressBytes();
            byte[]theAddress = IPAddress.Parse(theaddr).GetAddressBytes();
            for (int i = 0; i < theAddress.Length; i++)
            {
                theAddress[i] &= theMask[i];
            }
            int maskIndex = BlackListEntryHolder.GetMaskIndex(theMask);
            lock (fastmap)
            {
                Dictionary<string, List<BlackListEntryHolder>> value;
                List<BlackListEntryHolder> objList;
                if (fastmap.TryGetValue(maskIndex, out value) && value.TryGetValue(String.Format("{0}.{1}.{2}.{3}", theAddress[0], theAddress[1], theAddress[2], theAddress[3]), out objList))
                {
                    if (objList != null && objList.Count > 0)
                    {
                        foreach (var be in objList)
                        {
                            if ((theObj == null || theObj.Expires < be.Expires) && be.Obj.IPAddr == theaddr && be.Obj.IPMask == themask && be.Obj.BlockType == 0)
                            {
                                theObj = be;
                            }
                        }
                        if (theObj != null && theObj.Expires >= newDate)
                        {
                            return null;
                        }
                    }
                }
            }
            BlackListEntry theEntry;
            if (theObj == null)
            {
                theEntry = PhoneSystem.Root.CreateBlackListEntry();
                theEntry.IPAddr = theaddr;
                theEntry.IPMask = themask;
                theEntry.Description = description;
            }
            else
            {
                theEntry = theObj.Obj.Clone() as BlackListEntry;
            }
            theEntry.ExpiresAt = newDate;
            try
            {
                theEntry.Save();
            }
            catch
            {
                theEntry = PhoneSystem.Root.CreateBlackListEntry();
                theEntry.IPAddr = theaddr;
                theEntry.IPMask = themask;
                theEntry.Description = description;
                theEntry.ExpiresAt = newDate;
                theEntry.Save();
            }
            return theEntry;
        }

        public List<BlackListEntryHolder> BlockedBy(string address, string mask)
        {
            return BlockedBy(IPAddress.Parse(address).GetAddressBytes(), IPAddress.Parse(mask).GetAddressBytes());
        }

        public List<BlackListEntryHolder> BlockedBy(IPAddress address, IPAddress mask)
        {
            return BlockedBy(address.GetAddressBytes(), mask.GetAddressBytes());
        }

        public List<BlackListEntryHolder> BlockedBy(byte[] theAddress, byte[] theMask)
        {
            lock(fastmap)
            {
                int maskIndex = BlackListEntryHolder.GetMaskIndex(theMask);

                for (int i = 0; i < theAddress.Length; i++)
                {
                    theAddress[i] &= theMask[i];
                }

                Dictionary<string, List<BlackListEntryHolder>> value;
                List<BlackListEntryHolder> theList;
                if (fastmap.TryGetValue(maskIndex, out value) && value.TryGetValue(String.Format("{0}.{1}.{2}.{3}", theAddress[0], theAddress[1], theAddress[2], theAddress[3]), out theList))
                {
                    return theList;
                }
            }
            return null;
        }
    }



    public class BlackList
    {
        static public readonly BlackListChecker blc_ = new BlackListChecker();
        public BlackListChecker Checker
        {
            get
            {
                return blc_;
            }
        }

        private System.Collections.Generic.Dictionary<BlackListEntry, BlackListEntryHolder> BuildNewBlackList()
        {
            var retval = new System.Collections.Generic.Dictionary<BlackListEntry, BlackListEntryHolder>();
            BlackListEntry[] thearr = PhoneSystem.Root.GetAll<BlackListEntry>();
            foreach(var a in thearr)
            {
                try
                {
                    BlackListEntryHolder tmp = new BlackListEntryHolder(a);
                    retval[a] = tmp;
                    blc_.Add(tmp);
                }
                catch
                {
                    //just ignore
                }
            }
            return retval;
        }

        public BlackList()
        {
            lock (this)
            {
                PhoneSystem.Root.Updated += ps_Updated;
                PhoneSystem.Root.Inserted += ps_Inserted;
                PhoneSystem.Root.Deleted += ps_Deleted;
                the_set = BuildNewBlackList();
            }
        }

        public BlackListEntry InsertOrUpdateEntry(string theaddr, TimeSpan blockingtime, string description)
        {
            return InsertOrUpdateEntry(theaddr, "255.255.255.255", blockingtime, description);
        }

        public BlackListEntry InsertOrUpdateEntry(string theaddr, string themask, TimeSpan blockingtime, string description)
        {
            return blc_.InsertOrUpdateEntry(theaddr, themask, blockingtime, description);
        }

        public BlackListEntry InsertOrUpdateEntry(IPAddress theaddr, TimeSpan blockingtime, string description)
        {
            return InsertOrUpdateEntry(theaddr.ToString(), "255.255.255.255", blockingtime, description);
        }
        /// <summary>
        /// Inserts or updates existing entry
        /// </summary>
        /// <param name="theaddr">adress</param>
        /// <param name="themask">mask</param>
        /// <param name="blockingtime">How long this addresses should be blocked since now</param>
        /// <returns></returns>
        public BlackListEntry InsertOrUpdateEntry(IPAddress theaddr, IPAddress themask, TimeSpan blockingtime, string description)
        {
            return InsertOrUpdateEntry(theaddr.ToString(), themask.ToString(), blockingtime, description);
        }

        System.Collections.Generic.Dictionary<BlackListEntry, BlackListEntryHolder> the_set = new System.Collections.Generic.Dictionary<BlackListEntry, BlackListEntryHolder>();
        public void ps_Inserted(object sender, NotificationEventArgs e)
        {
            if (e.EntityName == "BLACKLIST")
            {
                BlackListEntry entry = e.ConfObject as BlackListEntry;
                if (entry != null)
                {
                    lock (this)
                    {
                    }
                    //Console.WriteLine("Added: {0}[{1}]. expires at {2}", entry.IPAddr, entry.IPMask, entry.ExpiresAt.ToLocalTime());
                    BlackListEntryHolder val;
                    BlackListEntryHolder tmp = new BlackListEntryHolder(entry);
                    blc_.Add(tmp);
                    if (the_set.TryGetValue(entry, out val))
                    {
                        blc_.Remove(val);
                        the_set.Remove(entry);
                    }
                    the_set[entry] = tmp;
                }
            }
        }

        public void ps_Updated(object sender, NotificationEventArgs e)
        {
            if (e.EntityName == "BLACKLIST")
            {
                BlackListEntry entry = e.ConfObject as BlackListEntry;
                if (entry != null)
                {
                    lock (this)
                    {
                    }
                    //Console.WriteLine("Updated: {0}[{1}]. expires at {2}", entry.IPAddr, entry.IPMask, entry.ExpiresAt.ToLocalTime());
                    BlackListEntryHolder val;
                    if (the_set.TryGetValue(entry, out val))
                    {
                        blc_.Remove(val);
                        the_set.Remove(entry);
                    }
                    BlackListEntryHolder tmp = new BlackListEntryHolder(entry);
                    blc_.Add(tmp);
                    the_set[entry] = tmp;
                }
            }
        }

        public void ps_Deleted(object sender, NotificationEventArgs e)
        {
            if (e.EntityName == "BLACKLIST")
            {
                BlackListEntry entry = e.ConfObject as BlackListEntry;
                if (entry != null)
                {
                    lock (this)
                    {
                    }
                    //Console.WriteLine("Removed: {0}[{1}]. expires at {2}", entry.IPAddr, entry.IPMask, entry.ExpiresAt.ToLocalTime());
                    BlackListEntryHolder val;
                    if (the_set.TryGetValue(entry, out val))
                    {
                        blc_.Remove(val);
                        the_set.Remove(entry);
                    }
                }
            }
        }
    };

    [SampleCode("blacklist_monitor")]
    [SampleDescription("Sample of IP Black List manager")]

    class BlackListMonitor : ISample
    {
        void FillBlacklist(BlackList a)
        {
            byte[] addrbytes = new byte[] { 192, 168, 0, 0 };
            for (byte i = 1; i < 5; i++)
            {
                addrbytes[2] = i;
                for (byte j = 1; j < 255; j++)
                {
                    addrbytes[3] = j;
                    a.InsertOrUpdateEntry(String.Format("{0}.{1}.{2}.{3}", addrbytes[0], addrbytes[1],addrbytes[2],addrbytes[3]), "255.255.255.0", new TimeSpan(0,30,0), "Test blocking");
                }
                Console.Write('.');
            }
            Console.WriteLine();
        }

        public void Run(params string[] args)
        {
            PhoneSystem ps = PhoneSystem.Root;
            String filter = null;
            if (args.Length > 1)
                filter = args[1];
            BlackList a = new BlackList();
            DateTime startAt = DateTime.Now;
            FillBlacklist(a);
            Console.WriteLine("Added entries. time elapsed {0}", DateTime.Now - startAt);
            startAt = DateTime.Now;
            for (int i = 0; i < 10000; i++)
            {
                if (a.Checker.IsBlackListed("10.172.0.103") == 0)
                    Console.Write('X');
                else
                    Console.Write('.');
                if(a.Checker.IsBlackListed("192.168.4.254")==0)
                    Console.Write('X');
                else
                    Console.Write('.');
            }
            Console.WriteLine("Checked 20000 times. time elabpsed {0}", DateTime.Now - startAt);
            while (true)
            {
                if (a.Checker.IsBlackListed("10.172.0.103") == 0)
                    Console.Write('X');
                else
                    Console.Write('.');
                if (a.Checker.IsBlackListed("192.168.1.83")==0)
                    Console.Write('X');
                else
                    Console.Write('.');
                Thread.Sleep(1000);
            }
        }
    }
}
