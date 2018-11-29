//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright Thomson Reuters 2018. All rights reserved.            --
//|-----------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
 * This example demonstrates retrieving JSON-formatted market content from a WebSocket server,
 * using a token retrieved from an authentication service and using websocket server requests
 * extracted from a service discovery request.
 *
 * This example can run with optional hotstandby support. Without this support, the application
 * will use a load-balanced interface with two hosts behind the load balancer. With hot standly
 * support, the application will access two hosts and display the data (should be identical) from
 * each of the hosts.
 *
 * It performs the following steps:
 * - Sends an HTTP request to the authentication server and retrieves a token.
 * - Sends an HTTP request to the service discovery server and retrieves the
 *   servers that support streaming pricing via websockets
 * - Connects to the WebSocket server(s) using the authentication token.
 * - Requests TRI.N market-price content.
 * - Prints the response content.
 */

namespace MarketPriceEdpGwServiceDiscoveryExample
{
    class MarketPriceEdpGwServiceDiscoveryExample
    {
        /// <summary>The websocket(s) used for retrieving market content.</summary>
        private static Dictionary<string, WebSocketSession> _webSocketSessions = new Dictionary<string, WebSocketSession>();

        /// <summary>The tokens retrieved from the authentication server.
        private static string _authToken;
        private static string _refreshToken;

        /// <summary>The configured hostname of the authentication server. This host is also used for service discovery</summary>
        private static string _authHostname = "api.edp.thomsonreuters.com";

        /// <summary>The configured port used when requesting from the authentication server.</summary>
        private static string _authPort = "443";

        /// <summary>The configured username used when requesting the token.</summary>
        private static string _username;

        /// <summary>The configured password used when requesting the token.</summary>
        private static string _password;

        /// <summary>The configured ApplicationID used when requesting the token.</summary>
        private static string _appID = "256";

        /// <summary>The configured scope used when requesting the token.</summary>
        private static string _scope = "trapi";

        /// <summary>The configured RIC used when requesting price data.</summary>
        private static string _ric = "/TRI.N";

        /// <summary>The IP address, used as the application's position when requesting the token.</summary>
        private static string _position;

        /// <summary>Amount of time until the authentication token expires; re-authenticate before then</summary>
        private static int _expiration_in_ms = Timeout.Infinite;

        /// <summary>indicates whether application should support hotstandby</summary>
        private static bool _hotstandby = false;

        /// <summary> Specifies a region to get endpoint(s) from the EDP-RT service discovery</summary>
        private static string _region = "amer";

        /// <summary>hosts returned by service discovery</summary>
        private static List<Tuple<string,string>> _hosts = new List<Tuple<string, string>>();

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

            /// <summary> URI to connect the WebSocket to. </summary>
            private Uri _uri;

            public WebSocketSession(String name, String host, String port)
            {
                Name = name;
                _uri = new Uri("wss://" + host + ":" + port + "/WebSocket");
                Canceling = false;
                Cts = new CancellationTokenSource();
                Console.WriteLine("Connecting to WebSocket " + _uri.AbsoluteUri + " ...");
            }

            /// <summary>
            /// Creates a WebSocket connection and sends a login request
            /// </summary>
            private void Connect()
            {
                IsLoggedIn = false;
                WebSocket = new ClientWebSocket();
                WebSocket.Options.SetBuffer(BUFFER_SIZE, BUFFER_SIZE);
                WebSocket.Options.AddSubProtocol("tr_json2");

                try
                {
                    WebSocket.ConnectAsync(_uri, CancellationToken.None).Wait();
                    SendLogin(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console_CancelKeyPress(this, null);
                }
            }

            /// <summary>
            /// Closes the existing connection and creates a new one.
            /// </summary>
            private void Reconnect()
            {
                Console.WriteLine("The WebSocket connection is closed for " + Name);
                if (!Canceling)
                {
                    Console.WriteLine("Reconnect to the endpoint for " + Name + " after 3 seconds...");
                    Thread.Sleep(3000);
                    WebSocket.Dispose();
                    Connect();
                }
            }

            /// <summary>
            /// The main loop of the WebSocketSession
            /// </summary>
            public void Run()
            {
                Connect();

                while (WebSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        ReceiveMessage();

                        if (WebSocket.State == WebSocketState.Aborted)
                        {
                            Reconnect();
                        }
                    }
                    catch (System.AggregateException)
                    {
                        Reconnect();
                    }
                }
            }

