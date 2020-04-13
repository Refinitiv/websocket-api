#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license      --
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
#|                See the project's LICENSE.md for details.                  --
#|           Copyright (C) 2019-2020 Refinitiv. All rights reserved.         --
#|-----------------------------------------------------------------------------


#!/usr/bin/ruby
# * Simple example of outputting Market Price JSON data using Websockets with authentication

require 'rubygems'
require 'websocket-client-simple'
require 'json'
require 'optparse'
require 'socket'
require 'http'

# Global Default Variables
$hostname = '127.0.0.1'
$port = '15000'
$user = 'root'
$app_id = '555'
$position = Socket.ip_address_list[0].ip_address
$auth_hostname = '127.0.0.1'
$auth_port = '8443'
$user = ''
$password = ''
$auth_token = ''

# Get command line parameters
opt_parser = OptionParser.new do |opt|

  opt.on('--hostname HOST','HOST') do |hostname|
    $hostname = hostname
  end

  opt.on('--port port','port') do |port|
    $port = port
  end
  
  opt.on('--app_id APP_ID','APP_ID') do |app_id|
    $app_id = app_id
  end
  
  opt.on('--user USER','USER') do |user|
    $user = user
  end
  
  opt.on('--password PASSWORD','PASSWORD') do |password|
    $password = password
  end
  
  opt.on('--position POSITION','POSITION') do |position|
    $position = position
  end
  
  opt.on('--auth_hostname AUTH_HOSTNAME','AUTH_HOSTNAME') do |auth_hostname|
    $auth_hostname = auth_hostname
  end
  
  opt.on('--auth_port AUTH_PORT','AUTH_PORT') do |auth_port|
    $auth_port = auth_port
  end
  
  opt.on('--help','HELP') do |help|
	puts 'Usage: market_price_authentication.rb [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--password password] [--position position] [--auth_hostname auth_hostname] [--auth_port auth_port] [--help]'
	exit 0
  end
end

opt_parser.parse!

# Create and send simple Market Price request
def send_market_price_request(ws)
  mp_req_json_hash = {
    'ID' => 2,
    'Key' => {
      'Name' => 'TRI.N'
    }
  }
  ws.send mp_req_json_hash.to_json.to_s
  puts 'SENT:'
  puts JSON.pretty_generate(mp_req_json_hash) 
end

# Parse at high level and output JSON of message
def process_message(ws, message_json)
  message_type = message_json['Type']
  
  if message_type == 'Refresh' then
    message_domain = message_json['Domain']
	if message_domain != nil then
	  if message_domain == 'Login' then
	    send_market_price_request(ws)
	  end
	end
  elsif message_type == 'Ping' then
    pong_json_hash = {
	    'Type' => 'Pong',
    }
    ws.send pong_json_hash.to_json.to_s
    puts 'SENT:'
    puts JSON.pretty_generate(pong_json_hash)  
  end
end

# Send login info for authentication token
puts 'Sending authentication request...'
headers = {'Content-Type' => 'application/x-www-form-urlencoded'}
url = "https://#{$auth_hostname}:#{$auth_port}/getToken"
data = "username=#{$user}&password=#{$password}"
ctx = OpenSSL::SSL::SSLContext.new
ctx.verify_mode = OpenSSL::SSL::VERIFY_NONE
auth_resp = HTTP.headers(headers).post(url, body: data, ssl_context: ctx)
auth_json = JSON.parse(auth_resp.to_s)

puts 'RECEIVED:'
puts JSON.pretty_generate(auth_json)
 
if auth_json['success'] == true
  # Start websocket handshake
  ws_address = "ws://#{$hostname}:#{$port}/WebSocket"
  $auth_token = HTTP::Cookie.cookie_value_to_hash(HTTP::Cookie.cookie_value(auth_resp.cookies.cookies))["AuthToken"]
  puts "Authentication Succeeded. Received AuthToken: #{$auth_token}"
  puts "Connecting to WebSocket #{ws_address} ..."
  ws = WebSocket::Client::Simple.connect(ws_address,{headers: {'Sec-WebSocket-Protocol' => 'tr_json2', 'User-Agent' => 'Ruby', 'Cookie' => "AuthToken=#{$auth_token};AuthPosition=#{$position};applicationId=#{$app_id};"}})
else
  puts "Authentication failed"
  exit -1
end

# Called when message received, parse message into JSON for processing
ws.on :message do |msg|
  msg = msg.to_s

  puts 'RECEIVED:'

  json_array = JSON.parse(msg)

  puts JSON.pretty_generate(json_array)

  for single_msg in json_array
    process_message(ws, single_msg)
  end
  
end

# Called when handshake is complete and websocket is open, send login
ws.on :open do
  puts 'WebSocket successfully connected!'
end

# Called when websocket is closed
ws.on :close do |e|
  puts 'CLOSED'
  p e
  exit 1
end

# Called when websocket error has occurred 
ws.on :error do |e|
  puts 'ERROR'
  p e
end

sleep
