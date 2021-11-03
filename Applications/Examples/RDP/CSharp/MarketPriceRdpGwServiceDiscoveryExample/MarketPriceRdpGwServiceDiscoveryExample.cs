﻿//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|            Copyright (C) 2018-2021 Refinitiv. All rights reserved.        --
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

/*
 * This example demonstrates authenticating via Refinitiv Data Platform, using an
 * authentication token to discover Refinitiv Real-Time service endpoint, and
 * using the endpoint and authentitcation to retrieve market content.  
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
 * - Periodically proactively re-authenticating to Refinitiv Data Platform, and
 *   providing the updated token to the Real-Time endpoint before token expiration.
 */

namespace MarketPriceRdpGwServiceDiscoveryExample
{
    static class Policy
    {
        public const int passwordLengthMask = 0x1;
        public const int passwordUppercaseLetterMask = 0x2;
        public const int passwordLowercaseLetterMask = 0x4;
        public const int passwordDigitMask = 0x8;
        public const int passwordSpecialCharacterMask = 0x10;
        public const int passwordInvalidCharacterMask = 0x20;

        // Default password policy
        public const int passwordLengthMin = 30;
        public const int passwordUppercaseLetterMin = 1;
        public const int passwordLowercaseLetterMin = 1;
        public const int passwordDigitMin = 1;
        public const int passwordSpecialCharacterMin = 1;
        public const String passwordSpecialCharacterSet = "~!@#$%^&*()-_=+[]{}|;:,.<>/?";
        public const int passwordMinNumberOfCategories = 3;
    }

    class MarketPriceRdpGwServiceDiscoveryExample
    {
        /// <summary>The websocket(s) used for retrieving market content.</summary>
        private static Dictionary<string, WebSocketSession> _webSocketSessions = new Dictionary<string, WebSocketSession>();

        /// <summary>The tokens retrieved from the authentication server.
        private static string _authToken;
        private static string _refreshToken;

        /// <summary>The full URL of the authentication server. If not specified,
        /// https://api.refinitiv.com:443/auth/oauth2/v1/token is used.</summary>
        private static string _authUrl = "https://api.refinitiv.com:443/auth/oauth2/v1/token";

        /// <summary>The full URL of the Refinitiv Data Platform service discovery server. If not specified,
        /// https://api.refinitiv.com/streaming/pricing/v1/ is used.</summary>
        private static string _discoveryUrl = "https://api.refinitiv.com/streaming/pricing/v1/";

        /// <summary>The configured username used when requesting the token.</summary>
        private static string _username;

        /// <summary>The configured client ID used when requesting the token.</summary>
        private static string _clientId;

        /// <summary>The configured password used when requesting the token.</summary>
        private static string _password;

        /// <summary>New password provided by user to change.</summary>
        private string _newPassword;

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

        /// <summary>Amount of time until the authentication token expires; re-authenticate before then</summary>
        private static int _expiration_in_ms = Timeout.Infinite;

        /// <summary>Expiration time returned by password (ho refresh) request</summary>
        private int _original_expiration_in_ms = Timeout.Infinite;

        /// <summary>indicates whether application should support hotstandby</summary>
        private static bool _hotstandby = false;

        /// <summary> Specifies a region to get endpoint(s) from the Refinitiv Data Platform service discovery</summary>
        private static string _region = "us-east-1";

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
                        Console.WriteLine("RECEIVED on {0}:\n{1}\n", Name, JsonConvert.SerializeObject(messages, Formatting.Indented));

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
            MarketPriceRdpGwServiceDiscoveryExample example = new MarketPriceRdpGwServiceDiscoveryExample();
            example.ParseCommandLine(args);
            example.Run();
        }

