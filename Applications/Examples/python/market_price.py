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
snapshot = False
service = 'ELEKTRON_DD'

updateTypeFilter = -1           # Determines what Update Types are desired. See more details below.
negativeUpdateTypeFilter = -1   # Determines what Update Types are NOT desired. See more details below.

#### Update Type Filters ####
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


# Global Variables
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
    """ Send item request """
    await send_market_price_request(ws)


async def send_market_price_request(ws):
    """ Create and send simple Market Price request """
    mp_req_json = {
        'ID': 2,
        'Key': {
            'Name': 'TRI.N',
            'Service': '',
        },
        'Streaming': not snapshot,
    }
    mp_req_json['Key']['Service'] = service

    await ws.send(json.dumps(mp_req_json))
    print("SENT:")
    print(json.dumps(mp_req_json, sort_keys=True, indent=2, separators=(',', ':')))


async def send_login_request(ws):
    global updateTypeFilter, negativeUpdateTypeFilter
    """ Generate a login request from command line data (or defaults) and send """
    login_json = {
        'ID': 1,
        'Domain': 'Login',
        'Key': {
            'Name': '',
            'Elements': {
                'ApplicationId': '',
                'Position': '',
            }
        }
    }

    login_json['Key']['Name'] = user
    login_json['Key']['Elements']['ApplicationId'] = app_id
    login_json['Key']['Elements']['Position'] = position
    if (updateTypeFilter != -1):
        login_json['Key']['Elements']['UpdateTypeFilter'] = int(updateTypeFilter)
    if (negativeUpdateTypeFilter != -1):
        login_json['Key']['Elements']['NegativeUpdateTypeFilter'] = int(negativeUpdateTypeFilter)

    await ws.send(json.dumps(login_json))
    print("SENT:")
    print(json.dumps(login_json, sort_keys=True, indent=2, separators=(',', ':')))

async def websocket_handler(uri):
    async with websockets.connect(uri,
        ping_interval=100,   # seconds
        ping_timeout=10,
        subprotocols=['tr_json2']) as ws:
        print("WebSocket successfully connected!")
        await send_login_request(ws)

        async for message in ws:
            print("RECEIVED:")
            message_json = json.loads(message)
            print(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))
            for singleMsg in message_json:
                await process_message(ws, singleMsg)


if __name__ == "__main__":

    # Get command line parameters
    try:
        opts, args = getopt.getopt(sys.argv[1:], "", ["help", "hostname=", "port=", "app_id=", "user=", "position=", "updateTypeFilter=", "negativeUpdateTypeFilter=", "service=", "snapshot"])
    except getopt.GetoptError:
        print('Usage: market_price.py [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--snapshot] [--updateTypeFilter] [--negativeUpdateTypeFilter] [--service] [--help]')
        sys.exit(2)
    for opt, arg in opts:
        if opt in ("--help"):
            print('Usage: market_price.py [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--snapshot] [--updateTypeFilter] [--negativeUpdateTypeFilter] [--service] [--help]')
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
        elif opt in "--snapshot":
            snapshot = True
        elif opt in ("--updateTypeFilter"):
            updateTypeFilter = arg
        elif opt in ("--negativeUpdateTypeFilter"):
            negativeUpdateTypeFilter = arg
        elif opt in ("--service"):
            service = arg

    # Start websocket handshake
    ws_address = "ws://{}:{}/WebSocket".format(hostname, port)
    print("Connecting to WebSocket " + ws_address + " ...")
    try:
        asyncio.run(websocket_handler(ws_address))
    except KeyboardInterrupt:
        ws.close()
        print("\nShutting down...\n")