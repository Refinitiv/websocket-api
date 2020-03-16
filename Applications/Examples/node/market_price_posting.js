//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright (C) Refinitiv 2019. All rights reserved.              --
//|-----------------------------------------------------------------------------


// Simple example of outputting Market Price JSON data using Websockets

// Global Default Variables
var hostName = '127.0.0.1'
var port = '15000'
var appId = '256'
var user = 'root'
var ip = require("ip");
var position = ip.address();

// Global Variables
var webSocketClosed = false;
var isItemStreamOpen = false;
var postId = 1;

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
	console.log("Usage: market_price.js [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--help]");
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

// Create and send simple Market Price request
function sendMarketPriceRequest()
{
	var msg = '{"ID":2,"Key":{"Name":"TRI.N"}}';
	_websocket.send(msg);
	console.log("SENT:");
	console.log(JSON.stringify(JSON.parse(msg), null, 2));
}

// Create and send simple Market Price post
function sendMarketPricePost()
{
	var msg = '{"ID":2,"Type":"Post","Domain":"MarketPrice","Ack":true,"PostID":' + postId + ', "PostUserInfo":{'

		// Use the IP address as the Post User Address
		+ '"Address":"' + position +

		// Use the current process ID as the Post User Id
		'","UserID":' + process.pid +

		'}, "Message": {' +
			'"ID":0, "Type":"Update", "Fields": {"BID":45.55,"BIDSIZE":18,"ASK":45.57,"ASKSIZE":19}}' +
		'}'
	'}'

	_websocket.send(msg);
	console.log("SENT:");
	console.log(JSON.stringify(JSON.parse(msg), null, 2));

	postId += 1
}

// Parse at high level and output JSON of message
function processMessage(msg)
{
	var msgType = msg.Type;

	switch (msgType)
	{
		case "Refresh":
		{
			var msgDomain = msg.Domain;
			if (msgDomain == "Login")
			{
				sendMarketPriceRequest();
			}

			if (msg.ID == 2)
			{
				if (isItemStreamOpen == false && (msg.State == null || msg.State.Stream == "Open" && msg.State.Data == "Ok"))
				{
					// Item stream is open. We can start posting.
					isItemStreamOpen = true;
					setInterval(function(){sendMarketPricePost()}, 3000);
				}
			}
		}
		break;

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
	console.log("Error");
}
