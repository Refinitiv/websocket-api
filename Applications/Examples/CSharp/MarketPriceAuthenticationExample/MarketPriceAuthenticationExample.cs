﻿//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.
//|                See the project's LICENSE.md for details.        
//|            Copyright (C) 2018-2024 LSEG. All rights reserved.    
//|-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
 * This example demonstrates authenticating via LSEG Delivery Platform (LDP), using an
 * authentication token and a LSEG Real-Time service endpoint to retrieve
 * market content. Specifically, for oAuthPasswordGrant authentication, this
 * application uses password grant type or refresh_token grant token in auth
 * request to LDP (auth/oauth2/v1/token) using LSEG provided credentials:
 * username (machine ID) and password. A client id is
 * generated by customers using the app-generator tool.
 *
 * This example maintains a session by proactively renewing the authentication
 * token before expiration.
 *
 * It performs the following steps:
 * - Authenticating via HTTP Post request to LSEG Delivery Platform
 * - Opening a WebSocket to a specified LSEG Real-Time Service endpoint (host/port)
 * - Sending Login into the Real-Time Service using the token retrieved
 *   from LSEG Delivery Platform.
 * - Requesting market-price content.
 * - Printing the response content.
 * - Periodically proactively re-authenticating to LSEG Delivery Platform, and
 *   providing the updated token to the Real-Time endpoint before token expiration.
 */


namespace MarketPriceAuthenticationExample
{

    class MarketPriceAuthenticationExample
    {
        /// <summary>The websocket used for retrieving market content.</summary>
        private ClientWebSocket _webSocket;

        /// <summary>Indicates whether we have successfully logged in.</summary>
        private bool _loggedIn = false;

        /// <summary>The tokens retrieved from the authentication server.
        private string _authToken;

        /// <summary>The configured hostname of the Websocket server.</summary>
        private string _hostName = "localhost";

        /// <summary>The configured port used when opening the WebSocket.</summary>
        private string _port = "15000";

        /// <summary>The configured hostname of the authentication server. If not specified, the same hostname as
        /// as the WebSocket server is used.</summary>
        private string _authHostName = null;

        /// <summary>The configured port used when requesting from the authentication server.</summary>
        private string _authPort = "8443";

        /// <summary>The configured username used when requesting the token.</summary>
        private string _userName;

        /// <summary>The configured client ID used when requesting the token.</summary>
        private string _clientId;

        /// <summary>The configured password used when requesting the token.</summary>
        private string _password;

        /// <summary>The configured ApplicationID used when requesting the token.</summary>
        private string _appId = "555";

        /// <summary>The configured scope used when requesting the token.</summary>
        private string _scope = "trapi.streaming.pricing.read";

        /// <summary>The configured RIC used when requesting price data.</summary>
        private string _ric = "/TRI.N";

        /// <summary>The requested service name or service ID.</summary>
        private string _service = "ELEKTRON_DD";

        /// <summary>The IP address, used as the application's position when requesting the token.</summary>
        private string _position;

        /// <summary> Specifies buffer size for each read from WebSocket.</summary>
        private static readonly int BUFFER_SIZE = 8192;

        /// <summary> This is used to cancel operations when something goes wrong. </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        static void Main(string[] args)
        {
            MarketPriceAuthenticationExample example = new MarketPriceAuthenticationExample();
            example.ParseCommandLine(args);
            example.Run();
        }

        /// <summary>
        /// HttpClientHandler is intended to be instantiated once per application, rather than per-use. See Remarks.
        /// </summary>
        static readonly HttpClientHandler httpHandler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            /* Add a cookie container to the request, so that we can get the token from the response cookies. */
            CookieContainer = new CookieContainer(),
            /* TODO Remove this. It disables certificate validation. */
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => { return true; }
        };

        /// <summary>
        /// HttpClient is intended to be instantiated once per application, rather than per-use. See Remarks.
        /// </summary>
        static readonly HttpClient httpClient = new HttpClient(httpHandler);

