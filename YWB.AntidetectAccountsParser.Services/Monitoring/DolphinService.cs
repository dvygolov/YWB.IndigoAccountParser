﻿using Newtonsoft.Json.Linq;
using RestSharp;
using YWB.AntidetectAccountsParser.Model;
using YWB.AntidetectAccountsParser.Model.Accounts;

namespace YWB.AntidetectAccountsParser.Services.Monitoring
{
    public class DolphinService : AbstractMonitoringService
    {
        public DolphinService(string credentials) : base(credentials) { }

        public override Task<List<AccountGroup>> GetExistingGroupsAsync()
        {
            return Task.FromResult(new List<AccountGroup>());
        }

        public override Task<AccountGroup> AddNewGroupAsync(string groupName)
        {
            return Task.FromResult(new AccountGroup() { Name = groupName });
        }
        protected override async Task<List<Proxy>> GetExistingProxiesAsync()
        {
            var r = new RestRequest("proxy", Method.GET);
            dynamic json = await ExecuteDolphinRequestAsync(r);
            return (json.data as JArray).Select((dynamic j) => new Proxy
            {
                Id = j.id,
                Type = j.type,
                Address = j.ip,
                Port = j.port,
                Login = j.login,
                Password = j.password
            }).ToList();
        }

        protected override async Task<string> AddProxyAsync(Proxy p)
        {
            var r = new RestRequest("proxy/add", Method.POST);
            dynamic container = new JObject();
            container.proxy = new JArray();

            if (p.Type == "socks") p.Type = "socks5";

            dynamic pJson = new JObject();
            pJson.name = DateTime.Now.ToString("G");
            pJson.host = p.Address;
            pJson.port = p.Port;
            pJson.type = p.Type;
            pJson.login = p.Login;
            pJson.password = p.Password;
            container.proxy.Add(pJson);

            r.AddJsonBody(container.ToString());
            var json = await ExecuteDolphinRequestAsync(r);
            return json["data"]["proxy_id"].ToString();
        }

        protected override async Task<bool> AddAccountAsync(FacebookAccount acc, AccountGroup g, string proxyId)
        {
            var r = new RestRequest("accounts/add", Method.POST);
            dynamic rJson = new JObject();
            rJson.name = acc.Name;
            rJson.access_token = acc.Token;
            if (!string.IsNullOrEmpty(acc.BmToken))
                rJson.business_access_token = acc.BmToken;
            rJson.tags = new JArray();
            if (g != null) rJson.tags.Add(g.Name);
            rJson.cookies = JArray.Parse(acc.Cookies);
            rJson.proxy = new JObject();
            rJson.proxy.id = proxyId;
            if (!string.IsNullOrEmpty(acc.Password))
                rJson.password = acc.Password;
            r.AddJsonBody(rJson.ToString());
            dynamic json = await ExecuteDolphinRequestAsync(r);
            return json.success == true;
        }

        protected override void AddAuthorization(RestRequest r)
        {
            r.AddHeader("Authorization", _token);
        }

        protected override void SetTokenAndApiUrl()
        {
            var split = _credentials.Split(':');
            (_apiUrl, _token) = (split[0], split[1]);
            _apiUrl = $"http://{_apiUrl}/new/";
        }

        private async Task<dynamic> ExecuteDolphinRequestAsync(RestRequest r)
        {
            dynamic res = await ExecuteRequestAsync<JObject>(r);
            if (res.success == false)
            {
                string errors = string.Empty;
                foreach (var e in res.errors)
                    errors += $"{e} ";
                throw new Exception(errors);
            }
            return res;
        }
    }
}