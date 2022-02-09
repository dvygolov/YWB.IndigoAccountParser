﻿using Microsoft.Extensions.Logging;
using YWB.AntidetectAccountsParser.Interfaces;
using YWB.AntidetectAccountsParser.Model.Accounts;
using YWB.AntidetectAccountsParser.Model.Actions;
using YWB.AntidetectAccountsParser.Services.Actions;
using YWB.Helpers;

namespace YWB.AntidetectAccountsParser.Services.Parsers
{
    public class FacebookArchivesAccountsParser : AbstractArchivesAccountsParser<FacebookAccount>
    {
        public FacebookArchivesAccountsParser(ILogger logger, IProxyProvider<FacebookAccount> pp) : base(logger, pp) { }

        public override ActionsFacade<FacebookAccount> GetActions(string filePath)
        {
            var fa = new FacebookAccount(Path.GetFileNameWithoutExtension(filePath));
            Console.WriteLine($"Parsing file: {filePath}");
            return new ActionsFacade<FacebookAccount>()
            {
                Account = fa,
                AccountActions = new List<AccountAction<FacebookAccount>>()
                {
                    new PasswordAccountAction<FacebookAccount>(),
                    new CookiesAccountAction<FacebookAccount>(),
                    new TokenAccountAction<FacebookAccount>()
                }
            };
        }

        public override AccountValidity IsValid(FacebookAccount fa)
        {
            if (fa.AllCookies.Any(c => CookieHelper.HasCUserCookie(c)))
            {
                var uid=CookieHelper.GetCUserCookie(fa.AllCookies);
                var ch=FbHeadersChecker.Check(uid);
                if (!ch) return AccountValidity.Invalid;
                return AccountValidity.Valid;
            }
            else if (fa.Login != null && fa.Password != null)
                return AccountValidity.PasswordOnly;
            else
                return AccountValidity.Invalid;
        }

        public override IEnumerable<FacebookAccount> MultiplyCookies(IEnumerable<FacebookAccount> accounts)
        {
            var finalRes = new List<FacebookAccount>();
            //If we have cookies from multiple accounts we should create an account for each cookie set
            foreach (var fa in accounts)
            {
                if (fa.AllCookies.Count == 1)
                {
                    finalRes.Add(fa);
                    continue;
                }
                for (int i = 0; i < fa.AllCookies.Count; i++)
                {
                    var cookies = fa.AllCookies[i];
                    var newFa = new FacebookAccount()
                    {
                        Birthday = fa.Birthday,
                        BmLinks = fa.BmLinks,
                        Cookies = cookies,
                        EmailLogin = fa.EmailLogin,
                        EmailPassword = fa.EmailPassword,
                        Logins = fa.Logins,
                        Passwords = fa.Passwords,
                        Token = fa.Token,
                        TwoFactor = fa.TwoFactor,
                        UserAgent = fa.UserAgent,
                        Name = $"{fa.Name}_{i + 1}",
                        Proxy=fa.Proxy
                    };
                    finalRes.Add(newFa);
                }
            }
            return finalRes;
        }
    }
}