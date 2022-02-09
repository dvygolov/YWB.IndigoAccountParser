﻿using System.IO.Compression;
using YWB.AntidetectAccountsParser.Interfaces;
using YWB.AntidetectAccountsParser.Model.Accounts;
using YWB.AntidetectAccountsParser.Model.Actions;

namespace YWB.AntidetectAccountsParser.Services.Archives
{
    public class ZipArchiveParser<T>:IArchiveParser<T> where T:SocialAccount
    {
        public List<string> Containers { get; set; }

        public ZipArchiveParser(List<string> archives) => Containers = archives;

        public T Parse(ActionsFacade<T> af, string filePath)
        {
            using (var archive = ZipFile.OpenRead(filePath))
            {
                foreach (var entry in archive.Entries)
                {
                    foreach (var a in af.AccountActions)
                    {
                        if (a.Condition(entry.FullName.ToLowerInvariant()))
                        {
                            Console.WriteLine($"{a.Message}{entry.FullName}");
                            using (var s = entry.Open())
                            {
                                a.Action(s,af.Account);
                            }
                        }
                    }
                }
            }
            return af.Account;
        }
    }
}