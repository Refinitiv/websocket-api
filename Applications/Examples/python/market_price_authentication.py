#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license      --
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
#|                See the project's LICENSE.md for details.                  --
#|           Copyright Thomson Reuters 2017. All rights reserved.            --
#|-----------------------------------------------------------------------------


#!/usr/bin/env python
""" Simple example of outputting Market Price JSON data using Websockets with authentication """

import sys
import time
import getopt
import requests
import socket
import json
import websocket
import threading
from threading import Thread, Event
from requests.packages.urllib3.exceptions import InsecureRequestWarning
requests.packages.urllib3.disable_warnings(InsecureRequestWarning)

# Global Default Variables
app_id = '555'
auth_hostname = '127.0.0.1'
auth_port = '8443'
hostname = '127.0.0.1'
password = ''
position = socket.gethostbyname(socket.gethostname())
token = ''
user = ''
port = '15000'

# Global Variables
web_socket_app = None
web_socket_open = False


def process_message(ws, message_json):
    """ Parse at high level and output JSON of message """
    message_type = message_json['Type']

    if message_type == "Refresh":
        if 'Domain' in message_json:
            message_domain = message_json['Domain']
            if message_domain == "Login":
                process_login_response(ws, message_json)
    elif message_type == "Ping":
        pong_json = { 'Type':'Pong' }
        ws.send(json.dumps(pong_json))
        print("SENT:")
        print(json.dumps(pong_json, sort_keys=True, indent=2, separators=(',', ':')))


def process_login_response(ws, message_json):
    """ Send item request """
    send_market_price_request(ws)


def send_market_price_request(ws):
    """ Create and send simple Market Price request """
    mp_req_json = {
        'ID': 2,
        'Key': {
            'Name': 'TRI.N',
        },
    }
    ws.send(json.dumps(mp_req_json))
    print("SENT:")
    print(json.dumps(mp_req_json, sort_keys=True, indent=2, separators=(',', ':')))


def on_message(ws, message):
    """ Called when message received, parse message into JSON for processing """
    print("RECEIVED: ")
    message_json = json.loads(message)
    print(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))

    for singleMsg in message_json:
        process_message(ws, singleMsg)


def on_error(ws, error):
    """ Called when websocket error has occurred """
    print(error)


def on_close(ws):
    """ Called when websocket is closed """
    global web_socket_open
    web_socket_open = False
    print("WebSocket Closed")


def on_open(ws):
    """ Called when handshake is complete and websocket is open, send login """

    print("WebSocket successfully connected!")
    global web_socket_open
    web_socket_open = True


if __name__ == "__main__":

    # Get command line parameters
    try:
        opts, args = getopt.getopt(sys.argv[1:], "", ["help", "hostname=", "port=", "app_id=", "user=", "password=", "position=", "auth_hostname=", "auth_port="])
    except getopt.GetoptError:
        print('Usage: market_price_authentication.py [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--password password] [--position position] [--auth_hostname auth_hostname] [--auth_port auth_port] [--help]')
        sys.exit(2)
    for opt, arg in opts:
        if opt in ("--help"):
            print('Usage: market_price_authentication.py [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--password password] [--position position] [--auth_hostname auth_hostname] [--auth_port auth_port] [--help]')
            sys.exit(0)
        elif opt in ("--hostname"):
            hostname = arg
        elif opt in ("--port"):
            port = arg
        elif opt in ("--app_id"):
            app_id = arg
        elif opt in ("--user"):
            user = arg
        elif opt in ("--password"):
            password = arg
        elif opt in ("--position"):
            position = arg
        elif opt in ("--auth_hostname"):
            auth_hostname = arg
        elif opt in ("--auth_port"):
            auth_port = arg

    # Send login info for authentication token
    print("Sending authentication request...")
    r = requests.post('https://{}:{}/getToken'.format(auth_hostname, auth_port),
                      data={'username': user, 'password': password},
                      verify=False)
    auth_json = r.json()
    print("RECEIVED:")
    print(json.dumps(auth_json, sort_keys=True, indent=2, separators=(',', ':')))

    if auth_json['success'] is True:
        token = r.cookies['AuthToken']
        print('Authentication Succeeded. Received AuthToken: {}'.format(token))
        
        cookie = "AuthToken={};AuthPosition={};applicationId={};".format(token, position, app_id)
        
        # Start websocket handshake
        ws_address = "ws://{}:{}/WebSocket".format(hostname, port)
        print("Connecting to WebSocket " + ws_address + " ...")
        web_socket_app = websocket.WebSocketApp(ws_address, on_message=on_message,
                                                on_error=on_error,
                                                on_close=on_close,
                                                subprotocols=['tr_json2'],
                                                cookie=cookie)
        web_socket_app.on_open = on_open

        # Event loop
        wst = threading.Thread(target=web_socket_app.run_forever)
        wst.start()

        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            web_socket_app.close()
    else:
        print('Authentication failed')
