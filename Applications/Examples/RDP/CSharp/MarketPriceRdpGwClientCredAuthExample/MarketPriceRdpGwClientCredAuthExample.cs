//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|            Copyright (C) 2018-2020 Refinitiv. All rights reserved.        --
//|-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Security.Cryptography.X509Certificates;
/*
 * This example demonstrates authenticating via Refinitiv Data Platform (RDP), using an
 * authentication token to discover Refinitiv Real-Time service endpoint or use specified
 * endpoint (host and port), and using the endpoint and authentitcation to
 * retrieve market content. Specifically for oAuthClientCred authentication, this
 * application uses the client credentials grant type in the auth request
 * RDP (auth/oauth2/v2/token) using Refinitiv provided credentials: client id (username)
 * and client secret (password).
 *
 * This example maintains a session by proactively renewing the authentication 
 * token before expiration.
 *
 * This example can run with optional hotstandby support. Without this support, the application
 * will use a load-balanced interface with two hosts behind the load balancer. With hot standly
 * support, the application will access two hosts and display the data (should be identical) from
 * each of the hosts.
 *
 * It performs the following steps:
 * - Authenticating via HTTP Post request to Refinitiv Data Platform 
 * - Retrieving service endpoints from Service Discovery via HTTP Get request, 
 *   using the token retrieved from Refinitiv Data Platform  
 * - Opening a WebSocket (or two, if the --hotstandby option is specified) to
 *   a Refinitiv Real-Time Service endpoint, as retrieved from Service Discovery
 * - Sending Login into the Real-Time Service using the token retrieved
 *   from Refinitiv Data Platform.
 * - Requesting market-price content.
 * - Printing the response content.
 * - Upon disconnect, re-request authentication token to reconnect to Refinitiv Data 
 * - Platform endpoint(s) if it is no longer valid.
 */

namespace MarketPriceRdpGwClientCredAuthExample
{


    class MarketPriceRdpGwClientCredAuthExample
    {
        /// <summary>The websocket(s) used for retrieving market content.</summary>
        private static Dictionary<string, WebSocketSession> _webSocketSessions = new Dictionary<string, WebSocketSession>();

        /// <summary>The tokens retrieved from the authentication server.
        private static string _authToken;

        /// <summary>The full URL of the authentication server. If not specified,
        /// https://api.refinitiv.com:443/auth/oauth2/v1/token is used.</summary>
        private static string _authUrl = "https://api.refinitiv.com/auth/oauth2/v2/token";

        /// <summary>The full URL of the Refinitiv Data Platform service discovery server. If not specified,
        /// https://api.refinitiv.com/streaming/pricing/v1/ is used.</summary>
        private static string _discoveryUrl = "https://api.refinitiv.com/streaming/pricing/v1/";

        /// <summary>The configured client ID used when requesting the token.</summary>
        private static string _clientId;

        /// <summary>The configured client secret used when requesting the token.</summary>
        private static string _clientSecret;

        /// <summary>The configured hostname used when start websoket session.</summary>
        private static string _hostName = null;

        /// <summary>The configured hostname used when start websoket session.</summary>
        private static string _hostName2 = null;
		
		/// <summary>The configured port used when requesting the token.</summary>
		private string _port = "443";
		
		/// <summary>The configured port used when requesting the token.</summary>
		private string _port2 = "443";

        /// <summary>The configured ApplicationID used when requesting the token.</summary>
        private static string _appID = "256";

        /// <summary>The configured scope used when requesting the token.</summary>
        private static string _scope = "trapi.streaming.pricing.read";

        /// <summary>The configured RIC used when requesting price data.</summary>
        private static string _ric = "/TRI.N";

        /// <summary>The requested service name or service ID.</summary>
        private static string _service = "ELEKTRON_DD";

        /// <summary>The IP address, used as the application's position when requesting the token.</summary>
        private static string _position;

        /// <summary>Timestamp when the token was obtained. </summary>
        private static double _tokenTS = 0.0;

        /// <summary>Amount of time until the authentication token expires; re-authenticate before then</summary>
        private static int _expiration_in_ms = Timeout.Infinite;

