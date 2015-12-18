using System;
using System.Net;
using System.Text.RegularExpressions;

namespace SelfishHttp
{
    public interface IHttpHandler
    {
        void AddHandler(Action<Match, HttpListenerContext, Action> handler);
        void AddHandler(Action<HttpListenerContext, Action> handler);
        void Handle(Match pathMatch, HttpListenerContext context, Action next);
        AuthenticationSchemes? AuthenticationScheme { get; set; }
        IServerConfiguration ServerConfiguration { get; }
    }

    public interface IHttpResourceHandler : IHttpHandler
    {
        Match Match(HttpListenerRequest request);
        IHttpResourceHandler IgnorePathCase();
    }
}