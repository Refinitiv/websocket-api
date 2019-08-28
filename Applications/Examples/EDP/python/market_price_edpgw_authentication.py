#!/usr/bin/env python
#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license      --
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
#|                See the project's LICENSE.md for details.                  --
#|           Copyright Thomson Reuters 2018. All rights reserved.            --
#|-----------------------------------------------------------------------------
"""
Simple example of authenticating to EDP-GW and using the token to login and
retrieve MarketPrice content.  A username and password are used to retrieve
this token.
"""

import sys
import time
import getopt
import requests
import socket
import json
import websocket
import threading

# Global Default Variables
app_id = '256'
auth_url = 'https://api.refinitiv.com:443/auth/oauth2/beta1/token'
hostname = ''
password = ''
position = ''
sts_token = ''
refresh_token = ''
user = ''
clientid = ''
port = '443'
client_secret = ''
scope = 'trapi'
ric = '/TRI.N'
service = 'ELEKTRON_DD'
view = []
no_streaming = False

# Global Variables
web_socket_app = None
web_socket_open = False
logged_in = False


def process_message(message_json):
    """ Parse at high level and output JSON of message """
    message_type = message_json['Type']

    if message_type == "Refresh":
        if 'Domain' in message_json:
            message_domain = message_json['Domain']
            if message_domain == "Login":
                process_login_response(message_json)
    elif message_type == "Ping":
        pong_json = {'Type': 'Pong'}
        web_socket_app.send(json.dumps(pong_json))
        print("SENT:")
        print(json.dumps(pong_json, sort_keys=True, indent=2, separators=(',', ':')))


def process_login_response(message_json):
    """ Send item request """
    global logged_in

    if message_json['State']['Stream'] != "Open" or message_json['State']['Data'] != "Ok":
        print("Login failed.")
        sys.exit(1)

    logged_in = True
    send_market_price_request(ric)


def send_market_price_request(ric_name):
    """ Create and send simple Market Price request """
    mp_req_json = {
        'ID': 2,
        'Key': {
            'Name': ric_name,
            'Service': service
        },
        'Streaming': not no_streaming,
    }

    if view:
        mp_req_json['View'] = view

    web_socket_app.send(json.dumps(mp_req_json))
    print("SENT:")
    print(json.dumps(mp_req_json, sort_keys=True, indent=2, separators=(',', ':')))


def send_login_request(auth_token, is_refresh_token):
    """
        Send login request with authentication token.
        Used both for the initial login and subsequent reissues to update the authentication token
    """
    login_json = {
        'ID': 1,
        'Domain': 'Login',
        'Key': {
            'NameType': 'AuthnToken',
            'Elements': {
                'ApplicationId': '',
                'Position': '',
                'AuthenticationToken': ''
            }
        }
    }

    login_json['Key']['Elements']['ApplicationId'] = app_id
    login_json['Key']['Elements']['Position'] = position
    login_json['Key']['Elements']['AuthenticationToken'] = auth_token

    # If the token is a refresh token, this is not our first login attempt.
    if is_refresh_token:
        login_json['Refresh'] = False

    web_socket_app.send(json.dumps(login_json))
    print("SENT:")
    print(json.dumps(login_json, sort_keys=True, indent=2, separators=(',', ':')))


def on_message(_, message):
    """ Called when message received, parse message into JSON for processing """
    print("RECEIVED: ")
    message_json = json.loads(message)
    print(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))

    for singleMsg in message_json:
        process_message(singleMsg)


def on_error(_, error):
    """ Called when websocket error has occurred """
    print(error)


def on_close(_):
    """ Called when websocket is closed """
    global web_socket_open
    web_socket_open = False
    print("WebSocket Closed")


def on_open(_):
    """ Called when handshake is complete and websocket is open, send login """

    print("WebSocket successfully connected!")
    global web_socket_open
    web_socket_open = True
    send_login_request(sts_token, False)


