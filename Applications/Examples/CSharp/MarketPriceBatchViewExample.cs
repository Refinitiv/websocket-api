//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|            Copyright (C) 2019 Refinitiv. All rights reserved.             --
//|-----------------------------------------------------------------------------


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using WebSocketSharp;

/*
 * This example demonstrates retrieving JSON-formatted market content from a WebSocket server.
 * It performs the following steps:
 * - Logs into the WebSocket server.
 * - Requests TRI.N, IBM.N, and T.N, market-price content via batch, and includes a view indicating we wish only to receive
 *   the BID, ASK, and BIDSIZE fields.
 * - Prints the response content.
 */

namespace MarketPriceBatchViewExample
{
    class MarketPriceBatchViewExample
    {
        /// <summary>The websocket used for retrieving market content.</summary>
        private WebSocket _webSocket;

        /// <summary>Indicates whether we have successfully logged in.</summary>
        private bool _loggedIn = false;

        /// <summary>The configured hostname of the Websocket server.</summary>
        private string _hostName = "localhost";

        /// <summary>The configured port used when opening the WebSocket.</summary>
        private string _port = "15000";

        /// <summary>The configured username used when logging in.</summary>
        private string _userName = Environment.UserName;

        /// <summary>The configured ApplicationID used when logging in.</summary>
        private string _appId = "256";

        /// <summary>The IP address, used as the application's position when logging in.</summary>
        private string _position;

        /// <summary>Parses commandline config and runs the application.</summary>
        static void Main(string[] args)
        {
            MarketPriceBatchViewExample example = new MarketPriceBatchViewExample();
            example.parseCommandLine(args);
            example.run();
        }

        /// <summary>Runs the application. Opens the WebSocket.</summary>
        public void run()
        {
            /* Get local hostname. */
            IPAddress hostEntry = Array.Find(Dns.GetHostEntry(Dns.GetHostName()).AddressList, ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (hostEntry != null)
                _position = hostEntry.ToString();
            else
                _position = "127.0.0.1";

            /* Open a websocket. */
            string hostString = "ws://" + _hostName + ":" + _port + "/WebSocket";
            Console.Write("Connecting to WebSocket " + hostString + " ...");
            _webSocket = new WebSocket(hostString, "tr_json2");

            _webSocket.OnOpen += onWebSocketOpened;
            _webSocket.OnError += onWebSocketError;
            _webSocket.OnClose += onWebSocketClosed;
            _webSocket.OnMessage += onWebSocketMessage;

            /* Print any log events (similar to default behavior, but we explicitly indicate it's a logger event). */
            _webSocket.Log.Output = (logData, text) => Console.WriteLine("Received Log Event (Level: {0}): {1}\n", logData.Level, logData.Message);

            _webSocket.Connect();

            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>Handles the initial open of the WebSocket. Sends login request.</summary>
        private void onWebSocketOpened(object sender, EventArgs e)
        {
            Console.WriteLine("WebSocket successfully connected!\n");

            sendMessage(
                    "{"
                    + "\"ID\": 1,"
                    + "\"Domain\":\"Login\", "
                    + "\"Key\":{"
                    + "\"Name\":\"" + _userName + "\", "
                    + "\"Elements\":{"
                    + "\"ApplicationId\":\"" + _appId + "\", "
                    + "\"Position\": \"" + _position + "\""
                    + "}"
                    + "}"
                    + "}"
                    );
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
        /// opens streams for TRI.N, IBM.N, and T.N content via a batch request.
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

                                /* Request items as a batch.
                                 * Include a view indicating the desired fields for these items. */
                                sendMessage(
                                        "{"
                                        + "\"ID\": 2,"
                                        + "\"Key\": {\"Name\":[\"TRI.N\",\"IBM.N\",\"T.N\"]},"
                                        + "\"View\":[\"BID\",\"ASK\",\"BIDSIZE\"]"
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

                    default:
                        Console.WriteLine("Unknown option: {0}", args[i]);
                        printCommandLineUsageAndExit();
                        break;

                }
            }
        }

        /// <summary>Prints usage information. Used when arguments cannot be parsed.</summary>
        void printCommandLineUsageAndExit()
        {
            Console.WriteLine("Usage: {0} [ -h hostname ] [-p port] [-a appID] [-u user]", System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(1);
        }
    }
}