        /// <summary> Send an HTTP request to the specified authentication server, containing our username and password.
        /// The token will be used to login on the websocket. </summary>
        /// <returns><c>true</c> if success otherwise <c>false</c></returns>
        public bool GetAuthenticationInfo(bool isRefresh, string url=null)
        {
            if (string.IsNullOrEmpty(url))
                url = _authUrl;

            Console.WriteLine("Sending authentication request (isRefresh {0}) to {1}", isRefresh, url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

            try
            {
                /* Send username and password in request. */
                string postString = "username=" + _username + "&client_id=" + _clientId;
                if (isRefresh)
                    postString += "&grant_type=refresh_token&refresh_token=" + _refreshToken;
                else
                {
                    postString += "&takeExclusiveSignOnControl=True";
                    postString += "&scope=" + _scope + "&grant_type=password&password=" + Uri.EscapeDataString(_password);
                }

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
                    Console.WriteLine("RECEIVED:\n{0}\n", JsonConvert.SerializeObject(msg, Formatting.Indented));

                    // other possible items: auth_token, refresh_token, expires_in
                    _authToken = msg["access_token"].ToString();
                    _refreshToken = msg["refresh_token"].ToString();
                    if (Int32.TryParse(msg["expires_in"].ToString(), out _expiration_in_ms))
                        _expiration_in_ms *= 1000;
                    if (!isRefresh)
                        _original_expiration_in_ms = _expiration_in_ms;

                    webResponse.Close();
                    return true;
                }
                else
                {
                    webResponse.Close();
                    return false;
                }

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
                        case HttpStatusCode.Moved:             // 301
                        case HttpStatusCode.Redirect:          // 302
                        case HttpStatusCode.TemporaryRedirect: // 307
                        case (HttpStatusCode)308:              // 308 Permanent Redirect
                            // Perform URL redirect
                            Console.WriteLine("Refinitiv Data Platform authentication HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            string newHost = response.Headers.Get("Location");
                            if (!string.IsNullOrEmpty(newHost))
                                ret = GetAuthenticationInfo(isRefresh, newHost);
                            break;
                        case HttpStatusCode.BadRequest:        // 400
                        case HttpStatusCode.Unauthorized:      // 401
                            // Retry with username and password
                            Console.WriteLine("Refinitiv Data Platform authentication HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            if (isRefresh)
                            {
                                Console.WriteLine("Retry with username and password");
                                ret = GetAuthenticationInfo(false);
                            }
                            else
                                ret = false;
                            break;
                        case HttpStatusCode.Forbidden:         // 403
                        case (HttpStatusCode)451:              // 451 Unavailable For Legal Reasons
                            // Stop retrying with the request
                            Console.WriteLine("Refinitiv Data Platform authentication HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            Console.WriteLine("Stop retrying with the request");
                            ret = false;
                            break;
                        default:
                            // Retry the request to Refinitiv Data Platform 
                            Console.WriteLine("Refinitiv Data Platform authentication HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            Console.WriteLine("Retry the request to Refinitiv Data Platform");
                            ret = GetAuthenticationInfo(isRefresh);
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
            if(string.IsNullOrEmpty(url))
              url = _discoveryUrl;

            string param_url = url + "?transport=websocket";

            Console.WriteLine("Sending service discovery request to {0}\n", param_url);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(param_url);
            webRequest.Headers.Add("Authorization", "Bearer " + _authToken);
            webRequest.AllowAutoRedirect = false;
            webRequest.UserAgent = "CSharpMarketPriceRdpGwServiceDiscoveryExample";
            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.GetResponseHeader("Transfer-Encoding").Equals("chunked") || webResponse.ContentLength > 0)
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

                        if (((string)endpoints[i]["location"][0]).StartsWith(_region) == false)
                            continue;

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
                            Console.WriteLine("Expected 2 hosts but received: {0} or the region: {1} is not present in list of endpoints\n", _hosts.Count, _region);
                            System.Environment.Exit(1);
                        }
                    }
                    else
                    {
                        if (_hosts.Count == 0)
                        {
                            Console.WriteLine("The region: {0} is not present in list of endpoints\n", _region);
                            System.Environment.Exit(1);
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
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
                        case HttpStatusCode.Moved:             // 301
                        case HttpStatusCode.Redirect:          // 302
                        case HttpStatusCode.RedirectMethod:    // 303
                        case HttpStatusCode.TemporaryRedirect: // 307
                        case (HttpStatusCode)308:              // 308 Permanent Redirect
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
                        case HttpStatusCode.Forbidden:         // 403
                        case (HttpStatusCode)451:              // 451 Unavailable For Legal Reasons
                            // Stop retrying with the request
                            Console.WriteLine("Refinitiv Data Platform service discovery HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            Console.WriteLine("Stop retrying with the request");
                            ret = false;
                            break;
                        default:
                            // Retry the service discovery request
                            Console.WriteLine("Refinitiv Data Platform service discovery HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            Console.WriteLine("Retry the service discovery request");
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

                if (!GetAuthenticationInfo(true))
                    Console_CancelKeyPress(null, null);
                if (_expiration_in_ms != _original_expiration_in_ms)
                {
                    System.Console.WriteLine("expire time changed from " + _original_expiration_in_ms / 1000
                        + " sec to " + _expiration_in_ms / 1000 + " sec; retry with password");
                    if (!GetAuthenticationInfo(false))
                        Console_CancelKeyPress(null, null);
                }

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

        /// <summary>
        /// This gets called when an option that requires an argument is called
        /// without one. Prints usage and exits with a failure status.
        /// </summary>
        /// <param name="option"></param>
        void GripeAboutMissingOptionArgumentAndExit(string option)
        {
            Console.WriteLine("Error: {0} requires an argument.", option);
            PrintCommandLineUsageAndExit(1);
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
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _appID = args[++i];
                        break;
                    case "--auth_url":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _authUrl = args[++i];
                        break;
                    case "--discovery_url":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _discoveryUrl = args[++i];
                        break;
                    case "--hotstandby":
                        _hotstandby = true;
                        break;
                    case "--password":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _password = args[++i];
                        break;
                    case "--newPassword":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _newPassword = args[++i];
                        break;
                    case "--region":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _region = args[++i];
                        break;
                    case "--ric":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _ric = args[++i];
                        break;
                    case "--scope":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _scope = args[++i];
                        break;
                    case "--user":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _username = args[++i];
                        break;
                    case "--clientid":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _clientId = args[++i];
                        break;
                    case "--service":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _service = args[++i];
                        break;
                    case "--help":
                        PrintCommandLineUsageAndExit(0);
                        break;
                    default:
                        Console.WriteLine("Unknown option: {0}", args[i]);
                        PrintCommandLineUsageAndExit(1);
                        break;
                }
            }

            if (_username == null || _password == null || _clientId == null)
            {
                Console.WriteLine("User, password and clientid must be specified on the command line");
                PrintCommandLineUsageAndExit(1);
            }

            if (!(_newPassword == null))
            {
                int result = checkNewPassword(_newPassword);

                if ((result & Policy.passwordInvalidCharacterMask) != 0)
                {
                    Console.WriteLine("New password contains invalid symbol\n" +
                        "valid symbols are [A-Z][a-z][0-9]" + Policy.passwordSpecialCharacterSet);
                    Environment.Exit(1);
                }

                if ((result & Policy.passwordLengthMask) != 0)
                {
                    Console.WriteLine("New password length should be at least "
                        + Policy.passwordLengthMin + " characters");
                    Environment.Exit(1);
                }

                int countCategories = 0;
                for (int mask = Policy.passwordUppercaseLetterMask;
                    mask <= Policy.passwordSpecialCharacterMask; mask <<= 1)
                {
                    if ((result & mask) == 0)
                    {
                        countCategories++;
                    }
                }
                if (countCategories < Policy.passwordMinNumberOfCategories)
                {
                    Console.WriteLine("Password must contain characters belonging to at least "
                        + Policy.passwordMinNumberOfCategories
                        + " of the following four categories:\n"
                        + "uppercase letters, lowercase letters, digits, and special characters.\n");
                    Environment.Exit(1);
                }

                if (ChangePassword())
                {
                    Console.WriteLine("Password successfully changed");
                    _password = _newPassword;
                    _newPassword = null;
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        }

        /// <summary>Prints usage information. Used when arguments cannot be parsed.</summary>
        void PrintCommandLineUsageAndExit(int exitStatus)
        {
            Console.WriteLine("Usage:\n" +
                "dotnet {0}.dll\n" +
                "   [--app_id appID]                    \n" +
                "   [--auth_url auth_url]               \n" +
                "   [--discovery_url discovery_url]     \n" +
                "   [--hotstandby]                      \n" +
                "   [--password password]               \n" +
                "   [--newPassword new_password]        \n" +
                "   [--region region]                   \n" +
                "   [--ric ric]                         \n" +
                "   [--scope scope]                     \n" +
                "   [--user user]                       \n" +
                "   [--clientid clientID]               \n" +
                "   [--service service]                 \n" +
                "   [--help]                            \n",
                System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(exitStatus);
        }

        /// <summary>Recognises  characteristics of proposed new password.</summary>
        /// <returns>set of bits describing the results of the check.</returns>
        public static int checkNewPassword(string pwd)
        {
            int result = 0;

            if (pwd.Length < Policy.passwordLengthMin)
            {
                result |= Policy.passwordLengthMask;
            }

            int countUpper = 0;
            int countLower = 0;
            int countDigit = 0;
            int countSpecial = 0;

            for (int i = 0; i < pwd.Length; i++)
            {
                char c = pwd[i];
                if (!Regex.IsMatch(new string(c, 1), "[A-Za-z0-9]")
                    && !Policy.passwordSpecialCharacterSet.Contains(c))
                {
                    result |= Policy.passwordInvalidCharacterMask;
                }
                if (Char.IsUpper(c))
                {
                    countUpper++;
                }
                if (Char.IsLower(c))
                {
                    countLower++;
                }
                if (Char.IsDigit(c))
                {
                    countDigit++;
                }

                if (Policy.passwordSpecialCharacterSet.Contains(c))
                {
                    countSpecial++;
                }
            }

            if (countUpper < Policy.passwordUppercaseLetterMin)
            {
                result |= Policy.passwordUppercaseLetterMask;
            }
            if (countLower < Policy.passwordLowercaseLetterMin)
            {
                result |= Policy.passwordLowercaseLetterMask;
            }
            if (countDigit < Policy.passwordDigitMin)
            {
                result |= Policy.passwordDigitMask;
            }
            if (countSpecial < Policy.passwordSpecialCharacterMin)
            {
                result |= Policy.passwordSpecialCharacterMask;
            }

            return result;
        }

        /// <summary> Send change password request to the  authentication server.</summary>
        /// <returns><c>true</c> if success otherwise <c>false</c></returns>
        public bool ChangePassword()
        {
            Console.WriteLine("Sending change password request to " + _authUrl);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_authUrl);

            try
            {
                string postString = "username=" + _username + "&client_id=" + _clientId;
                postString += "&takeExclusiveSignOnControl=True";
                postString += "&scope=" + _scope + "&grant_type=password&password=" + Uri.EscapeDataString(_password);
                postString += "&newPassword=" + Uri.EscapeDataString(_newPassword);

                byte[] postContent = Encoding.ASCII.GetBytes(postString);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = postContent.Length;
                webRequest.AllowAutoRedirect = false;

                System.IO.Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(postContent, 0, postContent.Length);
                requestStream.Close();

                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

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
                        case HttpStatusCode.Moved:             // 301
                        case HttpStatusCode.Redirect:          // 302
                        case HttpStatusCode.TemporaryRedirect: // 307
                        case (HttpStatusCode)308:              // 308 Permanent Redirect
                            // Perform URL redirect
                            Console.WriteLine("Request to aurh server is redirected");
                            string newHost = response.Headers.Get("Location");
                            if (!string.IsNullOrEmpty(newHost))
                                ret = ChangePassword();
                            break;
                        case HttpStatusCode.BadRequest:        // 400
                        case HttpStatusCode.Unauthorized:      // 401
                        case HttpStatusCode.Forbidden:         // 403
                        case (HttpStatusCode)451:              // 451 Unavailable For Legal Reasons
                            // Error of changing password
                            Console.WriteLine("Change password error");
                            if (response.ContentLength > 0)
                            {
                                /* If there is content in the response, print it. */
                                dynamic msg = JsonConvert.DeserializeObject(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
                                Console.WriteLine("RECEIVED:\n{0}\n", JsonConvert.SerializeObject(msg, Formatting.Indented));
                            }
                            ret = false;
                            break;
                        default:
                            // Retry the request to the API gateway
                            Console.WriteLine("Error changing password. Receive HTTP code: {0} {1}\n", statusCode, response.StatusDescription);
                            Console.WriteLine("Retry the request to the API gateway");
                            ret = ChangePassword();
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
    }
}
