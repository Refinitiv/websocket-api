#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.
#|                See the project's LICENSE.md for details.
#|           Copyright (C) 2017-2020,2024,2026 LSEG. All rights reserved.
#|-----------------------------------------------------------------------------

#!/usr/bin/env python
""" Simple example of outputting Market Price JSON data using Websockets """

import sys
import time
import getopt
import socket
import json
import asyncio
import websockets
import threading
from threading import Thread, Event

# Global Default Variables
hostname = '127.0.0.1'
port = '15000'
user = 'root'
app_id = '256'
position = socket.gethostbyname(socket.gethostname())

# Global Variables
ping_timeout_interval = 30
ping_timeout_time = 0
ping_send_time = 0
web_socket_app = None
web_socket_open = False


async def process_message(ws, message_json):
    """ Parse at high level and output JSON of message """
    message_type = message_json['Type']

    if message_type == "Refresh":
        if 'Domain' in message_json:
            message_domain = message_json['Domain']
            if message_domain == "Login":
                await process_login_response(ws, message_json)
    elif message_type == "Ping":
        pong_json = { 'Type':'Pong' }
        await ws.send(json.dumps(pong_json))
        print("SENT:")
        print(json.dumps(pong_json, sort_keys=True, indent=2, separators=(',', ':')))


async def process_login_response(ws, message_json):
    """ Parse login message to set/start client ping and send item request """
    global ping_timeout_interval

    ping_timeout_interval = int(message_json['Elements']['PingTimeout'])

    await send_market_price_request(ws)


async def send_market_price_request(ws):
    """ Create and send simple Market Price request """
    mp_req_json = {
        'ID': 2,
        'Streaming': False,
        'Key': {
            'Name': 'TRI.N',
        },
    }
    await ws.send(json.dumps(mp_req_json))
    print("SENT:")
    print(json.dumps(mp_req_json, sort_keys=True, indent=2, separators=(',', ':')))


async def send_login_request(ws):
    """ Generate a login request from command line data (or defaults) and send """
    login_json = {
        'ID': 1,
        'Domain': 'Login',
        'Key': {
            'Name': '',
            'Elements': {
                'ApplicationId': '',
                'Position': ''
            }
        }
    }

    login_json['Key']['Name'] = user
    login_json['Key']['Elements']['ApplicationId'] = app_id
    login_json['Key']['Elements']['Position'] = position

    await ws.send(json.dumps(login_json))
    print("SENT:")
    print(json.dumps(login_json, sort_keys=True, indent=2, separators=(',', ':')))

async def received_message(ws):
    global ping_timeout_time, ping_send_time, ping_timeout_interval
    async for message in ws:
        print("RECEIVED:")
        message_json = json.loads(message)
        print(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))
        ping_timeout_time = 0
        ping_send_time = time.time() + ping_timeout_interval / 3
        for singleMsg in message_json:
            await process_message(ws, singleMsg)

async def ping_loop(ws):
    global ping_timeout_time, ping_send_time, ping_timeout_interval
    while (True):
            await asyncio.sleep(1)
            if ping_send_time > 0 and time.time() > ping_send_time:
                ping_json = { 'Type':'Ping' }
                await ws.send(json.dumps(ping_json))
                print("SENT:")
                print(json.dumps(ping_json, sort_keys=True, indent=2, separators=(',', ':')))
                ping_send_time = 0
                ping_timeout_time = time.time() + ping_timeout_interval
            if ping_timeout_time > 0 and time.time() > ping_timeout_time:
                print("No ping from server, timing out")
                ws.close()
                break

async def websocket_handler(uri):
    global ping_timeout_time
    global ping_send_time
    async with websockets.connect(uri, 
        ping_interval=100,  # seconds
        ping_timeout=10,
        subprotocols=['tr_json2']) as ws:
        print("WebSocket successfully connected!")
        await send_login_request(ws)


        await asyncio.gather(
            received_message(ws),
            ping_loop(ws)
        )


if __name__ == "__main__":

    # Get command line parameters
    try:
        opts, args = getopt.getopt(sys.argv[1:], "", ["help", "hostname=", "port=", "app_id=", "user=", "position="])
    except getopt.GetoptError:
        print('Usage: market_price.py [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--help]')
        sys.exit(2)
    for opt, arg in opts:
        if opt in ("--help"):
            print('Usage: market_price.py [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--help]')
            sys.exit(0)
        elif opt in ("--hostname"):
            hostname = arg
        elif opt in ("--port"):
            port = arg
        elif opt in ("--app_id"):
            app_id = arg
        elif opt in ("--user"):
            user = arg
        elif opt in ("--position"):
            position = arg

    # Start websocket handshake
    ws_address = "ws://{}:{}/WebSocket".format(hostname, port)
    print("Connecting to WebSocket " + ws_address + " ...")
    try:
        asyncio.run(websocket_handler(ws_address))
    except KeyboardInterrupt:
        print("\nShutting down...\n")