        /// <summary>Expiration time returned by login (on refresh) request</summary>
        private int _original_expiration_in_ms = Timeout.Infinite;

        /// <summary>indicates whether application should support hotstandby</summary>
        private static bool _hotstandby = false;

        /// <summary> Specifies a region to get endpoint(s) from the Refinitiv Data Platform service discovery</summary>
        private static string _region = "us-east-1";

        /// <summary>hosts returned by service discovery</summary>
        private static List<Tuple<string,string>> _hosts = new List<Tuple<string, string>>();

        /// <summary>backup hosts for non-hotstandby that only have a single endpoint to use when multiples do not exist</summary>
        private static List<Tuple<string, string>> _backupHosts = new List<Tuple<string, string>>();

        /// <summary> Specifies buffer size for each read from WebSocket.</summary>
        private static readonly int BUFFER_SIZE = 8192;

        public class WebSocketSession
        {
            /// <summary> Name to use when printing messages sent/received over this WebSocket. </summary>
            public string Name { get; set; }

            /// <summary> Current WebSocket associated with this session. </summary>
            public ClientWebSocket WebSocket { get; set; }

            /// <summary> Indicates whether the session is canceled by users</summary>
            public bool Canceling { get; set; }

            /// <summary> This is used to cancel operations when closing the application. </summary>
            public CancellationTokenSource Cts { get; set; }

             /// <summary> Whether the session has successfully logged in. </summary>
            public bool IsLoggedIn = false;

            /// <summary> Whether current WebSocket has successfully connected. </summary>
            public bool WebSocketConnected = false;

            /// <summary> Whether the current WebSocket lost connection do token request & reconnect</summary>
            public bool Reconnecting = false;

            /// <summary> URI to connect the WebSocket to. </summary>
            private Uri _uri;

            public WebSocketSession(String name, String host, String port)
            {
                Name = name;
                _uri = new Uri("wss://" + host + ":" + port + "/WebSocket");
                Canceling = false;
                Cts = new CancellationTokenSource();
                Console.WriteLine("{0} Connecting to WebSocket {1} ...", DateTime.Now.ToString(), _uri.AbsoluteUri);
            }

            /// <summary>
            /// Creates a WebSocket connection and sends a login request
            /// </summary>
            public void Connect()
            {
                IsLoggedIn = false;

                WebSocket = new ClientWebSocket();
                WebSocket.Options.SetBuffer(BUFFER_SIZE, BUFFER_SIZE);
                WebSocket.Options.AddSubProtocol("tr_json2");

                try
                {
                    WebSocket.ConnectAsync(_uri, CancellationToken.None).Wait();
                    WebSocketConnected = true;
                    SendLogin();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("WebSoket client unable to connect: {0} -- {1}\n", ex.Message.ToString(), ex.InnerException.Message.ToString());
                }
            }

            /// <summary>
            /// Closes the existing connection and creates a new one.
            /// </summary>
            public void Reconnect()
            {
                Console.WriteLine("The WebSocket connection is closed for " + Name);
                if (!Canceling)
                {
                    Console.WriteLine("Reconnect to the endpoint for " + Name + " after 3 seconds...");
                    Thread.Sleep(3000);
                    WebSocket.Dispose();
                    Connect();
                    Reconnecting = false;
                }
            }

            /// <summary>
            /// The main loop of the WebSocketSession
            /// </summary>
            public void Run()
            {
                while (WebSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        ReceiveMessage();
                    }
                    catch (System.AggregateException)
                    {
                        // The connection was lost. Enable reconnection.
                        WebSocketConnected = false;
                        Reconnecting = true;
                    }
                }
            }

            /// <summary>
            /// Creates and sends a login message
            /// </summary>
            /// <param name="isRefresh">Setting <c>true</c> to not interest in the login refresh</param>
            public void SendLogin()
            {
                string msg = "{" + "\"ID\":1," + "\"Domain\":\"Login\"," + "\"Key\": {\"NameType\":\"AuthnToken\"," +
                    "\"Elements\":{\"ApplicationId\":\"" + _appID + "\"," + "\"Position\":\"" + _position + "\"," +
                    "\"AuthenticationToken\":\"" + _authToken + "\"}}";
                msg += "}";
                SendMessage(msg);
            }

