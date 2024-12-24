#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license 
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.
#|                See the project's LICENSE.md for details.
#|           Copyright (C) 2017-2020,2024 LSEG. All rights reserved.
#|-----------------------------------------------------------------------------

#!/usr/bin/ruby
# * Simple example of outputting Market Price JSON data using Websockets

require 'rubygems'
require 'websocket-client-simple'
require 'json'
require 'optparse'
require 'socket'

# Global Default Variables
$hostname = '127.0.0.1'
$port = '15000'
$user = 'root'
$app_id = '256'
$position = Socket.ip_address_list[0].ip_address

# Global Variables
$ping_timeout_interval = 30
$ping_timeout_time = nil
$ping_send_time = nil

# Get command line parameters
opt_parser = OptionParser.new do |opt|

  opt.on('--hostname HOST','HOST') do |hostname|
    $hostname = hostname
  end

  opt.on('--port port','port') do |port|
    $port = port
  end

  opt.on('--user USER','USER') do |user|
    $user = user
  end

  opt.on('--app_id APP_ID','APP_ID') do |app_id|
    $app_id = app_id
  end

  opt.on('--position POSITION','POSITION') do |position|
    $position = position
  end

  opt.on('--help','HELP') do |help|
	puts 'Usage: market_price.rb [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--help]'
	exit 0
  end
end

opt_parser.parse!

# Create and send simple Market Price request
def send_market_price_request(ws)
  mp_req_json_hash = {
    'ID' => 2,
	'Streaming' => false,
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
      $ping_timeout_interval = Integer(message_json['Elements']['PingTimeout'])
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

# Start websocket handshake
ws_address = "ws://#{$hostname}:#{$port}/WebSocket"
puts "Connecting to WebSocket #{ws_address} ..."
ws = WebSocket::Client::Simple.connect(ws_address,{:headers => {'Sec-WebSocket-Protocol' => 'tr_json2'}})

# Called when message received, parse message into JSON for processing
ws.on :message do |msg|
  msg = msg.to_s

  puts 'RECEIVED:'

  json_array = JSON.parse(msg)

  puts JSON.pretty_generate(json_array)

  for single_msg in json_array
    process_message(ws, single_msg)
  end

  $ping_timeout_time = nil
  $ping_send_time = Time.now + $ping_timeout_interval / 3

end

# Called when handshake is complete and websocket is open, send login
ws.on :open do
  puts 'WebSocket successfully connected!'
  $ping_timeout_time = nil
  $ping_send_time = Time.now + $ping_timeout_interval / 3
  Thread.new do
    loop do
      sleep 1
      if $ping_send_time != nil and Time.now > $ping_send_time
        ping_json_hash = {
          'Type' => 'Ping',
        }
        ws.send ping_json_hash.to_json.to_s
        puts 'SENT:'
        puts JSON.pretty_generate(ping_json_hash)
        $ping_send_time = nil
        $ping_timeout_time = Time.now + $ping_timeout_interval
      end
      if $ping_timeout_time != nil and Time.now > $ping_timeout_time
        puts 'No ping from server, timing out'
        exit 1
      end
    end
  end

  login_hash = {
    'ID' => 1,
    'Domain' => 'Login',
    'Key' => {
      'Name' => '',
      'Elements' => {
        'ApplicationId' => '',
        'Position' => ''
      }
    }
  }

  login_hash['Key']['Name'] = $user
  login_hash['Key']['Elements']['ApplicationId'] = $app_id
  login_hash['Key']['Elements']['Position'] = $position

  ws.send login_hash.to_json.to_s
  puts 'SENT:'
  puts JSON.pretty_generate(login_hash)
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
