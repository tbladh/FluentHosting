﻿using System;
using System.Net;

namespace FluentHosting.Contracts
{
	public interface IRouteHandler
    {
        string Route { get; }
        Verb Verb { get; }
        Func<HttpListenerContext, IHandlerResponse> Handler { get; }

        CorsConfig CorsConfig { get; }
    }
}
