﻿using YWB.AntidetectAccountsParser.Interfaces;
using YWB.AntidetectAccountsParser.Model.Accounts;
using YWB.AntidetectAccountsParser.Model.Actions;

namespace YWB.AntidetectAccountsParser.Services.Archives
{
    public class DirParser<T> : IArchiveParser<T> where T : SocialAccount
    {
        public List<string> Containers { get; set; }

        public DirParser(List<string> dirs, Microsoft.Extensions.Logging.ILoggerFactory _lf) => Containers = dirs;

        public T Parse(ActionsFacade<T> af, string dirPath)
        {
            foreach (var entry in Directory.GetFiles(dirPath,"*.*",SearchOption.AllDirectories))
            {
                foreach (var a in af.AccountActions)
                {
                    if (a.Condition(entry.ToLowerInvariant()))
                    {
                        Console.WriteLine($"{a.Message}{entry}");
                        using (var s = File.OpenRead(entry))
                        {
                            a.Action(s, af.Account);
                        }
                    }
                }
            }
            return af.Account;
        }
    }
}