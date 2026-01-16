#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.
#|                See the project's LICENSE.md for details.
#|           Copyright (C) 2017-2020,2024,2026 LSEG. All rights reserved.
#|-----------------------------------------------------------------------------


# Simple example of outputting Market Price JSON data using Websockets

require("websocket")
require("jsonlite")
require("GetoptLong")

config = ""
if(.Platform$OS.type == "unix") {
  config = system("ifconfig", intern=TRUE)
  ips = config[grep("inet addr", config)]
  if(length(ips) == 0) {
    ips = config[grep("inet ", config)]
    positions = strsplit(gsub(".*inet ([[:digit:]])", "\\1", ips), " ")
    first_positions = lapply(positions, '[[', 1)
    position = first_positions[first_positions != "127.0.0.1"][[1]]
  }
  else {
    position = strsplit(gsub(".*inet addr:([[:digit:]])", "\\1", ips), " ")[[1]][[1]]
  }
} else {
  config = system("ipconfig", intern=TRUE)
  ips = config[grep("IPv4", config)]
  position = strsplit(gsub(".*? ([[:digit:]])", "\\1", ips), " ")[[1]][[1]]
}

# Global Default Variables
hostname = "127.0.0.1"
port = "15000"
user = "root"
app_id = "256"

# Get command line parameters
GetoptLong(
  "hostname=s","",
  "port=s","",
  "user=s","",
  "app_id=s","",
  "position=s",""
)

ping_timeout_interval = 30
ping_timeout_time = 0
ping_send_time = 0

# Start websocket handshake
ws_address = paste("ws://", hostname, ":", port, "/WebSocket", sep="")
cat(paste("Connecting to WebSocket", ws_address, "...\n"))
ws = WebSocket$new(ws_address, protocols="tr_json2")

# Create and send simple Market Price request
send_market_price_request = function(ws) {
  mp_req_json_string = "{\"ID\":2,\"Streaming\":false,\"Key\":{\"Name\":\"TRI.N\"}}"
  mp_req_json = fromJSON(mp_req_json_string)
  ws$send(mp_req_json_string)
  cat("SENT:\n")
  cat(toJSON(mp_req_json, pretty=TRUE, auto_unbox=TRUE))
  cat("\n")
}

# Parse at high level and output JSON of message
process_message = function(ws, message_json) {
  message_type = message_json$Type

  if(message_type == "Refresh") {
	message_domain = message_json$Domain
	if(!is.null(message_domain)) {
	  if(message_domain == "Login") {
      ping_timeout_interval <<- as.integer(message_json$Elements$PingTimeout)
	    send_market_price_request(ws)
	  }
	}
  } else if (message_type == "Ping") {
    pong_json_string = "{\"Type\":\"Pong\"}"
    pong_json = fromJSON(pong_json_string)
    ws$send(pong_json_string)
    cat("SENT:\n")
    cat(toJSON(pong_json, pretty=TRUE, auto_unbox=TRUE))
    cat("\n")
  }
}

# Generate a login request from command line data (or defaults) and send
send_login_request = function(ws) {
  login_json_string = "{\"ID\":1,\"Domain\":\"Login\",\"Key\":{\"Name\":\"<USER>\",\"Elements\":{\"ApplicationId\":\"<APP_ID>\",\"Position\":\"<POSITION>\"}}}"
  login_json_string = gsub("<USER>", user, login_json_string)
  login_json_string = gsub("<APP_ID>", app_id, login_json_string)
  login_json_string = gsub("<POSITION>", position, login_json_string)
  login_json = fromJSON(login_json_string)
  ws$send(login_json_string)
  cat("SENT:\n")
  cat(toJSON(login_json, pretty=TRUE, auto_unbox=TRUE))
  cat("\n")
}

# Called when handshake is complete and websocket is open, send login
ws$onOpen(function(event) {
cat("WebSocket successfully connected!\n")
  send_login_request(ws)
})

# Called when message received, parse message into JSON for processing
ws$onMessage(function(event) {
  message = event$data
  jsonArray = fromJSON(message, simplifyDataFrame=FALSE)

  cat("RECEIVED:\n")
  cat(toJSON(jsonArray, pretty=TRUE, auto_unbox=TRUE))
  cat("\n")

  for (singleMsg in jsonArray)
     process_message(ws, singleMsg)

   ping_timeout_time <<- 0
   ping_send_time <<- Sys.time() + ping_timeout_interval / 3
})
# Called when message received, parse message into JSON for processing
r = function(DATA,WS,...) {

}

# Called when websocket is closed
ws$onClose(function(event) {
  cat("WebSocket Closed\n")
})

# Event loop and ping
ping_timeout_time = 0
ping_send_time = Sys.time() + ping_timeout_interval / 3
while (TRUE) {
  if (ping_send_time > 0 && Sys.time() > ping_send_time) {
    ping_json_string = "{\"Type\":\"Ping\"}"
    ping_json = fromJSON(ping_json_string)
    ws$send(ping_json_string)
    cat("SENT:\n")
    cat(toJSON(ping_json, pretty=TRUE, auto_unbox=TRUE))
    cat("\n")

    ping_send_time = 0
    ping_timeout_time = Sys.time() + ping_timeout_interval

  }
  if (ping_timeout_time > 0 && Sys.time() > ping_timeout_time) {
    cat("No ping from server, timing out\n")
    ws$close()
    break
  }
}
