//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.
//|                See the project's LICENSE.md for details.
//|            Copyright (C) 2018-2021,2024 LSEG. All rights reserved.
//|-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
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
 * This example demonstrates retrieving JSON-formatted market content from a WebSocket server.
 * It performs the following steps:
 * - Logs into the WebSocket server.
 * - Requests TRI.N market-price content.
 * - Prints the response content.
 * - Periodically sends post messages for TRI.N.
 */


namespace MarketPricePostingExample
{

    class MarketPricePostingExample
    {
        /// <summary>The websocket used for retrieving market content.</summary>
        private ClientWebSocket _webSocket;

        /// <summary>Indicates whether we have successfully logged in.</summary>
        private bool _loggedIn = false;

        /// <summary>Whether TRI is open and can be posted to.</summary>
        private bool _sendPosts;

        /// <summary>Next Post ID to use when sending a post message</summary>
        private int _postId = 1;

        /// <summary>The configured hostname of the Websocket server.</summary>
        private string _hostName = "localhost";

        /// <summary>The configured port used when opening the WebSocket.</summary>
        private string _port = "15000";

        /// <summary>The configured username used when requesting the token.</summary>
        private string _userName;

        /// <summary>The configured ApplicationID used when requesting the token.</summary>
        private string _appId = "555";

        /// <summary>The IP address, used as the application's position when requesting the token.</summary>
        private string _position;

        /// <summary> Specifies buffer size for each read from WebSocket.</summary>
        private static readonly int BUFFER_SIZE = 8192;

        /// <summary> This is used to cancel operations when something goes wrong. </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        static void Main(string[] args)
        {
            MarketPricePostingExample example = new MarketPricePostingExample();
            example.ParseCommandLine(args);
            example.Run();
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

            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetBuffer(BUFFER_SIZE, BUFFER_SIZE);
            _webSocket.Options.AddSubProtocol("tr_json2");

            Console.CancelKeyPress += Console_CancelKeyPress;

            try
            {
                _webSocket.ConnectAsync(uri, CancellationToken.None).Wait();

                if (_webSocket.State == WebSocketState.Open)
                {
                    SendLogin();

                    /* Run a take to read messages */
		   
                        /* The IP Address and UserID are used as our PostUserInfo when sending post messages.
                         * - We use the current process ID as our user ID. */
                    int userId = Process.GetCurrentProcess().Id;

                    while (_webSocket.State == WebSocketState.Open)
                    {
                        Thread.Sleep(3000);
                        /* If TRI.N is open, periodically post data to it. */
                        if (_sendPosts)
                        {
                            SendMessage(
                                    "{\"ID\":2,"
                                    + "\"Type\":\"Post\","
                                    + "\"Ack\":true,\"PostID\":" + _postId + ","
                                    + "\"Domain\":\"MarketPrice\","
                                    + "\"PostUserInfo\":{\"Address\":\"" + _position + "\",\"UserID\":" + userId + "},"
                                    + "\"Message\":{"
                                        + "\"ID\":0,\"Type\":\"Update\",\"Domain\":\"MarketPrice\","
                                        + "\"Fields\":{\"BID\": 45.55,\"BIDSIZE\": 18,\"ASK\": 45.57,\"ASKSIZE\": 19}"
                                    + "}"
                                    + "}");

                            ++_postId;
                        }

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
        private void SendLogin()
        {
            SendMessage(
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
                    switch ((string)msg["Domain"])
                    {
                        case "Login":
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
                                SendMessage(
                                    "{"
                                    + "\"ID\": 2,"
                                    + "\"Key\": {\"Name\":\"TRI.N\"}"
                                    + "}"
                                    );
                            }
                            break;
                        case null:
                        case "MarketPrice":
                            if (msg["ID"] == 2 && msg["Type"] == "Refresh")
                            {
                                /* This message is for the TRI.N stream we requested. If thre stream is open, start periodically posting. */
                                if (msg["State"] == null || msg["State"]["Stream"] == "Open" && msg["State"]["Data"] == "Ok")
                                    _sendPosts = true;
                                else
                                    _sendPosts = false;
                            }
                            break;
                        default:
                            break;
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
                    case "-h":
                    case "--hostname":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _hostName = args[++i];
                        break;
                    case "-p":
                    case "--port":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _port = args[++i];
                        break;
                    case "-u":
                    case "--user":
                        if (i + 1 >= args.Length)
                            GripeAboutMissingOptionArgumentAndExit(args[i]);
                        _userName = args[++i];
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
                "   [-h|--hostname hostname]         \n" +
                "   [-p|--port port]                 \n" +
                "   [-u|--user user]                 \n" +
                "   [--help]                      \n",
                System.AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(exitStatus);
        }
    }
}
