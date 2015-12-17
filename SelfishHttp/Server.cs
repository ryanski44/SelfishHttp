using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SelfishHttp
{
    public class Server : IDisposable, IServerConfiguration
    {
        private readonly string _uriPrefix;
        private readonly string _baseUri;
        private HttpListener _listener;
        private readonly HttpHandler _anyRequestHandler;
        private readonly List<IHttpResourceHandler> _resourceHandlers = new List<IHttpResourceHandler>();

        public IBodyParser BodyParser { get; set; }
        public IBodyWriter BodyWriter { get; set; }
        public IParamsParser ParamsParser { get; set; }

        public Server()
            : this(ChooseRandomUnusedPort())
        {
        }

        public Server(int port)
        {
            _uriPrefix = String.Format("http://localhost:{0}/", port);
            _baseUri = string.Format("http://localhost:{0}/", port);
            BodyParser = BodyParsers.DefaultBodyParser();
            BodyWriter = BodyWriters.DefaultBodyWriter();
            ParamsParser = new UrlParamsParser();
            _anyRequestHandler = new HttpHandler(this);
            Start();
        }

        public string BaseUri
        {
            get { return _baseUri; }
        }

        public IHttpResourceHandler OnGet(string path, bool ignoreCase = false)
        {
            return AddHttpHandler("GET", path, ignoreCase);
        }

        public IHttpResourceHandler OnGet(Regex pathRegex)
        {
            return AddHttpHandler("GET", pathRegex);
        }

        public IHttpResourceHandler OnHead(string path, bool ignoreCase = false)
        {
            return AddHttpHandler("HEAD", path, ignoreCase);
        }
        public IHttpResourceHandler OnHead(Regex pathRegex)
        {
            return AddHttpHandler("HEAD", pathRegex);
        }

        public IHttpResourceHandler OnPut(string path, bool ignoreCase = false)
        {
            return AddHttpHandler("PUT", path, ignoreCase);
        }
        public IHttpResourceHandler OnPut(Regex pathRegex)
        {
            return AddHttpHandler("PUT", pathRegex);
        }

        public IHttpResourceHandler OnPatch(string path, bool ignoreCase = false)
        {
            return AddHttpHandler("PATCH", path, ignoreCase);
        }
        public IHttpResourceHandler OnPatch(Regex pathRegex)
        {
            return AddHttpHandler("PATCH", pathRegex);
        }

        public IHttpResourceHandler OnPost(string path, bool ignoreCase = false)
        {
            return AddHttpHandler("POST", path, ignoreCase);
        }
        public IHttpResourceHandler OnPost(Regex pathRegex)
        {
            return AddHttpHandler("POST", pathRegex);
        }

        public IHttpResourceHandler OnDelete(string path, bool ignoreCase = false)
        {
            return AddHttpHandler("DELETE", path, ignoreCase);
        }
        public IHttpResourceHandler OnDelete(Regex pathRegex)
        {
            return AddHttpHandler("DELETE", pathRegex);
        }

        public IHttpResourceHandler OnOptions(string path, bool ignoreCase = false)
        {
            return AddHttpHandler("OPTIONS", path, ignoreCase);
        }
        public IHttpResourceHandler OnOptions(Regex pathRegex)
        {
            return AddHttpHandler("OPTIONS", pathRegex);
        }

        public IHttpHandler OnRequest()
        {
            return _anyRequestHandler;
        }

        private IHttpResourceHandler AddHttpHandler(string method, string path, bool ignoreCase = false)
        {
            return AddHttpHandler(method, new Regex(String.Format("^{0}$", Regex.Escape(path)), ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
        }

        private IHttpResourceHandler AddHttpHandler(string method, Regex pathRegex)
        {
            var httpHandler = new HttpResourceHandler(method, pathRegex, this);
            _resourceHandlers.Add(httpHandler);
            return httpHandler;
        }

        private void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_uriPrefix);
            _listener.AuthenticationSchemeSelectorDelegate = AuthenticationSchemeSelectorDelegate;
            _listener.Start();
            HandleNextRequest();
        }

        private AuthenticationSchemes AuthenticationSchemeSelectorDelegate(HttpListenerRequest httpRequest)
        {
            if (_anyRequestHandler.AuthenticationScheme.HasValue)
            {
                return _anyRequestHandler.AuthenticationScheme.Value;
            }

            var handler = _resourceHandlers.FirstOrDefault(h => h.Match(httpRequest).Success);
            if (handler != null && handler.AuthenticationScheme.HasValue)
            {
                return handler.AuthenticationScheme.Value;
            }
            return AuthenticationSchemes.Anonymous;
        }

        private void HandleNextRequest()
        {
            _listener.BeginGetContext(HandleRequest, null);
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private void HandleRequest(IAsyncResult ar)
        {
            try
            {
                var context = _listener.EndGetContext(ar);
                if (_listener.IsListening)
                {
                    HandleNextRequest();
                    HttpListenerRequest req = context.Request;
                    HttpListenerResponse res = context.Response;

                    try
                    {
                        _anyRequestHandler.Handle(null, context, () =>
                        {
                            bool found = false;
                            foreach(var handler in _resourceHandlers)
                            {
                                Match m = handler.Match(req);
                                if(m.Success)
                                {
                                    handler.Handle(m, context, () => { });
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                res.StatusCode = 404;
                            }
                        });

                        res.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        res.StatusCode = 500;
                        using (var output = new StreamWriter(res.OutputStream))
                        {
                            output.Write(ex);
                        }
                        res.Close();
                    }
                }
            }
            catch (HttpListenerException e)
            {
                if (!IsOperationAbortedOnStoppingServer(e))
                {
                    throw;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Return true if the exception is: The I/O operation has been aborted because of either a thread exit or an application request.
        /// Happens when we stop the server and the listening is cancelled.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static bool IsOperationAbortedOnStoppingServer(HttpListenerException e)
        {
            return e.NativeErrorCode == 0x000003E3;
        }

        public void Dispose()
        {
            Stop();
        }

        private static int ChooseRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}