        /// <summary> Send an HTTP request to the specified authentication server, containing our username and password.
        /// The token will be used to login on the websocket. </summary>
        /// <returns><c>true</c> if success otherwise <c>false</c></returns>
        public bool GetAuthenticationInfo(bool isRefresh, string url = null)
        {
            if (string.IsNullOrEmpty(url))
                url = "https://" + _authHostName + ":" + _authPort + "/getToken";

            Console.WriteLine("Sending authentication request (isRefresh {0}) to {1}", isRefresh, url);

            var headers = httpClient.DefaultRequestHeaders;
            headers.UserAgent.TryParseAdd("CSharpMarketPriceAuthenticationExample");

            try
            {
                /* Send username and password in request. */
                string postString = "username=" + _userName + "&password=" + _password;

                var content = new StringContent(postString, Encoding.UTF8, "application/x-www-form-urlencoded");
                using var response = httpClient.PostAsync(url, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    /* If there is content in the response, print it. */
                    /* Format the object string for easier reading. */
                    dynamic msg = JsonConvert.DeserializeObject(result);
                    string prettyJson = JsonConvert.SerializeObject(msg, Formatting.Indented);
                    Console.WriteLine("RECEIVED:\n{0}\n", prettyJson);

                    /* Get the token from the cookies. */
                    var responseCookies = httpHandler.CookieContainer.GetCookies(new Uri(url));
                    Cookie cookie = responseCookies["AuthToken"];
                    if (cookie == null)
                    {
                        Console.WriteLine("Authentication failed. Authentication token not found in cookies.");
                        Environment.Exit(1);
                    }
                    _authToken = cookie.Value;

                    /* We have our token. */
                    Console.WriteLine("Authentication Succeeded. Received AuthToken: {0}\n", _authToken);
                    return true;
                }
                else
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Moved:                  // 301
                        case HttpStatusCode.Redirect:               // 302
                        case HttpStatusCode.TemporaryRedirect:      // 307
                        case HttpStatusCode.PermanentRedirect:      // 308
                            // Perform URL redirect
                            Console.WriteLine("LSEG Delivery Platform authentication HTTP code: {0} {1}\n", response.StatusCode, response.ReasonPhrase);
                            string newHost = response.Headers.Location.AbsoluteUri;
                            if (!string.IsNullOrEmpty(newHost))
                                return GetAuthenticationInfo(isRefresh, newHost);
                            break;
                        case HttpStatusCode.BadRequest:        // 400
                        case HttpStatusCode.Unauthorized:      // 401
                            // Retry with username and password
                            Console.WriteLine("LSEG Delivery Platform authentication HTTP code: {0} {1}\n", response.StatusCode, response.ReasonPhrase);
                            if (isRefresh)
                            {
                                Console.WriteLine("Retry with username and password");
                                return GetAuthenticationInfo(false);
                            }
                            break;
                        case HttpStatusCode.Forbidden:                      // 403
                        case HttpStatusCode.NotFound:                       // 404
                        case HttpStatusCode.Gone:                           // 410
                        case HttpStatusCode.UnavailableForLegalReasons:     // 451
                            // Stop retrying with the request
                            Console.WriteLine("LSEG Delivery Platform authentication HTTP code: {0} {1}\n", response.StatusCode, response.ReasonPhrase);
                            Console.WriteLine("Stop retrying with the request");
                            break;
                        default:
                            // Retry the request to the LSEG Delivery Platform 
                            Console.WriteLine("LSEG Delivery Platform authentication HTTP code: {0} {1}\n", response.StatusCode, response.ReasonPhrase);
                            Console.WriteLine("Retrying the request to the LSEG Delivery Platform");
                            Thread.Sleep((int)(5000));
                            // CAUTION: This is sample code with infinite retries
                            return GetAuthenticationInfo(isRefresh);
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                /* The request to the authentication server failed, e.g. due to connection failure or HTTP error response. */
                Console.WriteLine("Authentication server request failed: {0}", e.Message);
            }
            return false;
        }

        /// <summary>Runs the application. Retrives a token from the authentication server, then opens
        /// the WebSocket using the token.</summary>
        public void Run()
        {
            /* Get local hostname. */
            IPAddress hostEntry = Array.Find(Dns.GetHostEntry(Dns.GetHostName()).AddressList, ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            _position = (hostEntry == null) ? "127.0.0.1/net" : hostEntry.ToString();

            /* Open a websocket. */
            Uri uri = new Uri("ws://" + _hostName + ":" + _port + "/WebSocket");
            Console.WriteLine("Connecting to WebSocket " + uri.AbsoluteUri + " ...");

            if (!GetAuthenticationInfo(false))
                Environment.Exit(1);

            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetBuffer(BUFFER_SIZE, BUFFER_SIZE);
            _webSocket.Options.AddSubProtocol("tr_json2");

            Console.CancelKeyPress += Console_CancelKeyPress;

            try
            {
                _webSocket.ConnectAsync(uri, CancellationToken.None).Wait();

                if (_webSocket.State == WebSocketState.Open)
                {
                    SendLogin(false);

                    /* Read messages */
                    while (_webSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            ReceiveMessage();
                        }
                        catch (System.AggregateException)
                        {
                            System.Console.WriteLine("The WebSocket connection is closed");
                            Console_CancelKeyPress(null, null);
                        }
                    }
                }
                else
                {
                    System.Console.WriteLine("Failed to open a WebSocket connection");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
            finally
            {
                Console_CancelKeyPress(this, null);
            }
        }

        /// <summary>
        /// Handles Ctrl + C or exits the application.
        /// </summary>
        /// <param name="sender">The caller of this method</param>
        /// <param name="e">The <c>ConsoleCancelEventArgs</c> if any</param>
        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (_webSocket != null)
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine("The WebSocket connection is closed");
                    _cts.Cancel();
                    _webSocket.Dispose();
                }
            }
            Environment.Exit(0);
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
                    result = _webSocket.ReceiveAsync(readBuffer, _cts.Token);

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
                    Console.WriteLine("RECEIVED:\n{0}\n", JsonConvert.SerializeObject(messages, Formatting.Indented));

