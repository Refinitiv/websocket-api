#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license      --
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
#|                See the project's LICENSE.md for details.                  --
#|           Copyright (C) Refinitiv 2019. All rights reserved.              --
#|-----------------------------------------------------------------------------


# Simple example of outputting Market Price JSON data using Websockets with authentication

require("websockets") # https://github.com/brettjbush/R-Websockets
require("jsonlite")
require("curl")
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
app_id = "555"
auth_token = ""
auth_hostname = "127.0.0.1"
auth_port = "8443"
password = ""

# Get command line parameters
GetoptLong(
  "hostname=s","",
  "port=s","",
  "user=s","",
  "app_id=s","",
  "position=s","",
  "password=s","",
  "auth_hostname=s","",
  "auth_port=s",""
)

# Send login info for authentication token
cat("Sending authentication request...\n")
content = paste("username=", user, "&password=", password, sep="")
h <- new_handle(copypostfields = content)
handle_setheaders(h,
  "Content-Type" = "application/x-www-form-urlencoded"
)
handle_setopt(h, ssl_verifypeer = FALSE, ssl_verifyhost = FALSE)
auth_url = paste("https://", auth_hostname, ":", auth_port, "/getToken", sep="")
req <- curl_fetch_memory(auth_url, handle = h)
res_headers = parse_headers(req$headers)
auth_json_string = rawToChar(req$content)
auth_json = fromJSON(auth_json_string)

cat("RECEIVED:\n")
cat(toJSON(auth_json, pretty=TRUE, auto_unbox=TRUE))
cat("\n")

if (auth_json$success) {
  auth_token = gsub(".*AuthToken=([[:alnum:]]*);.*", "\\1", grep('AuthToken', res_headers, value=TRUE))
  cat("Authentication Succeeded. Received AuthToken: ")
  cat(auth_token)
  cat("\n")

  # Start websocket handshake
  ws_address = paste("ws://", hostname, ":", port, "/WebSocket", sep="")
  cat(paste("Connecting to WebSocket", ws_address, "...\n"))
  header_entry = list(name="Cookie", value=paste("AuthToken=", auth_token, ";AuthPosition=", position, ";applicationId=", app_id,";", sep=""))
  headers_list = list(header_entry)
  con = websocket(ws_address, port=as.integer(port), subprotocol="tr_json2", headers=headers_list, version=13)

  # Create and send simple Market Price request
  send_market_price_request = function(con) {
    mp_req_json_string = "{\"ID\":2,\"Key\":{\"Name\":\"TRI.N\"}}"
    mp_req_json = fromJSON(mp_req_json_string)
    websocket_write(mp_req_json_string, con)
    cat("SENT:\n")
    cat(toJSON(mp_req_json, pretty=TRUE, auto_unbox=TRUE))
    cat("\n")
  }

  # Parse at high level and output JSON of message
  process_message = function(con, message_json) {
    message_type = message_json$Type

    if(message_type == "Refresh") {
  	message_domain = message_json$Domain
  	if(!is.null(message_domain)) {
  	  if(message_domain == "Login") {
  	    send_market_price_request(con)
  	  }
    }
    } else if (message_type == "Ping") {
      pong_json_string = "{\"Type\":\"Pong\"}"
      pong_json = fromJSON(pong_json_string)
      websocket_write(pong_json_string, con)
      cat("SENT:\n")
      cat(toJSON(pong_json, pretty=TRUE, auto_unbox=TRUE))
      cat("\n")
      }
  }

  # Called when handshake is complete and websocket is open, send login
  e = function(WS) {
    cat("WebSocket successfully connected!\n")
  }
  set_callback("established", e, con)

  # Called when message received, parse message into JSON for processing
  r = function(DATA,WS,...) {
    message = rawToChar(DATA)
    jsonArray = fromJSON(message, simplifyDataFrame=FALSE)

    cat("RECEIVED:\n")
    cat(toJSON(jsonArray, pretty=TRUE, auto_unbox=TRUE))
    cat("\n")

    for (singleMsg in jsonArray)
       process_message(con, singleMsg)
  }
  set_callback("receive", r, con)

  # Called when websocket is closed
  c = function(WS) {
    cat("WebSocket Closed\n")
  }
  set_callback("closed", c, con)

  while (TRUE) {
    service(con)
  }
} else {
  cat("Authentication failed")
}
