﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using YWB.AntidetectAccountsParser.Model;
using YWB.AntidetectAccountsParser.Services;
using YWB.AntidetectAccountsParser.Services.Proxies;
using YWB.AntidetectAccountsParser.TelegramBot.MessageProcessors;
using YWB.Helpers;

namespace YWB.AntidetectAccountsParser.TelegramBot
{
    internal class BotServiceStarter
    {
        private AccountsBot _bot;
        public void OnStart()
        {
            CopyrightHelper.Show(true);
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                            .AddJsonFile("appsettings.json", false, true);
            IConfigurationRoot configuration = builder.Build();
            List<ServiceCredentials> services = new List<ServiceCredentials>();
            configuration.GetSection("Services").Bind(services);
            services = services.Where(s => !string.IsNullOrEmpty(s.Credentials) || s.Name == "Indigo").ToList();

            List<string> users = new List<string>();
            configuration.GetSection("AllowedUsers").Bind(users);

            var sc = new ServiceCollection();
            sc.AddSingleton(users);
            sc.AddSingleton(configuration);
            sc.AddSingleton(services);
            sc.AddTransient<AbstractProxyProvider, TextProxyProvider>();
            sc.AddTransient<FbHeadersChecker>();
            sc.AddLogging(builder => builder.AddConsole().AddFile(@"logging\AAP.Telegram.log", LogLevel.Trace));

            Assembly.GetEntryAssembly().GetTypesAssignableFrom<AbstractMessageProcessor>().ForEach((t) =>
            {
                sc.AddScoped(typeof(AbstractMessageProcessor),t);
            });

            sc.AddSingleton(x => new AccountsBot(configuration.GetValue<string>("TelegramBotApiKey"), users, 
                    x.GetServices<AbstractMessageProcessor>(), x.GetService<ILogger<AccountsBot>>()));

            var sp = sc.BuildServiceProvider();
            _bot = sp.GetService<AccountsBot>();
            _bot.Start();
        }

        public void OnStop()
        {
            _bot.Stop();
        }
    }
}
