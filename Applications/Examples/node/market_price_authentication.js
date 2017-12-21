//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright Thomson Reuters 2017. All rights reserved.            --
//|-----------------------------------------------------------------------------


// Simple example of outputting Market Price JSON data using Websockets with authentication

// Global Default Variables
var hostName = '127.0.0.1'
var port = '15000'
var appId = '555'
var user = 'root'
var authHostName = '127.0.0.1'
var authPort = '8443'
var password = ''
var authToken = ''
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
if(argv.password)
{
	password = argv.password.toString();
}
if(argv.auth_hostname)
{
	authHostName = argv.auth_hostname.toString();
}
if(argv.auth_port)
{
	authPort = argv.auth_port.toString();
}
if(argv.help)
{
	console.log("Usage: market_price_authentication.py [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--password password] [--position position] [--auth_hostname auth_hostname] [--auth_port auth_port] [--help]");
	process.exit();
}

// Send login info for authentication token
console.log("Sending authentication request...");
var request = require('request');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
authUrl = "https://" + authHostName + ":" + authPort + "/getToken"
request.post({url:authUrl, form: {'username':user,'password':password}}, function(err,httpResponse,body){

	if (err) {
		return console.error('Token request failed:', err);
	}

	var authJson = JSON.parse(body);

	console.log("RECEIVED:");
	console.log(JSON.stringify(authJson, null, 2));

	if (authJson.success == true) {
		httpResponse.headers['set-cookie'].forEach(function(value){
		  if (value.startsWith("AuthToken")) {
			  authToken = value.substring(value.indexOf("=") + 1, value.indexOf(";"));
		  }
		});

		console.log("Authentication Succeeded. Received AuthToken: " + authToken);

		var WebSocket = require('ws')
		var WS_URL = 'ws://' + hostName + ':' + port + '/WebSocket';
		console.log("Connecting to WebSocket " + WS_URL + " ...")
		_websocket = new WebSocket(WS_URL, "tr_json2", {'headers':{'Cookie':"AuthToken=" + authToken + ";AuthPosition=" + position + ";applicationId=" + appId +";"}});
		_websocket.onopen = onOpen;
		_websocket.onclose = onClose;
		_websocket.onmessage = onMessage;
		_websocket.onerror = onError;
	}
	else {
		console.log("Authentication failed")
	}
});



// Create and send simple Market Price request
function sendMarketPriceRequest()
{
	var msg = '{"ID":2,"Key":{"Name":"TRI.N"}}';
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