                    for (int index = 0; index < messages.Count; ++index)
                        ProcessJsonMsg(messages[index]);
                }
            }
        }

        /// <summary>
        /// Creates and sends a login message
        /// </summary>
        /// <param name="isRefresh">Setting <c>true</c> to not interest in the login refresh</param>
        private void SendLogin(bool isRefresh)
        {
            string msg;
            msg = "{" + "\"ID\":1," + "\"Domain\":\"Login\"," + "\"Key\": {\"NameType\":\"AuthnToken\"," +
                "\"Elements\":{\"ApplicationId\":\"" + _appId + "\"," + "\"Position\":\"" + _position + "\"," +
                "\"AuthenticationToken\":\"" + _authToken + "\"}}";
            if (isRefresh)
                msg += ",\"Refresh\": false";
            msg += "}";
            SendMessage(msg);
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
                            Environment.Exit(1);
                        }

                        if (!_loggedIn && (msg["State"] == null || (string)msg["State"]["Data"] == "Ok"))
                        {
                            /* Login was successful. */
                            _loggedIn = true;

                            /* Request an item. */
                            SendMessage("{" + "\"ID\": 2," + "\"Key\": {\"Name\":\"" + _ric + "\",\"Service\":\"" + _service + "\"}" + "}");
                        }
                    }
                    break;
                case "Status":
                    if (msg["Domain"] != null && (string)msg["Domain"] == "Login" &&
                        msg["State"] != null && msg["State"]["Stream"] != null && (string)msg["State"]["Stream"] != "Open")
                    {
                        Console.WriteLine("Stream is no longer open (state is {0})", (string)msg["State"]["Stream"]);
                        Environment.Exit(1);
                    }
                    break;
                case "Ping":
                    SendMessage("{\"Type\":\"Pong\"}");
                    break;
                default:
                    break;
            }
        }

        /// <summary>Prints the outbound message and sends it on the WebSocket.</summary>
        /// <param name="jsonMsg">Message to send</param>
        void SendMessage(string jsonMsg)
        {
            /* Print the message (format the object string for easier reading). */
            Console.WriteLine("SENT:\n{0}\n", JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonMsg), Formatting.Indented));

            var encoded = Encoding.ASCII.GetBytes(jsonMsg);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _cts.Token).Wait();
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
                    case "-a":
                    case "--app_id":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _appId = args[++i];
                        break;
                    case "--auth_hostname":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _authHostName = args[++i];
                        break;
                    case "--auth_port":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _authPort = args[++i];
                        break;
                    case "-h":
                    case "--hostname":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _hostName = args[++i];
                        break;
                    case "--password":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _password = args[++i];
                        break;
                    case "-p":
                    case "--port":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _port = args[++i];
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
                    case "-u":
                    case "--user":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _userName = args[++i];
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

            if (_hostName == null)
            {
                Console.WriteLine("hostname must be specified on the command line");
                PrintCommandLineUsageAndExit(1);
            }
        }

        /// <summary>Prints usage information. Used when arguments cannot be parsed.</summary>
        void PrintCommandLineUsageAndExit(int exitStatus)
        {
            Console.WriteLine("Usage:\n" +
                "dotnet {0}.dll\n" +
                "   [-a|--app_id appId]              \n" +
                "   [--auth_hostname auth_hostname]\n" +
                "   [--auth_port auth_port]       \n" +
                "   [-h|--hostname hostname]         \n" +
                "   [--password password]         \n" +
                "   [--newPassword new_password]  \n" +
                "   [-p|--port port]                 \n" +
                "   [--ric ric]                   \n" +
                "   [--scope scope]               \n" +
                "   [-u|--user user]                 \n" +
                "   [--clientid clientID]         \n" +
                "   [--service service]           \n" +
                "   [--help]                      \n",
                System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(exitStatus);
        }
    }
}
