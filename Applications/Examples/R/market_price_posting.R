#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license 
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.
#|                See the project's LICENSE.md for details.
#|           Copyright (C) 2017-2020,2024 LSEG. All rights reserved.
#|-----------------------------------------------------------------------------


# Simple example of outputting Market Price JSON data using Websockets

require("websockets") # https://github.com/brettjbush/R-Websockets
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

next_post_time = 0
post_id = 1

# Start websocket handshake
ws_address = paste("ws://", hostname, ":", port, "/WebSocket", sep="")
cat(paste("Connecting to WebSocket", ws_address, "...\n"))
con = websocket(ws_address, port=as.integer(port), subprotocol="tr_json2", version=13)

# Create and send simple Market Price request
send_market_price_request = function(con) {
  mp_req_json_string = "{\"ID\":2,\"Key\":{\"Name\":\"TRI.N\"}}"
  mp_req_json = fromJSON(mp_req_json_string)
  websocket_write(mp_req_json_string, con)
  cat("SENT:\n")
  cat(toJSON(mp_req_json, pretty=TRUE, auto_unbox=TRUE))
  cat("\n")
}

# Create and send simple Market Price post
send_market_price_post = function(con) {
  mp_post_json_string =
		"{ \"ID\": 2, \"Type\": \"Post\", \"Domain\": \"MarketPrice\",
            \"Ack\": true, \"PostID\": <POSTID>,
			\"PostUserInfo\": { \"Address\": \"<ADDRESS>\", \"UserID\": <USERID> },
            \"Message\": {
                \"ID\":0,
                \"Type\":\"Update\",
                \"Fields\": {\"BID\": 45.55, \"BIDSIZE\": 18, \"ASK\": 45.57, \"ASKSIZE\": 19}
            }
		}"

	#Use the IP address as the Post User Address
	mp_post_json_string = gsub("<ADDRESS>", position, mp_post_json_string)

	#Use the current process ID as the Post User ID
	mp_post_json_string = gsub("<USERID>", Sys.getpid(), mp_post_json_string)

	mp_post_json_string = gsub("<POSTID>", post_id, mp_post_json_string)

  mp_post_json = fromJSON(mp_post_json_string)
  websocket_write(mp_post_json_string, con)
  cat("SENT:\n")
  cat(toJSON(mp_post_json, pretty=TRUE, auto_unbox=TRUE))
  cat("\n")

  next_post_time <<- Sys.time() + 3
  post_id <<- post_id + 1
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

    if (as.integer(message_json$ID) == 2)
    {
        if (next_post_time == 0 &&
                (is.null(message_json$State) || message_json$State$Stream == "Open" && message_json$State$Data == "Ok"))
        {
            next_post_time <<- Sys.time() + 3
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

# Generate a login request from command line data (or defaults) and send
send_login_request = function(con) {
  login_json_string = "{\"ID\":1,\"Domain\":\"Login\",\"Key\":{\"Name\":\"<USER>\",\"Elements\":{\"ApplicationId\":\"<APP_ID>\",\"Position\":\"<POSITION>\"}}}"
  login_json_string = gsub("<USER>", user, login_json_string)
  login_json_string = gsub("<APP_ID>", app_id, login_json_string)
  login_json_string = gsub("<POSITION>", position, login_json_string)
  login_json = fromJSON(login_json_string)
  websocket_write(login_json_string, con)
  cat("SENT:\n")
  cat(toJSON(login_json, pretty=TRUE, auto_unbox=TRUE))
  cat("\n")
}

# Called when handshake is complete and websocket is open, send login
e = function(WS) {
  cat("WebSocket successfully connected!\n")
  send_login_request(con)
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
  if (next_post_time != 0 && Sys.time() > next_post_time) {
    send_market_price_post(con)
  }
}
