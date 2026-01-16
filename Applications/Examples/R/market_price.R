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
snapshot = FALSE
updateTypeFilter = -1
negativeUpdateTypeFilter = -1
service = ""

# Get command line parameters
GetoptLong(
  "hostname=s","",
  "port=s","",
  "user=s","",
  "app_id=s","",
  "position=s","",
  "snapshot!","",
  "updateTypeFilter=i","", 		# See Below for details about Update Type Filter
  "negativeUpdateTypeFilter=i","", 	# See Below for details about Negative Update Type Filter
  "service=s",""
)

#### Update Type Filters ####
# Update Type Filter specifies the update types DESIRED
# Negative Update Type Filter specifies the update types NOT DESIRED
# A single value, converted into binary, can be used to determine what Update Types
#    you want filtered in your responses.
# UPDATE TYPE FILTERS
# Unspecified = 0x001
# Quote = 0x002
# Trade = 0x004
# News Alert = 0x008
# Volume Alert = 0x010
# Order Indication = 0x020
# Closing Run = 0x040
# Correction = 0x080
# Market Digest = 0x100
# Quotes Trade = 0x200
# Multiple = 0x400
# Verify = 0x800

# Start websocket handshake
ws_address = paste("ws://", hostname, ":", port, "/WebSocket", sep="")
cat(paste("Connecting to WebSocket", ws_address, "...\n"))
ws = WebSocket$new(ws_address, protocols="tr_json2")

# Create and send simple Market Price request
send_market_price_request = function(ws) {
  serviceInfo = ""
  if (nchar(service) > 0) {
	serviceInfo <- paste(",\"Service\":\"", service, "\"", sep = "")
  }
  mp_req_json_string = paste("{\"ID\":2,\"Key\":{\"Name\":\"TRI.N\"", serviceInfo, "},", "\"Streaming\":", tolower(!snapshot), "}")
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
  updateTypeFilterText = ""
  negativeUpdateTypeFilterText = ""
  if (updateTypeFilter != -1)
    updateTypeFilterText = paste(",\"UpdateTypeFilter\":", updateTypeFilter)
  if (negativeUpdateTypeFilter != -1)
    negativeUpdateTypeFilterText = paste(",\"NegativeUpdateTypeFilter\":", negativeUpdateTypeFilter)
  login_json_string = paste("{\"ID\":1,\"Domain\":\"Login\",\"Key\":{\"Name\":\"<USER>\",\"Elements\":{\"ApplicationId\":\"<APP_ID>\",\"Position\":\"<POSITION>\"",updateTypeFilterText,negativeUpdateTypeFilterText,"}}}")
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
})

# Called when websocket is closed
ws$onClose(function(event) {
  cat("WebSocket Closed\n")
})

ws$onError(function(event) {
cat("Client failed to connect: ", event$message, "\n")
})

while (TRUE) {
  
}


