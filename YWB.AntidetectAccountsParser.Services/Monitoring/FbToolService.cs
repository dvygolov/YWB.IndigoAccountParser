﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using YWB.AntidetectAccountsParser.Model;
using YWB.AntidetectAccountsParser.Model.Accounts;

namespace YWB.AntidetectAccountsParser.Services.Monitoring
{
    public class FbToolService : AbstractMonitoringService
    {
        public FbToolService(string credentials,ILoggerFactory lf) : base(credentials,lf) { }

        public override async Task<List<AccountGroup>> GetExistingGroupsAsync()
        {
            var r = new RestRequest("get-groups", Method.GET);
            var json = await ExecuteRequestAsync<JObject>(r);
            return json.Children().Select(t => t.First).Where(t => t.HasValues).Select(t => new AccountGroup()
            {
                Id = t["id"].ToString(),
                Name = t["name"].ToString()
            }).ToList();
        }
        public override Task<AccountGroup> AddNewGroupAsync(string groupName)
        {
            return Task.FromResult(new AccountGroup() { Id = "new", Name = groupName });
        }

        protected override async Task<string> AddProxyAsync(Proxy p)
        {
            p.Type = p.Type == "socks" ? "socks5" : p.Type;
            var r = new RestRequest("add-proxy", Method.POST);
            r.AddParameter("proxy", $"{p.Address}:{p.Port}:{p.Login}:{p.Password}:{p.Type}");
            dynamic json = await ExecuteRequestAsync<JObject>(r);
            return json.id;
        }

        protected override async Task<List<Proxy>> GetExistingProxiesAsync()
        {
            var r = new RestRequest("get-proxy", Method.GET);
            var json = await ExecuteRequestAsync<JObject>(r);
            return json.Children().Select(t => t.First).Where(t => t.HasValues).Select(t =>
            {
                var pStr = t["proxy"].ToString();
                try
                {
                    var s = pStr.Split(":");
                    var p = new Proxy()
                    {
                        Id = t["id"].ToString(),
                        Address = s[0],
                        Port = s[1],
                        Login = s[2],
                        Password = s[3],
                        Type = (t["type"].ToString() == string.Empty ? "http" : (t["type"].ToString() == "https" ? "http" : t["type"].ToString()))
                    };
                    return p;
                }
                catch
                {
                    _logger.LogError($"Couldn't parse proxy string:{pStr}");
                    return null;
                }
            }).Where(p => p != null).ToList();
        }

        protected override async Task<bool> AddAccountAsync(FacebookAccount acc, AccountGroup g, string proxyId)
        {
            var r = new RestRequest("add-account", Method.POST);
            r.AddParameter("token", acc.Token);
            r.AddParameter("proxy", proxyId);
            r.AddParameter("group", g.Id);
            if (g.Id=="new")
                r.AddParameter("groupName", g.Name);
            r.AddParameter("name", acc.Name);
            if (!string.IsNullOrEmpty(acc.UserAgent))
                r.AddParameter("useragent", acc.UserAgent);
            if (!string.IsNullOrEmpty(acc.Password))
                r.AddParameter("pass", acc.Password);
            if (!string.IsNullOrEmpty(acc.Cookies))
                r.AddParameter("cookie", acc.Cookies);
            if (!string.IsNullOrEmpty(acc.BmToken))
                r.AddParameter("bm_token", acc.BmToken);
            r.AddParameter("accept_policy", "on");
            r.AddParameter("disable_notifications", "on");
            r.AddParameter("autopublish_fp", "on");
            r.AddParameter("comment_status", "on");
            r.AddParameter("deleteOrHide", 0);
            dynamic json = await ExecuteRequestAsync<JObject>(r);
            if (json.ContainsKey("success"))
            {
                if (json.success == false)
                {
                    _logger.LogError($"Couldn't add account {acc.Name} to FbTool. Error:{json.message}");
                    return false;
                }
                else
                    return true;
            }
            else
            {
                _logger.LogError($"Couldn't add account {acc.Name} to FbTool. Error:{json}");
                return false;
            }
        }

        protected override void AddAuthorization(RestRequest r)
        {
            r.AddQueryParameter("key", _token);
        }


        protected override void SetTokenAndApiUrl()
        {
            _token = _credentials;
            _apiUrl = "https://fbtool.pro/api";
        }

    }
}