            /// <summary>Prints the outbound message and sends it on the WebSocket.</summary>
            /// <param name="jsonMsg">Message to send</param>
            private void SendMessage(string jsonMsg)
            {
                /* Print the message (format the object string for easier reading). */
                Console.WriteLine("{0} SENT on {1}:\n{2}\n", DateTime.Now.ToString(), Name, JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonMsg), Formatting.Indented));

                var encoded = Encoding.ASCII.GetBytes(jsonMsg);
                var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

                WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, Cts.Token).Wait();
            }

            /// <summary>Reads data from the WebSocket and parses to JSON message</summary>
            private void ReceiveMessage()
            {
                using (MemoryStream memoryStream = new MemoryStream(BUFFER_SIZE * 5))
                {
                    var readBuffer = new ArraySegment<byte>(new byte[BUFFER_SIZE]);
                    Task<WebSocketReceiveResult> result = null;

                    do
                    {
                        result = WebSocket.ReceiveAsync(readBuffer, Cts.Token);
                        result.Wait();

                        if (result.IsFaulted)
                        {
                            Console.WriteLine("Read message failed " + result.Exception.Message);
                            Console_CancelKeyPress(this, null);
                        }
                        else
                        {
                            memoryStream.Write(readBuffer.Array, readBuffer.Offset, result.Result.Count);
                        }
                    }
                    while (!result.Result.EndOfMessage);

                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader reader = new StreamReader(memoryStream, Encoding.ASCII))
                    {
                        /* Received message(s). */
                        JArray messages = JArray.Parse(reader.ReadToEnd());
                        /* Print the message (format the object string for easier reading). */
                        Console.WriteLine("{0} RECEIVED on {1}:\n{2}\n", DateTime.Now.ToString(), Name, JsonConvert.SerializeObject(messages, Formatting.Indented));

                        for (int index = 0; index < messages.Count; ++index)
                            ProcessJsonMsg(messages[index]);
                    }
                }
            }

            /// <summary>
            /// Processes the received message. If the message is a login response indicating we are now logged in,
            /// opens a stream for price content.
            /// </summary>
            /// <param name="msg">The received JSON message</param>
            void ProcessJsonMsg(dynamic msg)
            {
                switch ((string)msg["Type"])
                {
                    case "Refresh":
                        if ((string)msg["Domain"] == "Login")
                        {
                            if (msg["State"] != null && (string)msg["State"]["Stream"] != "Open")
                            {
                                Console.WriteLine("Login stream was closed.\n");
                            }

                            if ((msg["State"] == null || (string)msg["State"]["Data"] == "Ok"))
                            {
                                // Login was successful. Request an item
                                IsLoggedIn = true;
                                SendMessage("{" + "\"ID\": 2," + "\"Key\": {\"Name\":\"" + _ric + "\",\"Service\":\"" + _service + "\"}" + "}");
                            }
                        }
                        break;
                    case "Status":
                        if (msg["Domain"] != null && (string)msg["Domain"] == "Login" &&
                            msg["State"] != null && (string)msg["State"]["Stream"] != null && (string)msg["State"]["Stream"] != "Open")
                        {
                            IsLoggedIn = false;
                            Console.WriteLine("Stream is no longer open (state is {0})", (string)msg["State"]["Stream"]);
                        }
                        break;
                    case "Ping":
                        SendMessage("{\"Type\":\"Pong\"}");
                        break;
                    default:
                        break;
                }
            }

        }

        /// <summary>Parses commandline config and runs the application.</summary>
        static void Main(string[] args)
        {
            MarketPriceRdpGwClientCredAuthExample example = new MarketPriceRdpGwClientCredAuthExample();
            example.ParseCommandLine(args);
            example.Run();
        }

        /// <summary> Send an HTTP request to the specified authentication server, containing our clientId and clientSecret.
        /// The token will be used to login on the websocket. </summary>
        /// <returns><c>true</c> if success otherwise <c>false</c></returns>
        public bool GetAuthenticationInfo(string url = null)
        {
            if (string.IsNullOrEmpty(url))
                url = _authUrl;

            Console.WriteLine("{0} Sending authentication request to {1}", DateTime.Now.ToString(), url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

            try
            {
                /* Create string for the request. */
                string postString = "grant_type=client_credentials"; 
                postString += "&client_id=" + _clientId;
                postString += "&client_secret=" + _clientSecret;
                postString += "&scope=" + _scope;

                byte[] postContent = Encoding.ASCII.GetBytes(postString);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = postContent.Length;
                webRequest.AllowAutoRedirect = false;

                System.IO.Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(postContent, 0, postContent.Length);
                requestStream.Close();

                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.GetResponseHeader("Transfer-Encoding").Equals("chunked") || webResponse.ContentLength > 0)
                {
                    /* If there is content in the response, print it. */
                    /* Format the object string for easier reading. */
                    dynamic msg = JsonConvert.DeserializeObject(new System.IO.StreamReader(webResponse.GetResponseStream()).ReadToEnd());
                    Console.WriteLine("{0} RECEIVED:\n{1}\n", DateTime.Now.ToString(), JsonConvert.SerializeObject(msg, Formatting.Indented));

                    // other possible items: auth_token, refresh_token, expires_in
                    _authToken = msg["access_token"].ToString();
                    if (Int32.TryParse(msg["expires_in"].ToString(), out _expiration_in_ms))
                        _expiration_in_ms *= 1000;
						_original_expiration_in_ms = _expiration_in_ms;
                }

                webResponse.Close();
                return true;

            }
            catch (WebException e)
            {
                HttpWebResponse response = null;
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    response = (HttpWebResponse)e.Response;

                    HttpStatusCode statusCode = response.StatusCode;

                    bool ret = false;

                    switch (statusCode)
                    {
                        case HttpStatusCode.Moved:                         // 301
                        case HttpStatusCode.Redirect:                      // 302
                        case HttpStatusCode.TemporaryRedirect:             // 307
                        case HttpStatusCode.PermanentRedirect:             // 308
                            // Perform URL redirect
                            Console.WriteLine("Refinitiv Data Platform authentication HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            string newHost = response.Headers.Get("Location");
                            if (!string.IsNullOrEmpty(newHost))
                                ret = GetAuthenticationInfo(newHost);
                            break;
                        case HttpStatusCode.BadRequest:                     // 400
                        case HttpStatusCode.Unauthorized:                   // 401
                        case HttpStatusCode.Forbidden:                      // 403
                        case HttpStatusCode.NotFound:                       // 404
                        case HttpStatusCode.Gone:                           // 410
                        case HttpStatusCode.UnavailableForLegalReasons:     // 451
                            Console.WriteLine("Refinitiv Data Platform authentication HTTP code: {0} {1}\n", response.StatusCode, response.StatusDescription);
                            Console.WriteLine("Unrecoverable error: stopped retrying request");
                            break;
                        default:
                            Console.WriteLine("Refinitiv Data Platform authentication HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            Console.WriteLine("Retrying auth request");
                            Thread.Sleep((int)(5000));
                            // CAUTION: This is sample code with infinite retries
                            ret = GetAuthenticationInfo();
                            break;
                    }

                    response.Close();
                    return ret;
                }
                else
                {
                    /* The request to the authentication server failed, e.g. due to connection failure or HTTP error response. */
                    if (e.InnerException != null)
                        Console.WriteLine("Authentication server request failed: {0} -- {1}\n", e.Message, e.InnerException.Message);
                    else
                        Console.WriteLine("Authentication server request failed: {0}", e.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Requests Refinitiv Data Platform service discovery to get endpoint(s) to connect to Refinitiv Real-Time - Optimized 
        /// </summary>
        /// <returns><c>true</c> if success otherwise <c>false</c></returns>
        public static bool DiscoverServices(string url = null)
        {
            /*If host was set no need to perfor service discovery */
            if (!string.IsNullOrEmpty(_hostName))
                return true;

            if(string.IsNullOrEmpty(url))
              url = _discoveryUrl;

            string param_url = url + "?transport=websocket";

            Console.WriteLine("{0} Sending service discovery request to {1}\n", DateTime.Now.ToString(), param_url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(param_url);
            webRequest.Headers.Add("Authorization", "Bearer " + _authToken);

            webRequest.UserAgent = "CSharpMarketPriceRdpGwClientCredAuthExample";
            webRequest.AllowAutoRedirect = false;

            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.GetResponseHeader("Transfer-Encoding").Equals("chunked") || webResponse.ContentLength > 0)
                {
                    /* If there is content in the response, print it. */
                    /* Format the object string for easier reading. */
                    dynamic msg = JsonConvert.DeserializeObject(new System.IO.StreamReader(webResponse.GetResponseStream()).ReadToEnd());
                    Console.WriteLine("{0} RECEIVED:\n{1}", DateTime.Now.ToString(), JsonConvert.SerializeObject(msg, Formatting.Indented));

                    // extract endpoints
                    Newtonsoft.Json.Linq.JArray endpoints = msg["services"];
                    for (int i = 0; i < endpoints.Count; ++i)
                    {
                        Newtonsoft.Json.Linq.JArray locations = (Newtonsoft.Json.Linq.JArray)endpoints[i]["location"];

                        /* If a service has one location, then it represents an endpoint used to support hot standby. Users are
                            * expected to connect to multiple endpoints to activate hot standby, so this example fails if only one service
                            * exists with a single location and the hot standby feature is active.
                            *
                            * If a service has two locations, then it is not a server that supports hot standby, but instead a load balancer
                            * with multiple endpoints behind it represented by the locations. Users connect to the endpoint and port
                            * indicated for the service.
                            *
                            * Services not fitting this pattern are ignored
                            */

                        if (((string)endpoints[i]["location"][0]).StartsWith(_region) == false)
                            continue;

                        if (_hotstandby && locations.Count == 1 && _hostName == null && _hostName2 == null)
                        {
                            _hosts.Add(new Tuple<string,string>((endpoints[i]["endpoint"]).ToString(),(endpoints[i]["port"]).ToString()));
                            continue;
                        }
                        if (!_hotstandby && locations.Count >= 2 && _hostName == null)
                        {
                            _hosts.Add(new Tuple<string, string>((endpoints[i]["endpoint"]).ToString(),(endpoints[i]["port"]).ToString()));
                            continue;
                        }
                        else if (!_hotstandby && locations.Count == 1 && _hostName == null)
                        {
                            _backupHosts.Add(new Tuple<string, string>((endpoints[i]["endpoint"]).ToString(), (endpoints[i]["port"]).ToString()));
                            continue;
                        }
                    }

                    if (_hotstandby)
                    {
                        if(_hosts.Count < 2)
                        {
                            Console.WriteLine("hotstandby support requires at least two hosts");
                            System.Environment.Exit(1);
                        }
                    }
                    else
                    {
                        if (_hosts.Count == 0)
                        {
                            if (_backupHosts.Count > 0)
                            {
                                _hosts = _backupHosts;
                            }
                        }
                    }

                    if (_hosts.Count == 0)
                    {
                        Console.WriteLine("No host found from Refinitiv Data Platform service discovery");
                        System.Environment.Exit(1);
                    }
                }
                return true;
            }
            catch (WebException e)
            {

                HttpWebResponse response = null;
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    response = (HttpWebResponse)e.Response;

                    HttpStatusCode statusCode = response.StatusCode;

                    bool ret = false;

                    switch (statusCode)
                    {
                        case HttpStatusCode.Moved:                  // 301
                        case HttpStatusCode.Redirect:               // 302
                        case HttpStatusCode.TemporaryRedirect:      // 307
                        case HttpStatusCode.PermanentRedirect:      // 308
                            // Perform URL redirect
                            Console.WriteLine("Refinitiv Data Platform service discovery HTTP code: {0} {1}\n", statusCode, response.StatusDescription);

                            string newHost = response.Headers.Get("Location");
                            if (!string.IsNullOrEmpty(newHost))
                            {
                                Console.WriteLine("Perform URL redirect to {0}", newHost);
                                ret = DiscoverServices(newHost);
                            }
                            else
                                ret = false;
                            break;
                        case HttpStatusCode.Forbidden:                      // 403
                        case HttpStatusCode.NotFound:                       // 404
                        case HttpStatusCode.Gone:                           // 410
                        case HttpStatusCode.UnavailableForLegalReasons:     // 451
                            Console.WriteLine("Refinitiv Data Platform service discovery HTTP code: {0} {1}\n", response.StatusCode, response.StatusDescription);
                            Console.WriteLine("Unrecoverable error: stopped retrying request");
                            break;
                        default:
                            Console.WriteLine("Refinitiv Data Platform service discovery HTTP code: {0} {1}\n", response.StatusCode, response.StatusDescription);
                            Console.WriteLine("Retrying service discovery request");
                            Thread.Sleep((int)(5000));
                            // CAUTION: This is sample code with infinite retries
                            ret = DiscoverServices();
                            break;
                    }

                    response.Close();
                    return ret;
                }
                else
                {
                    // service discovery failed
                    if (e.InnerException != null)
                        Console.WriteLine("Service discovery request failed: {0} -- {1}\n", e.Message.ToString(), e.InnerException.Message.ToString());
                    else
                        Console.WriteLine("Service discovery request failed: {0}", e.Message.ToString());
                    Environment.Exit(1);
                }
            }
            return false;
        }

        /// <summary>Runs the application. Retrives a token from the authentication server, then retrieves the list of services, and
        /// then opens the WebSocket(s) at the address(es) specified by service discovery using the token.</summary>
        public void Run()
        {
            /* Get local hostname. */
            IPAddress hostEntry = Array.Find(Dns.GetHostEntry(Dns.GetHostName()).AddressList, ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (string.IsNullOrEmpty(_position))
                _position = (hostEntry == null) ? "127.0.0.1/net" : hostEntry.ToString();

            if (!GetAuthenticationInfo() || !DiscoverServices())
                Environment.Exit(1);

            _tokenTS = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;

            if (_hostName != null)
            {
                _hosts.Add(new Tuple<string, string>(_hostName, _port));
                if (_hostName2 != null)
                {
                    _hosts.Add(new Tuple<string, string>(_hostName2, _port2));
                }
            }

            Console.CancelKeyPress += Console_CancelKeyPress;
            WebSocketSession session1 = null;
            WebSocketSession session2 = null;
            double curTS = 0.0;
            double deltaTime = 0.0;

            try
            {
                /* Open websocket(s) */
                foreach (var host in _hosts)
                {
                    var webSocketSession = new WebSocketSession("Session" + (_webSocketSessions.Count + 1), host.Item1, host.Item2);
                    _webSocketSessions.Add(webSocketSession.Name, webSocketSession);

                    webSocketSession.Connect();

                    Task.Factory.StartNew(() =>
                    {
                        webSocketSession.Run();
                    });

                    // if hotstandby is not supported, stop after connecting to the first host in hosts. Otherwise,
                    // connect to the first two hosts
                    if (_webSocketSessions.Count == 2 || !_hotstandby)
                        break;
                }

                // after 95% of the time allowed before the token expires, retrive a new set of tokens and send a login to each open websocket
                while (true)
                {
                    Thread.Sleep((int)(3000)); // in ms

                    if (_webSocketSessions.ContainsKey("Session1"))
                    {
                        session1 = _webSocketSessions["Session1"];
                    }
                    if (_webSocketSessions.ContainsKey("Session2"))
                    {
                        session2 = _webSocketSessions["Session2"];
                    }

                    if((session1 != null && !session1.WebSocketConnected) || (session2 != null && !session2.WebSocketConnected))
                    {
                        if ((session1 != null && !session1.Reconnecting) || (session2 != null && !session2.Reconnecting))
                        {
                            curTS = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;

                            if ((_expiration_in_ms / 1000) < 600)
                            {
                                deltaTime = _expiration_in_ms * 0.95;
                            }
                            else
                            {
                                deltaTime = 300 * 1000;
                            }

                            if (Convert.ToInt64(curTS - deltaTime) >= Convert.ToInt64(_tokenTS))
                            {
                                if (!GetAuthenticationInfo())
                                    Console_CancelKeyPress(null, null);

                                _tokenTS = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;

                                if (_expiration_in_ms != _original_expiration_in_ms)
                                {
                                    System.Console.WriteLine("expire time changed from " + _original_expiration_in_ms / 1000
                                        + " sec to " + _expiration_in_ms / 1000 + " sec; retry login");
                                    if (!GetAuthenticationInfo())
                                        Console_CancelKeyPress(null, null);
                                }
                            }

                        }
                        else
                        {
                            if (!GetAuthenticationInfo())
                                Console_CancelKeyPress(null, null);

                            _tokenTS = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
                        }
                        if (!session1.Canceling)
                        {
                            if (session1.WebSocket.State != WebSocketState.Open)
                            {
                                session1.Reconnect();

                                Task.Factory.StartNew(() =>
                                {
                                    session1.Run();
                                });
                            }
                        }
                        if (session2 != null && !session2.Canceling)
                        {
                            if (session2.WebSocket.State != WebSocketState.Open)
                            {
                                session2.Reconnect();

                                Task.Factory.StartNew(() =>
                                {
                                    session2.Run();
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
                Console.ReadKey();
            }
            finally
            {
                Console_CancelKeyPress(this, null);
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Handles Ctrl + C or exits the application.
        /// </summary>
        /// <param name="sender">The caller of this method</param>
        /// <param name="e">The <c>ConsoleCancelEventArgs</c> if any</param>
        public static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            foreach (var session in _webSocketSessions)
            {
                if(session.Value.WebSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine("The WebSocket connection is closed for " + session.Value.Name);
                    session.Value.Canceling = true;
                    session.Value.Cts.Cancel();
                    session.Value.WebSocket.Dispose();
                }
            }
            Console.WriteLine("{0} Exiting...", DateTime.Now.ToString());
            Environment.Exit(0);
        }

        /// <summary>Parses command-line arguments.</summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "--app_id":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }

                        _appID = args[++i];
                        break;
                    case "--auth_url":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _authUrl = args[++i];
                        break;
                    case "--discovery_url":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _discoveryUrl = args[++i];
                        break;
                    case "--hotstandby":
                        _hotstandby = true;
                        break;
                    case "--region":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _region = args[++i];
                        break;
                    case "--ric":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _ric = args[++i];
                        break;
                    case "--scope":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _scope = args[++i];
                        break;
                    case "--clientsecret":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _clientSecret = args[++i];
                        break;
                    case "--clientid":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _clientId = args[++i];
                        break;
                    case "--service":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _service = args[++i];
                        break;
                    case "--hostname":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _hostName = args[++i];
                        break;
                    case "--standbyhostname":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _hostName2 = args[++i];
                        break;
                    case "--port":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _port = args[++i];
                        break;
                    case "--standbyport":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _port2 = args[++i];
                        break;
                    case "--position":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _position = args[++i];
                        break;
                    default:
                        Console.WriteLine("Unknown option: {0}", args[i]);
                        PrintCommandLineUsageAndExit();
                        break;
                }
            }

            if (_clientSecret == null || _clientId == null)
            {
                Console.WriteLine("clientsecret and clientid must be specified on the command line");
                PrintCommandLineUsageAndExit();
            }
        }

        /// <summary>Prints usage information. Used when arguments cannot be parsed.</summary>
        void PrintCommandLineUsageAndExit()
        {
            Console.WriteLine("Usage: {0} [--app_id appID] [--auth_url auth_url] [--discovery_url discovery_url] [--hotstandby] [--region region] [--ric ric] [--scope scope] [--clientid clientID] [--clientsecret clientSecret] [--service service] [--hostname hostname] [--port port] [--standbyhostname hostname] [--standbyport port] [--position position]", System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(1);
        }
    }
}