            /// <summary>
            /// Creates and sends a login message
            /// </summary>
            /// <param name="isRefresh">Setting <c>true</c> to not interest in the login refresh</param>
            public void SendLogin(bool isRefresh)
            {
                string msg = "{" + "\"ID\":1," + "\"Domain\":\"Login\"," + "\"Key\": {\"NameType\":\"AuthnToken\"," +
                    "\"Elements\":{\"ApplicationId\":\"" + _appID + "\"," + "\"Position\":\"" + _position + "\"," +
                    "\"AuthenticationToken\":\"" + _authToken + "\"}}";
                if (isRefresh)
                    msg += ",\"Refresh\": false";
                msg += "}";
                SendMessage(msg);
            }

            /// <summary>Prints the outbound message and sends it on the WebSocket.</summary>
            /// <param name="jsonMsg">Message to send</param>
            private void SendMessage(string jsonMsg)
            {
                /* Print the message (format the object string for easier reading). */
                Console.WriteLine("SENT on {0}:\n{1}\n", Name, JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonMsg), Formatting.Indented));

                var encoded = Encoding.ASCII.GetBytes(jsonMsg);
                var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

                WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, Cts.Token).Wait();
            }

            /// <summary>Reads data from the WebSocket and parses to JSON message</summary>
            private void ReceiveMessage()
            {
                var readBuffer = new ArraySegment<byte>(new byte[BUFFER_SIZE]);
                MemoryStream memoryStream = null;
                byte[] dataBuffer = null;

                while (true)
                {
                    var result = WebSocket.ReceiveAsync(readBuffer, Cts.Token);
                    if (result.IsFaulted)
                    {
                        Console_CancelKeyPress(this, null);
                    }
                    else
                    {
                        if (!result.Result.EndOfMessage)
                        {
                            if (memoryStream == null) memoryStream = new MemoryStream(BUFFER_SIZE * 5);

                            memoryStream.Write(readBuffer.Array, readBuffer.Offset, readBuffer.Count);
                            readBuffer = new ArraySegment<byte>(new byte[BUFFER_SIZE]);
                        }
                        else
                        {
                            if (memoryStream != null)
                            {
                                memoryStream.Write(readBuffer.Array, readBuffer.Offset, readBuffer.Count);
                                dataBuffer = memoryStream.GetBuffer();
                                memoryStream.Dispose();
                            }
                            else
                            {
                                dataBuffer = readBuffer.Array;
                            }
                            break;
                        }
                    }
                }

                /* Received message(s). */
                JArray messages = JArray.Parse(Encoding.ASCII.GetString(dataBuffer));
                /* Print the message (format the object string for easier reading). */
                Console.WriteLine("RECEIVED on {0}:\n{1}\n", Name, JsonConvert.SerializeObject(messages, Formatting.Indented));

                for (int index = 0; index < messages.Count; ++index)
                    ProcessJsonMsg(messages[index]);
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
                                SendMessage("{" + "\"ID\": 2," + "\"Key\": {\"Name\":\"" + _ric + "\"}" + "}");
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
            MarketPriceEdpGwServiceDiscoveryExample example = new MarketPriceEdpGwServiceDiscoveryExample();
            example.ParseCommandLine(args);
            example.Run();
        }

        /// <summary> Send an HTTP request to the specified authentication server, containing our username and password.
        /// The token will be used to login on the websocket. </summary>
        /// <returns><c>true</c> if success otherwise <c>false</c></returns>
        public static bool GetAuthenticationInfo(bool isRefresh)
        {
            string url = "https://" + _authHostname + ":" + _authPort + "/auth/oauth2/beta1/token";
            Console.WriteLine("Sending authentication request (isRefresh {0}) to {1}\n", isRefresh, url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

            webRequest.UserAgent = "CSharpMarketPriceEdpGwServiceDiscoveryExample";
            try
            {
                /* Send username and password in request. */
                string postString = "username=" + _username + "&takeExclusiveSignOnControl=True" + "&client_id=" + _username;
                if (isRefresh)
                    postString += "&grant_type=refresh_token&refresh_token=" + _refreshToken;
                else
                    postString += "&scope=" + _scope + "&grant_type=password&password=" + Uri.EscapeDataString(_password);

                byte[] postContent = Encoding.ASCII.GetBytes(postString);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = postContent.Length;

                System.IO.Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(postContent, 0, postContent.Length);
                requestStream.Dispose();

                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.GetResponseHeader("Transfer-Encoding").Equals("chunked") ||
                    webResponse.ContentLength > 0)
                {
                    /* If there is content in the response, print it. */
                    /* Format the object string for easier reading. */
                    dynamic msg = JsonConvert.DeserializeObject(new System.IO.StreamReader(webResponse.GetResponseStream()).ReadToEnd());
                    Console.WriteLine("RECEIVED:\n{0}", JsonConvert.SerializeObject(msg, Formatting.Indented));

                    // other possible items: auth_token, refresh_token, expires_in
                    _authToken = msg["access_token"].ToString();
                    _refreshToken = msg["refresh_token"].ToString();
                    if (Int32.TryParse(msg["expires_in"].ToString(), out _expiration_in_ms))
                        _expiration_in_ms *= 1000;
                }
                webResponse.Dispose();
                return true;
            }
            catch (WebException e)
            {
                /* The request to the authentication server failed, e.g. due to connection failure or HTTP error response. */
                if (e.InnerException != null)
                    Console.WriteLine("Authentication server request failed: {0} -- {1}\n", e.Message, e.InnerException.Message.ToString());
                else
                    Console.WriteLine("Authentication server request failed: {0}", e.Message);
            }
            return false;
        }

        /// <summary>
        /// Requests EDP service discovery to get endpoint(s) for EDP-RT
        /// </summary>
        /// <returns><c>true</c> if success otherwise <c>false</c></returns>
        public static bool DiscoverServices()
        {
            string url = "https://" + _authHostname + ":" + _authPort + "/streaming/pricing/v1/?transport=websocket";
            Console.WriteLine("Sending service discovery request to {0}\n", url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("Authorization", "Bearer " + _authToken);

            webRequest.UserAgent = "CSharpMarketPriceEdpGwServiceDiscoveryExample";
            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                if (webResponse.ContentLength > 0)
                {
                    /* If there is content in the response, print it. */
                    /* Format the object string for easier reading. */
                    dynamic msg = JsonConvert.DeserializeObject(new System.IO.StreamReader(webResponse.GetResponseStream()).ReadToEnd());
                    Console.WriteLine("RECEIVED:\n{0}", JsonConvert.SerializeObject(msg, Formatting.Indented));

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

                        if(_region.Equals("amer"))
                        {
                            if (((string)endpoints[i]["location"][0]).StartsWith("us-") == false)
                                continue;
                        }
                        else if (_region.Equals("emea"))
                        {
                            if (((string)endpoints[i]["location"][0]).StartsWith("eu-") == false)
                                continue;
                        }

                        if (_hotstandby && locations.Count == 1)
                        {
                            _hosts.Add(new Tuple<string,string>((endpoints[i]["endpoint"]).ToString(),(endpoints[i]["port"]).ToString()));
                            continue;
                        }
                        if (!_hotstandby && locations.Count == 2)
                        {
                            _hosts.Add(new Tuple<string, string>((endpoints[i]["endpoint"]).ToString(),(endpoints[i]["port"]).ToString()));
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
                            Console.WriteLine("No host found from EDP service discovery");
                            System.Environment.Exit(1);
                        }
                    }
                }
                webResponse.Dispose();
                return true;
            }
            catch (WebException e)
            {
                // service discovery failed
                if (e.InnerException != null)
                    Console.WriteLine("Service discovery request failed: {0} -- {1}\n", e.Message.ToString(), e.InnerException.Message.ToString());
                else
                    Console.WriteLine("Service discovery request failed: {0}", e.Message.ToString());
                Environment.Exit(1);
            }
            return false;
        }

        /// <summary>Runs the application. Retrives a token from the authentication server, then retrieves the list of services, and
        /// then opens the WebSocket(s) at the address(es) specified by service discovery using the token.</summary>
        public void Run()
        {
            /* Get local hostname. */
            IPAddress hostEntry = Array.Find(Dns.GetHostEntry(Dns.GetHostName()).AddressList, ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            _position = (hostEntry == null) ? "127.0.0.1/net" : hostEntry.ToString();

            if (!GetAuthenticationInfo(false) || !DiscoverServices())
                Environment.Exit(1);

            Console.CancelKeyPress += Console_CancelKeyPress;

            /* Open websocket(s) */
            foreach (var host in _hosts)
            {
                var webSocketSession = new WebSocketSession("Session" + (_webSocketSessions.Count + 1), host.Item1, host.Item2);
                _webSocketSessions.Add(webSocketSession.Name, webSocketSession);

                Task.Factory.StartNew(() =>
                {
                    webSocketSession.Run();
                });

                // if hotstandby is not supported, stop after connecting to the first host in hosts. Otherwise,
                // connect to the first two hosts
                if (_webSocketSessions.Count == 2 || !_hotstandby)
                    break;
            }

            // after 90% of the time allowed before the token expires, retrive a new set of tokens and send a login to each open websocket
            while (true)
            {
                Thread.Sleep((int)(_expiration_in_ms * .90));

                // refresh authentication token; if refresh attempt fails, try full auth
                if (!GetAuthenticationInfo(true))
                    if (!GetAuthenticationInfo(false))
                        Console_CancelKeyPress(null, null);

                foreach (var webSocketSession in _webSocketSessions)
                {
                    if(webSocketSession.Value.IsLoggedIn)
                        webSocketSession.Value.SendLogin(true);
                }
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
            Console.WriteLine("Exiting...");
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
                    case "--auth_hostname":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _authHostname = args[++i];
                        break;
                    case "--auth_port":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _authPort = args[++i];
                        break;
                    case "--hotstandby":
                        _hotstandby = true;
                        break;
                    case "--password":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _password = args[++i];
                        break;
                    case "--region":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _region = args[++i];
                        if(!_region.Equals("amer") && !_region.Equals("emea"))
                        {
                            Console.WriteLine("Unknown region \"" + _region + "\". The region must be either \"amer\" or \"emea\".");
                            Environment.Exit(1);
                        }
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
                    case "--user":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            PrintCommandLineUsageAndExit();
                        }
                        _username = args[++i];
                        break;
                    default:
                        Console.WriteLine("Unknown option: {0}", args[i]);
                        PrintCommandLineUsageAndExit();
                        break;
                }
            }

            if (_username == null || _password == null)
            {
                Console.WriteLine("both password and user must be specified on the command line");
                PrintCommandLineUsageAndExit();
            }
        }

        /// <summary>Prints usage information. Used when arguments cannot be parsed.</summary>
        void PrintCommandLineUsageAndExit()
        {
            Console.WriteLine("Usage: {0} [--app_id appID] [--auth_hostname hostname] [--auth_port port] [--hotstandby] [--password password] [--region region] [--ric ric] [--scope scope] [--user user]", System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(1);
        }
    }
}
