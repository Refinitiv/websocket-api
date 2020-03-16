//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright (C) 2019 Refinitiv. All rights reserved.              --
//|-----------------------------------------------------------------------------


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using WebSocketSharp;

/*
 * This example demonstrates retrieving JSON-formatted market content from a WebSocket server,
 * using a token retrieved from an authentication service.
 * It performs the following steps:
 * - Sends an HTTP request to the authentication server and retrives a token.
 * - Connects to the WebSocket server using the authentication token.
 * - Requests TRI.N market-price content.
 * - Prints the response content.
 */

namespace MarketPriceAuthenticationExample
{
    class MarketPriceAuthenticationExample
    {
        /// <summary>The websocket used for retrieving market content.</summary>
        private WebSocket _webSocket;

        /// <summary>Indicates whether we have successfully logged in.</summary>
        private bool _loggedIn = false;

        /// <summary>The token retrieved from the authentication server.
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
        private string _userName = Environment.UserName;

        /// <summary>The configured password used when requesting the token.</summary>
        private string _password = "";

        /// <summary>The configured ApplicationID used when requesting the token.</summary>
        private string _appId = "555";

        /// <summary>The IP address, used as the application's position when requesting the token.</summary>
        private string _position;

        /// <summary>Parses commandline config and runs the application.</summary>
        static void Main(string[] args)
        {
            MarketPriceAuthenticationExample example = new MarketPriceAuthenticationExample();
            example.parseCommandLine(args);
            example.run();
        }

        /// <summary>Runs the application. Retrives a token from the authentication server, then opens the WebSocket using the token.</summary>
        public void run()
        {
            /* Get local hostname. */
            IPAddress hostEntry = Array.Find(Dns.GetHostEntry(Dns.GetHostName()).AddressList, ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (hostEntry != null)
                _position = hostEntry.ToString();
            else
                _position = "127.0.0.1";

            /* Send an HTTP request to the specified authentication server, containing our username and password.
             * The token will be used to login on the websocket.  */
            Console.WriteLine("Sending authentication request...\n");

            string url = "https://" + _authHostName + ":" + _authPort + "/getToken";
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

            webRequest.UserAgent = "CSharpMarketPriceAuthenticationExample";

            /* TODO Remove this. It disables certificate validation. */
            webRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };

            /* Add a cookie container to the request, so that we can get the token from the response cookies. */
            webRequest.CookieContainer = new CookieContainer();

            try
            {
                /* Send username and password in request. */
                string postString = "username=" + _userName + "&password=" + _password;
                byte[] postContent = Encoding.ASCII.GetBytes(postString);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = postContent.Length;
                System.IO.Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(postContent, 0, postContent.Length);
                requestStream.Close();

                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.ContentLength > 0)
                {
                    /* If there is content in the response, print it. */
                    /* Format the object string for easier reading. */
                    dynamic msg = JsonConvert.DeserializeObject(new System.IO.StreamReader(webResponse.GetResponseStream()).ReadToEnd());
                    string prettyJson = JsonConvert.SerializeObject(msg, Formatting.Indented);
                    Console.WriteLine("RECEIVED:\n{0}\n", prettyJson);
                }

                /* Get the token from the cookies. */
                Cookie cookie = webResponse.Cookies["AuthToken"];
                if (cookie == null)
                {
                    Console.WriteLine("Authentication failed. Authentication token not found in cookies.");
                    Environment.Exit(1);
                }
                _authToken = cookie.Value;

                /* We have our token. */
                Console.WriteLine("Authentication Succeeded. Received AuthToken: {0}\n", _authToken);
                webResponse.Close();
            }
            catch (WebException e)
            {
                /* The request to the authentication server failed, e.g. due to connection failure or HTTP error response. */
                if (e.InnerException != null)
                    Console.WriteLine("Authentication server request failed: {0} -- {1}\n", e.Message.ToString(), e.InnerException.Message.ToString());
                else
                    Console.WriteLine("Authentication server request failed: {0}", e.Message.ToString());
                Environment.Exit(1);
            }

            /* Open a websocket. */
            string hostString = "ws://" + _hostName + ":" + _port + "/WebSocket";
            Console.Write("Connecting to WebSocket " + hostString + " ...");
            _webSocket = new WebSocket(hostString, "tr_json2");

            /* Set the token we received from the authentication request. */
            _webSocket.SetCookie(new WebSocketSharp.Net.Cookie("AuthToken", _authToken));
            _webSocket.SetCookie(new WebSocketSharp.Net.Cookie("AuthPosition", _position));
            _webSocket.SetCookie(new WebSocketSharp.Net.Cookie("applicationId", _appId));

            _webSocket.OnOpen += onWebSocketOpened;
            _webSocket.OnError += onWebSocketError;
            _webSocket.OnClose += onWebSocketClosed;
            _webSocket.OnMessage += onWebSocketMessage;

