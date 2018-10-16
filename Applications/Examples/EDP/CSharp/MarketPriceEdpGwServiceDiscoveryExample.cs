//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright Thomson Reuters 2018. All rights reserved.            --
//|-----------------------------------------------------------------------------


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using WebSocketSharp;

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
        private List<Tuple<String, WebSocket>> _webSockets = new List<Tuple<String, WebSocket>>();

        /// <summary>Indicates whether we have successfully logged in.</summary>
        private bool _loggedIn = false;

        /// <summary>The tokens retrieved from the authentication server.
        private string _authToken;
        private string _refreshToken;

        /// <summary>The configured hostname of the authentication server. This host is also used for service discovery</summary>
        private string _authHostName = "api.edp.thomsonreuters.com";

        /// <summary>The configured port used when requesting from the authentication server.</summary>
        private string _authPort = "443";

        /// <summary>The configured username used when requesting the token.</summary>
        private string _userName;

        /// <summary>The configured password used when requesting the token.</summary>
        private string _password;

        /// <summary>The configured ApplicationID used when requesting the token.</summary>
        private string _appId = "256";

        /// <summary>The configured scope used when requesting the token.</summary>
        private string _scope = "trapi";

        /// <summary>The configured RIC used when requesting price data.</summary>
        private string _ric = "/TRI.N";

        /// <summary>The IP address, used as the application's position when requesting the token.</summary>
        private string _position;

        /// <summary>Amount of time until the authentication token expires; re-authenticate before then</summary>
        private int _expirationInMilliSeconds = Timeout.Infinite;

        /// <summary>indicates whether application should support hotstandby</summary>
        private bool hotStandbySupported = false;

        /// <summary>hosts returned by service discovery</summary>
        private LinkedList<String> hosts = new LinkedList<string>();

        /// <summary>Parses commandline config and runs the application.</summary>
        static void Main(string[] args)
        {
            MarketPriceEdpGwServiceDiscoveryExample example = new MarketPriceEdpGwServiceDiscoveryExample();
            example.parseCommandLine(args);
            example.run();
            Console.WriteLine("done");
        }

        /* Send an HTTP request to the specified authentication server, containing our username and password.
         * The token will be used to login on the websocket.  */
        public bool getAuthenticationInfo(bool isRefresh)
        {
            string url = "https://" + _authHostName + ":" + _authPort + "/auth/oauth2/beta1/token";
            Console.WriteLine("Sending authentication request (isRefresh {0}) to {1}\n", isRefresh, url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

            webRequest.UserAgent = "CSharpMarketPriceEdpGwServiceDiscoveryExample";
            try
            {
                /* Send username and password in request. */
                string postString = "username=" + _userName + "&takeExclusiveSignOnControl=True" + "&client_id=" + _userName;
                if (isRefresh)
                    postString += "&grant_type=refresh_token&refresh_token=" + _refreshToken;
                else
                    postString += "&scope=" + _scope + "&grant_type=password&password=" + _password;

                byte[] postContent = Encoding.ASCII.GetBytes(postString);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = postContent.Length;

                System.IO.Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(postContent, 0, postContent.Length);
                requestStream.Close();
                Console.WriteLine(webRequest.Headers.ToString());
                Console.WriteLine(postString);

                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.ContentLength > 0)
                {
                    /* If there is content in the response, print it. */
                    /* Format the object string for easier reading. */
                    dynamic msg = JsonConvert.DeserializeObject(new System.IO.StreamReader(webResponse.GetResponseStream()).ReadToEnd());
                    Console.WriteLine("RECEIVED:\n{0}", JsonConvert.SerializeObject(msg, Formatting.Indented));

                    // other possible items: auth_token, refresh_token, expires_in
                    _authToken = msg["access_token"].ToString();
                    _refreshToken = msg["refresh_token"].ToString();
                    if (Int32.TryParse(msg["expires_in"].ToString(), out _expirationInMilliSeconds))
                        _expirationInMilliSeconds *= 1000;
                }
                webResponse.Close();
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

        public bool discoverServices()
        {
            string url = "https://" + _authHostName + ":" + _authPort + "/streaming/pricing/v1/?transport=websocket";
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

                        /* algo: if a service has one location, then it represents an endpoint used to support hot standby. Users are
                         * expected to connect to multiple endpoints to activate hot standby, so this example fails if only one service
                         * exists with a single location and the hot standby feature is active.
                         *
                         * if a service has two locations, then it is not a server that supports hot standby, but instead a load balancer
                         * with multiple endpoints behind it represented by the locations. Users connect to the endpoint and port
                         * indicated for the service.
                         *
                         * Services not fitting this pattern are ignored
                         */
                        if (hotStandbySupported && locations.Count == 1)
                        {
                            hosts.AddLast((String)(endpoints[i]["endpoint"]) + ":" + (String)(endpoints[i]["port"]));
                            continue;
                        }
                        if (!hotStandbySupported && locations.Count == 2)
                        {
                            hosts.AddLast((String)(endpoints[i]["endpoint"]) + ":" + (String)(endpoints[i]["port"]));
                            continue;
                        }
                    }

                    // minor sanity checking to ensure we have enough hosts
                    if (hosts.Count == 0)
                    {
                        Console.WriteLine("no hosts specified");
                        System.Environment.Exit(1);
                    }
                    if (hotStandbySupported && hosts.Count < 2)
                    {
                        Console.WriteLine("hotstandby support requires at least two hosts");
                        System.Environment.Exit(1);
                    }
                }
                webResponse.Close();
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
        public void run()
        {
            /* Get local hostname. */
            IPAddress hostEntry = Array.Find(Dns.GetHostEntry(Dns.GetHostName()).AddressList, ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            _position = (hostEntry == null) ? "127.0.0.1" : hostEntry.ToString();

            if (!getAuthenticationInfo(false) || !discoverServices())
                Environment.Exit(1);

            /* Open websocket(s) */
            LinkedList<String>.Enumerator I = hosts.GetEnumerator();
            while (I.MoveNext())
            {
                string hostString = "wss://" + I.Current.ToString() + "/WebSocket";
                Console.WriteLine("Connecting to WebSocket " + hostString + " ...");
                WebSocket _webSocket;
                _webSocket = new WebSocket(hostString, "tr_json2");

                _webSocket.OnOpen += onWebSocketOpened;
                _webSocket.OnError += onWebSocketError;
                _webSocket.OnClose += onWebSocketClosed;
                _webSocket.OnMessage += onWebSocketMessage;

                /* Print any log events (similar to default behavior, but we explicitly indicate it's a logger event). */
                _webSocket.Log.Output = (logData, text) => Console.WriteLine("Received Log Event (Level: {0}): {1}\n", logData.Level, logData.Message);

                _webSocket.Connect();
                _webSockets.Add(new Tuple<string, WebSocket>(hostString, _webSocket));

                // if hotstandby is not supported, stop after connecting to the first host in hosts. Otherwise,
                // connect to the first two hosts
                if (_webSockets.Count == 2 || !hotStandbySupported)
                    break;
            }

            // after 90% of the time allowed before the token expires, retrive a new set of tokens and send a login to each open websocket
            while (true)
            {
                Thread.Sleep((int)(_expirationInMilliSeconds * .90));
                if (_loggedIn)
                {
                    // refresh authentication token; if refresh attempt fails, try full auth
                    if (!getAuthenticationInfo(true))
                        if (!getAuthenticationInfo(false))
                            Environment.Exit(1);

                    foreach( Tuple<string, WebSocket> _webSocket in _webSockets)
                        sendLogin(_webSocket.Item2, true);
                }
            }
        }

        private void sendLogin(WebSocket _webSocket, bool isRefresh)
        {
            //string msg;
            string msg = "{" + "\"ID\":1," + "\"Domain\":\"Login\"," + "\"Key\": {\"NameType\":\"AuthnToken\"," +
                "\"Elements\":{\"ApplicationId\":\"" + _appId + "\"," + "\"Position\":\"" + _position + "\"," +
                "\"AuthenticationToken\":\"" + _authToken + "\"}}";
            if (isRefresh)
                msg += ",\"Refresh\": false";
            msg += "}";
            sendMessage(_webSocket, msg);
        }

        /// <summary>Handles the initial open of the WebSocket.</summary>
        private void onWebSocketOpened(object sender, EventArgs e)
        {
            Console.WriteLine("{0} WebSocket successfully connected", DateTime.Now.ToString("HH:mm:ss.fff"));

            // create JSON string with token, app id, and position and send it over the web socket
            sendLogin((WebSocket) sender, false);
        }

        /// <summary>Handles messages received on the websocket.</summary>
        private void onWebSocketMessage(object sender, MessageEventArgs e)
        {
            /* Received message(s). */
            JArray messages = JArray.Parse(e.Data);
            Console.WriteLine("RECEIVED:\n{0}", JsonConvert.SerializeObject(messages, Formatting.Indented));

            for (int index = 0; index < messages.Count; ++index)
                processJsonMsg((WebSocket)sender, messages[index]);
        }

        /// <summary>Handles the WebSocket closing.</summary>
        private void onWebSocketClosed(object sender, CloseEventArgs e)
        {
            foreach (Tuple<string, WebSocket> _webSocket in _webSockets)
            {
                if (_webSocket.Item2 == (WebSocket)sender) {
                    Console.WriteLine("{0} WebSocket [{1}] disconnected: {2}", DateTime.Now.ToString("HH:mm:ss.fff"), _webSocket.Item1,
                        e.Reason);
                    while(true)
                    {
                        Thread.Sleep(3000);
                        Console.WriteLine("{0} reconnecting to {1}", DateTime.Now.ToString("HH:mm:ss.fff"), _webSocket.Item1);
                        try {
                            _webSocket.Item2.Connect();
                        }
                        catch (InvalidOperationException msg) {
                            Console.WriteLine("failed while reconnecting to {0}; message was {1}", _webSocket.Item1, msg);
                            Environment.Exit(1);
                        }
                        if (_webSocket.Item2.ReadyState == WebSocketState.Open)
                            return;
                        else
                        {
                            Console.WriteLine("after reconnecting, websocket for {0} in unexpected state {1}", _webSocket.Item1, _webSocket.Item2.ReadyState);
                            Environment.Exit(1);
                        }
                    }
                }
            }

            // should not get here
            Console.WriteLine("internal error: received disconnect for unknown websocket");
            Environment.Exit(1);
        }

        /// <summary>Handles any error that occurs on the WebSocket.</summary>
        private void onWebSocketError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Received Error: {0}", e.Exception.ToString());
            Environment.Exit(1);
        }

        /// <summary>
        /// Processes the received message. If the message is a login response indicating we are now logged in,
        /// opens a stream for price content.
        /// </summary>
        /// <param name="msg">The received JSON message</param>
        void processJsonMsg(WebSocket _webSocket, dynamic msg)
        {
            switch((string)msg["Type"])
            {
                case "Refresh":
                    if ((string)msg["Domain"] == "Login")
                    {
                        if (msg["State"] != null && (string)msg["State"]["Stream"] != "Open")
                        {
                            Console.WriteLine("Login stream was closed.\n");
                            Environment.Exit(1);
                        }

                        if ((msg["State"] == null || (string)msg["State"]["Data"] == "Ok"))
                        {
                            // Login was successful. Request an item
                            _loggedIn = true;
                            sendMessage(_webSocket, "{" + "\"ID\": 2," + "\"Key\": {\"Name\":\"" + _ric + "\"}" + "}");
                        }
                    }
                    break;
                case "Status":
                    if (msg["Domain"] != null && (string)msg["Domain"] == "Login" &&
                        msg["State"] != null && (string)msg["State"]["Stream"] != null && (string)msg["State"]["Stream"] != "Open")
                    {
                        Console.WriteLine("stream is no longer open (state is {0})", (string)msg["State"]["Stream"]);
                        Environment.Exit(1);
                    }
                    break;
                case "Ping":
                    sendMessage(_webSocket, "{\"Type\":\"Pong\"}");
                    break;
                default:
                    break;
            }

        }

        /// <summary>Prints the outbound message and sends it on the WebSocket.</summary>
        /// <param name="jsonMsg">Message to send</param>
        void sendMessage(WebSocket _webSocket, string jsonMsg)
        {
            /* Print the message (format the object string for easier reading). */
            Console.WriteLine("SENT:\n{0}", JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonMsg), Formatting.Indented));

            _webSocket.Send(jsonMsg);
        }


        /// <summary>Parses command-line arguments.</summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        void parseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "--app_id":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _appId = args[++i];
                        break;
                    case "--auth_hostname":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _authHostName = args[++i];
                        break;
                    case "--auth_port":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _authPort = args[++i];
                        break;
                    case "--hotstandby":
                        hotStandbySupported = true;
                        break;
                    case "--password":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _password = args[++i];
                        break;
                    case "--ric":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _ric = args[++i];
                        break;
                    case "--scope":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _scope = args[++i];
                        break;
                    case "--user":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _userName = args[++i];
                        break;
                    default:
                        Console.WriteLine("Unknown option: {0}", args[i]);
                        printCommandLineUsageAndExit();
                        break;
                }
            }

            if (_userName == null || _password == null)
            {
                Console.WriteLine("both password and user must be specified on the command line");
                printCommandLineUsageAndExit();
            }
        }

        /// <summary>Prints usage information. Used when arguments cannot be parsed.</summary>
        void printCommandLineUsageAndExit()
        {
            Console.WriteLine("Usage: {0} [--app_id appID] [--auth_hostname hostname] [--auth_port port] [--hotstandby] [--password password] [--ric ric] [--scope scope] [--user user]", System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(1);
        }
    }
}
