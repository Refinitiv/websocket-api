//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.
//|                See the project's LICENSE.md for details.
//|           Copyright (C) 2017-2020,2024 LSEG. All rights reserved.
//|-----------------------------------------------------------------------------

// Simple example of outputting Market Price JSON data with Batch and View using Websockets

// Global Default Variables
var hostName = '127.0.0.1'
var port = '15000'
var appId = '256'
var user = 'root'
var ip = require("ip");
var position = ip.address();

// Global Variables
var webSocketClosed = false;

// Get command line parameters
var argv = require('optimist').argv;

if(argv.hostname)
{
	hostName = argv.hostname.toString();
}
if(argv.port)
{
	port = argv.port.toString();
}
if(argv.appId)
{
	appId = argv.appId.toString();
}
if(argv.user)
{
	user = argv.user.toString();
}
if(argv.position)
{
	position = argv.position.toString();
}
if(argv.help)
{
	console.log("Usage: market_price_batch_view.js [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--help]");
	process.exit();
}

// Start websocket handshake
var WebSocket = require('ws')
var WS_URL = 'ws://' + hostName + ':' + port + '/WebSocket';
console.log("Connecting to WebSocket " + WS_URL + " ...");
_websocket = new WebSocket(WS_URL, "tr_json2");
_websocket.onopen = onOpen;
_websocket.onclose = onClose;
_websocket.onmessage = onMessage;
_websocket.onerror = onError;

// Create and send simple Market Price batch request with view
function sendMarketPriceRequest()
{
	var msg = '{"ID":2,"Key":{"Name":["TRI.N","IBM.N","T.N"]},"View":["BID","ASK","BIDSIZE"]}';
	_websocket.send(msg);
	console.log("SENT:");
	console.log(JSON.stringify(JSON.parse(msg), null, 2));
}

// Parse at high level and output JSON of message
function processMessage(msg)
{
	var msgType = msg.Type;
	
	switch(msgType)
	{
		case "Refresh":
		{
			if (msg.Domain)
			{
				var msgDomain = msg.Domain;
				if (msgDomain == "Login")
				{
					sendMarketPriceRequest();
				}
			}
			break;
		}

		case "Ping":
		{
			var msg = '{"Type":"Pong"}';
			_websocket.send(msg);
			console.log("SENT:");
			console.log(JSON.stringify(JSON.parse(msg), null, 2));
			break;
		}

		default:
			break;
	}
}

// Called when handshake is complete and websocket is open, send login
function onOpen(evt)
{
	console.log("WebSocket successfully connected!");
	
	// Generate a login request from command line data (or defaults) and send
	var msg = '{"ID":1,"Domain":"Login","Key":{"Name":"","Elements":{"ApplicationId":"","Position":""}}}'
	var msgJson = JSON.parse(msg);
	msgJson.Key.Name = user
	msgJson.Key.Elements.ApplicationId = appId
	msgJson.Key.Elements.Position = position
	_websocket.send(JSON.stringify(msgJson));
	console.log("SENT:");
	console.log(JSON.stringify(msgJson, null, 2));
}

// Called when websocket is closed
function onClose(evt){
	console.log("WebSocket Closed");
}

// Called when message received, parse message into JSON for processing
function onMessage(evt)
{
	console.log("RECEIVED:");

	var parsedMsg = JSON.parse(evt.data.toString());
	console.log(JSON.stringify(parsedMsg, null, 2));

	for (var i = 0; i < parsedMsg.length; ++i)
		processMessage(parsedMsg[i]);
}

// Called when websocket has error
function onError(evt)
{
	console.log("Error:" + evt.data.toString());
}
