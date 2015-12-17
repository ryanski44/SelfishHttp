using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace SelfishHttp
{
    public class HttpHandler : IHttpHandler
    {
        private IList<Action<Match, HttpListenerContext, Action>> _handlers;

        public HttpHandler(IServerConfiguration serverConfig)
        {
            _handlers = new List<Action<Match, HttpListenerContext, Action>>();
            ServerConfiguration = serverConfig;
        }

        public void Handle(Match pathMatch, HttpListenerContext context, Action next)
        {
            var handlerEnumerator = _handlers.GetEnumerator();
            Action handle = null;
            handle = () =>
                         {
                             if (handlerEnumerator.MoveNext())
                             {
                                 handlerEnumerator.Current(pathMatch, context, () => handle());
                             } else
                             {
                                 next();
                             }
                         };

            handle();
        }

        public AuthenticationSchemes? AuthenticationScheme { get; set; }
        public IServerConfiguration ServerConfiguration { get; private set; }

        public void AddHandler(Action<Match, HttpListenerContext, Action> handler)
        {
            _handlers.Add(handler);
        }

        public void AddHandler(Action<HttpListenerContext, Action> handler)
        {
            _handlers.Add(new Action<Match, HttpListenerContext, Action>((match, context, next) => handler.Invoke(context, next)));
        }
    }
}