def get_sts_token(current_refresh_token, url=None):
    """
        Retrieves an authentication token.
        :param current_refresh_token: Refresh token retrieved from a previous authentication, used to retrieve a
        subsequent access token. If not provided (i.e. on the initial authentication), the password is used.
    """

    if url is None:
        url = auth_url

    if not current_refresh_token:  # First time through, send password
        if url.startswith('https'):
            data = {'username': user, 'password': password, 'grant_type': 'password', 'takeExclusiveSignOnControl': True,
                    'scope': scope}
        else:
            data = {'username': user, 'password': password, 'client_id': clientid, 'grant_type': 'password', 'takeExclusiveSignOnControl': True,
                    'scope': scope}
        print("Sending authentication request with password to", url, "...")
    else:  # Use the given refresh token
        if url.startswith('https'):
            data = {'username': user, 'refresh_token': current_refresh_token, 'grant_type': 'refresh_token'}
        else:
            data = {'username': user, 'client_id': clientid, 'refresh_token': current_refresh_token, 'grant_type': 'refresh_token'}
        print("Sending authentication request with refresh token to", url, "...")

    try:
        if url.startswith('https'):
            # Request with auth for https protocol
            r = requests.post(url,
                              headers={'Accept': 'application/json'},
                              data=data,
                              auth=(clientid, client_secret),
                              verify=True,
                              allow_redirects=False)
        else:
            # Request without auth for non https protocol (e.g. http)
            r = requests.post(url,
                              headers={'Accept': 'application/json'},
                              data=data,
                              verify=True,
                              allow_redirects=False)

    except requests.exceptions.RequestException as e:
        print('EDP-GW authentication exception failure:', e)
        return None, None, None

    if r.status_code == 200:
        auth_json = r.json()
        print("EDP-GW Authentication succeeded. RECEIVED:")
        print(json.dumps(auth_json, sort_keys=True, indent=2, separators=(',', ':')))

        return auth_json['access_token'], auth_json['refresh_token'], auth_json['expires_in']
    elif r.status_code == 301 or r.status_code == 302 or r.status_code == 307 or r.status_code == 308:
        # Perform URL redirect
        print('EDP-GW authentication HTTP code:', r.status_code, r.reason)
        new_host = r.headers['Location']
        if new_host is not None:
            print('Perform URL redirect to ', new_host)
            return get_sts_token(current_refresh_token, new_host)
        return None, None, None
    elif r.status_code == 400 or r.status_code == 401:
        # Retry with username and password
        print('EDP-GW authentication HTTP code:', r.status_code, r.reason)
        if current_refresh_token:
            # Refresh token may have expired. Try using our password.
            print('Retry with username and password')
            return get_sts_token(None)
        return None, None, None
    elif r.status_code == 403 or r.status_code == 451:
        # Stop retrying with the request
        print('EDP-GW authentication HTTP code:', r.status_code, r.reason)
        print('Stop retrying with the request')
        return None, None, None
    else:
        # Retry the request to the API gateway
        print('EDP-GW authentication HTTP code:', r.status_code, r.reason)
        print('Retry the request to the API gateway')
        return get_sts_token(current_refresh_token)

if __name__ == "__main__":
    # Get command line parameters
    try:
        opts, args = getopt.getopt(sys.argv[1:], "", ["help", "hostname=", "port=", "app_id=", "user=", "clientid=", "password=",
                                                      "position=", "auth_url=", "scope=", "ric=", "service=", "view=", "no_streaming"])
    except getopt.GetoptError:
        print('Usage: market_price_edpgw_authentication.py [--hostname hostname] [--port port] [--app_id app_id] '
              '[--user user] [--clientid clientid] [--password password] [--position position] [--auth_url auth_url] '
              '[--scope scope] [--ric ric] [--service service] [--view view] [--no_streaming] [--help]')
        sys.exit(2)
    for opt, arg in opts:
        if opt in "--help":
            print('Usage: market_price_edpgw_authentication.py [--hostname hostname] [--port port] [--app_id app_id] '
                  '[--user user] [--clientid clientid] [--password password] [--position position] [--auth_url auth_url] '
                  '[--scope scope] [--ric ric] [--service service] [--view view] [--no_streaming] [--help]')
            sys.exit(0)
        elif opt in "--hostname":
            hostname = arg
        elif opt in "--port":
            port = arg
        elif opt in "--app_id":
            app_id = arg
        elif opt in "--user":
            user = arg
        elif opt in "--clientid":
            clientid = arg
        elif opt in "--password":
            password = arg
        elif opt in "--position":
            position = arg
        elif opt in "--auth_url":
            auth_url = arg
        elif opt in "--scope":
            scope = arg
        elif opt in "--ric":
            ric = arg
        elif opt in "--service":
            service = arg
        elif opt in "--view":
            view = arg.split(',')
        elif opt in "--no_streaming":
            no_streaming = True

    if user == '' or password == '' or  hostname == '' or clientid == '':
        print("user, clientid, password, and hostname are required options")
        sys.exit(2)

    if position == '':
        # Populate position if possible
        try:
            position_host = socket.gethostname()
            position = socket.gethostbyname(position_host) + "/" + position_host
        except socket.gaierror:
            position = "127.0.0.1/net"

    sts_token, refresh_token, expire_time = get_sts_token(None)
    if not sts_token:
        sys.exit(1)

    # Start websocket handshake
    ws_address = "wss://{}:{}/WebSocket".format(hostname, port)
    print("Connecting to WebSocket " + ws_address + " ...")
    web_socket_app = websocket.WebSocketApp(ws_address, on_message=on_message,
                                            on_error=on_error,
                                            on_close=on_close,
                                            subprotocols=['tr_json2'])
    web_socket_app.on_open = on_open

    # Event loop
    wst = threading.Thread(target=web_socket_app.run_forever, kwargs={'sslopt': {'check_hostname': False}})
    wst.start()

    try:
        while True:
            # Give 30 seconds to obtain the new security token and send reissue
            if int(expire_time) > 30:
                time.sleep(int(expire_time) - 30)
            else:
                # Fail the refresh since value too small
                sys.exit(1)
            sts_token, refresh_token, expire_time = get_sts_token(refresh_token)
            if not sts_token:
                sys.exit(1)

            # Update token.
            if logged_in:
                send_login_request(sts_token, True)
    except KeyboardInterrupt:
        web_socket_app.close()
