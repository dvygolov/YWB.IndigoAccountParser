﻿using System.Collections.Generic;
using YWB.AntidetectAccountParser.Model;

namespace YWB.AntidetectAccountParser.Services.Interfaces
{
    public interface IProxyProvider
    {
        List<Proxy> Get();
    }
}