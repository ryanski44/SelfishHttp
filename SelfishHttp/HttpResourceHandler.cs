using System;
using System.Net;
using System.Text.RegularExpressions;

namespace SelfishHttp
{
    public class HttpResourceHandler : IHttpResourceHandler
    {
        private string _method;
        private Regex _pathRegex;
        private HttpHandler _pipeline;

        public IServerConfiguration ServerConfiguration { get; private set; }
        public AuthenticationSchemes? AuthenticationScheme { get; set; }

        public HttpResourceHandler(string method, Regex pathRegex, IServerConfiguration serverConfiguration)
        {
            _method = method;
            _pathRegex = pathRegex;
            _pipeline = new HttpHandler(serverConfiguration);
            ServerConfiguration = serverConfiguration;
            AuthenticationScheme = AuthenticationSchemes.Anonymous;
        }

        public void AddHandler(Action<Match, HttpListenerContext, Action> handler)
        {
            _pipeline.AddHandler(handler);
        }

        public void AddHandler(Action<HttpListenerContext, Action> handler)
        {
            _pipeline.AddHandler(handler);
        }

        public void Handle(Match match, HttpListenerContext context, Action next)
        {
            _pipeline.Handle(match, context, next);
        }

        public Match Match(HttpListenerRequest request)
        {
            if (request.HttpMethod == _method)
            {
                return _pathRegex.Match(request.Url.AbsolutePath);
            }
            return System.Text.RegularExpressions.Match.Empty;
        }

        public IHttpResourceHandler IgnorePathCase()
        {
            _pathRegex = new Regex(_pathRegex.ToString(), _pathRegex.Options | RegexOptions.IgnoreCase);
            return this;
        }
    }
}