//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright Thomson Reuters 2018. All rights reserved.            --
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

namespace MarketPriceEdpGwAuthenticationExample
{
    class MarketPriceEdpGwAuthenticationExample
    {
        /// <summary>The websocket used for retrieving market content.</summary>
        private WebSocket _webSocket;

        /// <summary>Indicates whether we have successfully logged in.</summary>
        private bool _loggedIn = false;

        /// <summary>The tokens retrieved from the authentication server.
        private string _authToken;
        private string _refreshToken;

        /// <summary>The configured hostname of the Websocket server.</summary>
        private string _hostName;

        /// <summary>The configured port used when opening the WebSocket.</summary>
        private string _port = "443";

        /// <summary>The configured hostname of the authentication server. If not specified, the same hostname as
        /// as the WebSocket server is used.</summary>
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

        /// <summary>Parses commandline config and runs the application.</summary>
        static void Main(string[] args)
        {
            MarketPriceEdpGwAuthenticationExample example = new MarketPriceEdpGwAuthenticationExample();
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

            webRequest.UserAgent = "CSharpMarketPriceEdpGwAuthenticationExample";
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
                    Console.WriteLine("RECEIVED:\n{0}\n", JsonConvert.SerializeObject(msg, Formatting.Indented));

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
                    Console.WriteLine("Authentication server request failed: {0} -- {1}\n", e.Message, e.InnerException.Message);
                else
                    Console.WriteLine("Authentication server request failed: {0}", e.Message);
            }
            return false;
        }

        /// <summary>Runs the application. Retrives a token from the authentication server, then opens the WebSocket using the token.</summary>
        public void run()
        {
            /* Get local hostname. */
            IPAddress hostEntry = Array.Find(Dns.GetHostEntry(Dns.GetHostName()).AddressList, ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            _position = (hostEntry == null) ? "127.0.0.1" : hostEntry.ToString();

            if (!getAuthenticationInfo(false))
                Environment.Exit(1);

            /* Open a websocket. */
            string hostString = "wss://" + _hostName + ":" + _port + "/WebSocket";
            Console.WriteLine("Connecting to WebSocket " + hostString + " ...");
            _webSocket = new WebSocket(hostString, "tr_json2");

            _webSocket.OnOpen += onWebSocketOpened;
            _webSocket.OnError += onWebSocketError;
            _webSocket.OnClose += onWebSocketClosed;
            _webSocket.OnMessage += onWebSocketMessage;

            /* Print any log events (similar to default behavior, but we explicitly indicate it's a logger event). */
            _webSocket.Log.Output = (logData, text) => Console.WriteLine("Received Log Event (Level: {0}): {1}\n", logData.Level, logData.Message);

            _webSocket.Connect();

            while (true)
            {
                Thread.Sleep((int)(_expirationInMilliSeconds * .90));
                if (_loggedIn)
                {
                    // refresh authentication token; if refresh attempt fails, try full auth
                    if (!getAuthenticationInfo(true))
                        if (!getAuthenticationInfo(false))
                            Environment.Exit(1);
                    sendLogin(true);
                }
            }
        }

        private void sendLogin(bool isRefresh)
        {
            string msg;
            msg = "{" + "\"ID\":1," + "\"Domain\":\"Login\"," + "\"Key\": {\"NameType\":\"AuthnToken\"," +
                "\"Elements\":{\"ApplicationId\":\"" + _appId + "\"," + "\"Position\":\"" + _position + "\"," +
                "\"AuthenticationToken\":\"" + _authToken + "\"}}";
            if (isRefresh)
                msg += ",\"Refresh\": false";
            msg += "}";
            sendMessage(msg);
        }

        /// <summary>Handles the initial open of the WebSocket.</summary>
        private void onWebSocketOpened(object sender, EventArgs e)
        {
            Console.WriteLine("WebSocket successfully connected");

            // create JSON string with token, app id, and position and send it over the web socket
            sendLogin(false);
        }

        /// <summary>Handles messages received on the websocket.</summary>
        private void onWebSocketMessage(object sender, MessageEventArgs e)
        {
            /* Received message(s). */
            JArray messages = JArray.Parse(e.Data);

            /* Print the message (format the object string for easier reading). */
            Console.WriteLine("RECEIVED:\n{0}\n", JsonConvert.SerializeObject(messages, Formatting.Indented));

            for(int index = 0; index < messages.Count; ++index)
                processJsonMsg(messages[index]);
        }

        /// <summary>
        /// Processes the received message. If the message is a login response indicating we are now logged in,
        /// opens a stream for price content.
        /// </summary>
        /// <param name="msg">The received JSON message</param>
        void processJsonMsg(dynamic msg)
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

                        if (!_loggedIn && (msg["State"] == null || (string)msg["State"]["Data"] == "Ok"))
                        {
                            /* Login was successful. */
                            _loggedIn = true;

                            /* Request an item. */
                            sendMessage("{" + "\"ID\": 2," + "\"Key\": {\"Name\":\"" + _ric + "\"}" + "}");
                        }
                    }
                    break;
                case "Status":
                    if (msg["Domain"] != null && (string)msg["Domain"] == "Login" &&
                        msg["State"] != null && msg["State"]["Stream"] != null && (string)msg["State"]["Stream"] != "Open")
                    {
                        Console.WriteLine("stream is no longer open (state is {0})", (string)msg["State"]["Stream"]);
                        Environment.Exit(1);
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
            Console.WriteLine("SENT:\n{0}\n", JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonMsg), Formatting.Indented));

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
                // all commands require an argument
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("{0} requires an argument.", args[i]);
                    printCommandLineUsageAndExit();
                }
                switch (args[i])
                {
                    case "--app_id":
                        _appId = args[++i];
                        break;
                    case "--auth_hostname":
                        _authHostName = args[++i];
                        break;
                    case "--auth_port":
                        _authPort = args[++i];
                        break;
                    case "--hostname":
                        _hostName = args[++i];
                        break;
                    case "--password":
                        _password = args[++i];
                        break;
                    case "--port":
                        _port = args[++i];
                        break;
                    case "--ric":
                        _ric = args[++i];
                        break;
                    case "--scope":
                        _scope = args[++i];
                        break;
                    case "--user":
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

            if (_hostName == null)
            {
                Console.WriteLine("hostname must be specified on the command line");
                printCommandLineUsageAndExit();
            }

            /* If authentication server host wasn't specified, use same host as websocket. */
            if (_authHostName == null)
                _authHostName = _hostName;
        }

        /// <summary>Prints usage information. Used when arguments cannot be parsed.</summary>
        void printCommandLineUsageAndExit()
        {
            Console.WriteLine("Usage: {0} [--app_id appId] [--auth_hostname hostname] [--auth_port port] [--hostname hostname] [--password password] [--port port] [--ric ric] [--scope scope] [--user user]", System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(1);
        }
    }
}
