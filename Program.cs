﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YWB.AntidetectAccountParser.Helpers;
using YWB.AntidetectAccountParser.Model.Accounts;
using YWB.AntidetectAccountParser.Services.Browsers;
using YWB.AntidetectAccountParser.Services.Monitoring;
using YWB.AntidetectAccountParser.Services.Parsers;
using YWB.AntidetectAccountParser.Services.Proxies;

namespace YWB.AntidetectAccountParser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Antidetect Accounts Parser v5.0b Yellow Web (https://yellowweb.top)");
            Console.WriteLine("If you like this software, please, donate!");
            Console.WriteLine("WebMoney: Z182653170916");
            Console.WriteLine("Bitcoin: bc1qqv99jasckntqnk0pkjnrjtpwu0yurm0qd0gnqv");
            Console.WriteLine("Ethereum: 0xBC118D3FDE78eE393A154C29A4545c575506ad6B");
            await Task.Delay(3000);
            Console.WriteLine();

            var apf = new AccountsParserFactory();
            var parser = apf.CreateParser();
            var accounts = parser.Parse();
            if (accounts.Count() == 0)
            {
                Console.WriteLine("Couldn't find any accounts to import(((");
                return;
            }

            var proxyProvider = new FileProxyProvider();
            proxyProvider.SetProxies(accounts);

            int answer = 0;
            if (accounts.All(a => a is FacebookAccount))
            {
                Console.WriteLine("What do you want to do?");
                Console.WriteLine("1. Create Profiles in an Antidetect Browser");
                Console.WriteLine("2. Import accounts to FbTool/Dolphin");
                answer = YesNoSelector.GetMenuAnswer(2);
            }
            else
                answer = 1;

            if (answer == 1)
            {
                Console.WriteLine("Choose your antidetect browser:");
                var browsers = new Dictionary<string, Func<AbstractAntidetectApiService>>
                {
                    {"Indigo",()=> new IndigoApiService() },
                    {"Dolphin Anty",()=>new DolphinAntyApiService() },
                    {"AdsPower",()=>new AdsPowerApiService() }
                };
                var selectedBrowser = SelectHelper.Select(browsers, b => b.Key).Value();

                await selectedBrowser.ImportAccountsAsync(accounts.ToList());

                if (accounts?.All(a => a is FacebookAccount && !string.IsNullOrEmpty((a as FacebookAccount).Token)) ?? false)
                {
                    var add = YesNoSelector.ReadAnswerEqualsYes(
                        "All accounts have access tokens! Do you wand to add them to Dolphin/FbTool?");
                    if (add)
                    {
                        await ImportToMonitoringService(accounts.Cast<FacebookAccount>().ToList());
                    }
                }
            }
            else if (answer == 2)
            {
                var fbAccounts = accounts.Cast<FacebookAccount>().ToList();
                if (fbAccounts.All(a => !string.IsNullOrEmpty(a.Token)))
                {
                    await ImportToMonitoringService(fbAccounts);
                }
                else if (fbAccounts.Any(a => !string.IsNullOrEmpty(a.Token)))
                {
                    var anwser = YesNoSelector.ReadAnswerEqualsYes("Not all accounts have Facebook Access Tokens! Import only those, that have tokens?");
                    if (anwser)
                        await ImportToMonitoringService(fbAccounts.Where(a => !string.IsNullOrEmpty(a.Token)).ToList());
                }
                else
                    Console.WriteLine("No accounts with access tokens found!((");
            }


            Console.WriteLine("All done! Press any key to exit... and don't forget to donate ;-)");
            Console.ReadKey();
        }

        private static async Task ImportToMonitoringService(List<FacebookAccount> accounts)
        {
            if (accounts.All(a => string.IsNullOrEmpty(a.Name)))
            {
                Console.Write("Enter account name prefix:");
                var namePrefix = Console.ReadLine();
                for (int i = 0; i < accounts.Count; i++)
                {
                    accounts[i].Name = $"{namePrefix}{i + 1}";
                }
            }
            var monitoringServices = new Dictionary<string, Func<AbstractMonitoringService>> {
                            {"FbTool",()=>new FbToolService() },
                            {"Dolphin",()=>new DolphinService() }
                        };
            Console.WriteLine("Choose your service:");
            var monitoringService = SelectHelper.Select(monitoringServices, ms => ms.Key).Value();
            await monitoringService.AddAccountsAsync(accounts);
            Console.WriteLine("All accounts added to FbTool/Dolphin.");

        }
    }
}