            /* Print any log events (similar to default behavior, but we explicitly indicate it's a logger event). */
            _webSocket.Log.Output = (logData, text) => Console.WriteLine("Received Log Event (Level: {0}): {1}\n", logData.Level, logData.Message);

            _webSocket.Connect();

            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>Handles the initial open of the WebSocket.</summary>
        private void onWebSocketOpened(object sender, EventArgs e)
        {
            Console.WriteLine("WebSocket successfully connected!\n");

            /* Don't login -- the authentication token should do that for us. Just wait for the login refresh. */
        }

        /// <summary>Handles messages received on the websocket.</summary>
        private void onWebSocketMessage(object sender, MessageEventArgs e)
        {
            /* Received message(s). */
            int index = 0;
            JArray messages = JArray.Parse(e.Data);

            /* Print the message (format the object string for easier reading). */
            string prettyJson = JsonConvert.SerializeObject(messages, Formatting.Indented);
            Console.WriteLine("RECEIVED:\n{0}\n", prettyJson);

            for(index = 0; index < messages.Count; ++index)
                processJsonMsg(messages[index]);
        }

        /// <summary>
        /// Processes the received message. If the message is a login response indicating we are now logged in,
        /// opens a stream for TRI.N content.
        /// </summary>
        /// <param name="msg">The received JSON message</param>
        void processJsonMsg(dynamic msg)
        {
            switch((string)msg["Type"])
            {
                case "Refresh":
                    switch ((string)msg["Domain"])
                    {
                        case "Login":
                            if (msg["State"] != null && msg["State"]["Stream"] != "Open")
                            {
                                Console.WriteLine("Login stream was closed.\n");
                                Environment.Exit(1);
                            }

                            if (!_loggedIn && (msg["State"] == null || msg["State"]["Data"] == "Ok"))
                            {
                                /* Login was successful. */
                                _loggedIn = true;

                                /* Request an item. */
                                sendMessage(
                                    "{"
                                    + "\"ID\": 2,"
                                    + "\"Key\": {\"Name\":\"TRI.N\"}"
                                    + "}"
                                    );
                            }
                            break;

                        default:
                            break;
                    }
                    break;
                case "Ping":
                    sendMessage("{\"Type\":\"Pong\"}");
                    break;
                default:
                    break;
            }

        }

        /// <summary>Prints the outbound message and sends it on the WebSocket.</summary>
        /// <param name="jsonMsg">Message to send</param>
        void sendMessage(string jsonMsg)
        {
            /* Print the message (format the object string for easier reading). */
            dynamic msg = JsonConvert.DeserializeObject(jsonMsg);
            string prettyJson = JsonConvert.SerializeObject(msg, Formatting.Indented);
            Console.WriteLine("SENT:\n{0}\n", prettyJson);

            _webSocket.Send(jsonMsg);
        }


        /// <summary>Handles the WebSocket closing.</summary>
        private void onWebSocketClosed(object sender, CloseEventArgs e)
        {
            Console.WriteLine("WebSocket was closed: {0}\n", e.Reason);
            Environment.Exit(1);
        }

        /// <summary>Handles any error that occurs on the WebSocket.</summary>
        private void onWebSocketError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Received Error: {0}\n", e.Exception.ToString());
            Environment.Exit(1);
        }

        /// <summary>Parses command-line arguments.</summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        void parseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-a":
                    case "--app_id":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _appId = args[i + 1];
                        ++i;
                        break;

                    case "--auth_hostname":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _authHostName = args[i + 1];
                        ++i;
                        break;

                    case "--auth_port":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _authPort = args[i + 1];
                        ++i;
                        break;

                    case "-h":
                    case "--hostname":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _hostName = args[i + 1];
                        ++i;
                        break;

                    case "-p":
                    case "--port":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _port = args[i + 1];
                        ++i;
                        break;

                    case "-u":
                    case "--user":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _userName = args[i + 1];
                        ++i;
                        break;

                    case "--password":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("{0} requires an argument.", args[i]);
                            printCommandLineUsageAndExit();
                        }
                        _password = args[i + 1];
                        ++i;
                        break;

                    default:
                        Console.WriteLine("Unknown option: {0}", args[i]);
                        printCommandLineUsageAndExit();
                        break;

                }
            }

            /* If authentication server host wasn't specified, use same host as websocket. */
            if (_authHostName == null)
                _authHostName = _hostName;
        }

        /// <summary>Prints usage information. Used when arguments cannot be parsed.</summary>
        void printCommandLineUsageAndExit()
        {
            Console.WriteLine("Usage: {0} [ -h hostname ] [-p port] [-a appID] [-u user] [--password password] [--auth_hostname hostname] [--auth_port port]", System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(1);
        }
    }
}
