#!/usr/bin/env python
# |-----------------------------------------------------------------------------
# |            This source code is provided under the Apache 2.0 license      --
# |  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
# |                See the project's LICENSE.md for details.                  --
# |           Copyright Thomson Reuters 2018. All rights reserved.            --
# |-----------------------------------------------------------------------------
"""
Simple example of authenticating to EDP-GW and using the token to query VIPs
from EDP service discovery, login to the Elektron Real-Time Service, and
retrieve MarketPrice content. A username and password are used to
retrieve this token.
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
auth_hostname = 'api.edp.thomsonreuters.com'
auth_port = '443'
auth_path = 'auth/oauth2/beta1/token'
discovery_path = 'streaming/pricing/v1/'
password = ''
position = ''
sts_token = ''
refresh_token = ''
user = ''
client_secret = ''
scope = 'trapi'
ric = '/TRI.N'
hostList = ['127.0.0.1', '127.0.0.1']
portList = ['443', '443']
hotstandby = False
# Global Variables
session2 = None


class WebSocketSession:
    logged_in = False
    session_name = ''
    web_socket_app = None
    web_socket_open = False
    host = ''
    port = ''
    disconnected_by_user = False

    def __init__(self, name, host, port):
        self.session_name = name
        self.host = host
        self.port = port

    def _send_market_price_request(self, ws, ric_name):
        """ Create and send simple Market Price request """
        mp_req_json = {
            'ID': 2,
            'Key': {
                'Name': ric_name,
            },
        }
        ws.send(json.dumps(mp_req_json))
        print("SENT on " + self.session_name + ":")
        print(json.dumps(mp_req_json, sort_keys=True, indent=2, separators=(',', ':')))

    def _send_login_request(self, ws, auth_token, is_refresh_token):
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

        ws.send(json.dumps(login_json))
        print("SENT on " + self.session_name + ":")
        print(json.dumps(login_json, sort_keys=True, indent=2, separators=(',', ':')))

    def _process_login_response(self, ws, message_json):
        """ Send item request """
        if message_json['State']['Stream'] != "Open" or message_json['State']['Data'] != "Ok":
            print("Login failed.")
            sys.exit(1)

        self.logged_in = True
        self._send_market_price_request(ws, ric)

    def _process_message(self, ws, message_json):
        """ Parse at high level and output JSON of message """
        message_type = message_json['Type']

        if message_type == "Refresh":
            if 'Domain' in message_json:
                message_domain = message_json['Domain']
                if message_domain == "Login":
                    self._process_login_response(ws, message_json)
        elif message_type == "Ping":
            pong_json = {'Type': 'Pong'}
            ws.send(json.dumps(pong_json))
            print("SENT on " + self.session_name + ":")
            print(json.dumps(pong_json, sort_keys=True, indent=2, separators=(',', ':')))

    # Callback events from WebSocketApp
    def _on_message(self, ws, message):
        """ Called when message received, parse message into JSON for processing """
        print("RECEIVED on " + self.session_name + ":")
        message_json = json.loads(message)
        print(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))

        for singleMsg in message_json:
            self._process_message(ws, singleMsg)

    def _on_error(self, error):
        """ Called when websocket error has occurred """
        print(error + " for " + self.session_name)

    def _on_close(self, _):
        """ Called when websocket is closed """
        self.web_socket_open = False
        self.logged_in = False
        print("WebSocket Closed for " + self.session_name)

        if not self.disconnected_by_user:
            print("Reconnect to the endpoint for " + self.session_name + " after 3 seconds... ")
            time.sleep(3)
            self.connect()

    def _on_open(self, ws):
        """ Called when handshake is complete and websocket is open, send login """

        print("WebSocket successfully connected for " + self.session_name + "!")
        self.web_socket_open = True
        self._send_login_request(ws, sts_token, False)

    # Operations
    def connect(self):
        # Start websocket handshake
        ws_address = "wss://{}:{}/WebSocket".format(self.host, self.port)
        print("Connecting to WebSocket " + ws_address + " for " + self.session_name + "...")
        self.web_socket_app = websocket.WebSocketApp(ws_address, on_message=self._on_message,
                                                     on_error=self._on_error,
                                                     on_close=self._on_close,
                                                     subprotocols=['tr_json2'])
        self.web_socket_app.on_open = self._on_open

        # Event loop
        wst = threading.Thread(target=self.web_socket_app.run_forever, kwargs={'sslopt': {'check_hostname': False}})
        wst.start()

    def disconnect(self):
        print("Closing the WebSocket connection for " + self.session_name)
        self.disconnected_by_user = True
        if self.web_socket_open:
            self.web_socket_app.close()

    def refresh_token(self):
        if self.logged_in:
            print("Refreshing the access token for " + self.session_name)
            self._send_login_request(self.web_socket_app, sts_token, True)


def query_service_discovery():

    url = 'https://{}/{}'.format(auth_hostname, discovery_path)
    print("Sending EDP-GW service discovery request to " + url)

    try:
        r = requests.get(url, headers={"Authorization": "Bearer " + sts_token}, params={"transport": "websocket"})

    except requests.exceptions.RequestException as e:
        print('EDP-GW service discovery exception failure:', e)
        return False

    if r.status_code != 200:
        print('EDP-GW service discovery result failure:', r.status_code, r.reason)
        print('Text:', r.text)
        return False

    response_json = r.json()
    print("EDP-GW Service discovery succeeded. RECEIVED:")
    print(json.dumps(response_json, sort_keys=True, indent=2, separators=(',', ':')))

    count = 0
    for index in range(len(response_json['services'])):
        if not hotstandby:
            if len(response_json['services'][index]['location']) == 2:
                hostList[count] = response_json['services'][index]['endpoint']
                portList[count] = response_json['services'][index]['port']
                break
        else:
            if len(response_json['services'][index]['location']) == 1:
                hostList[count] = response_json['services'][index]['endpoint']
                portList[count] = response_json['services'][index]['port']
                count = count + 1
    return True


def get_sts_token(current_refresh_token):
    """
        Retrieves an authentication token.
        :param current_refresh_token: Refresh token retrieved from a previous authentication, used to retrieve a
        subsequent access token. If not provided (i.e. on the initial authentication), the password is used.
    """
    url = 'https://{}:{}/{}'.format(auth_hostname, auth_port, auth_path)

    if not current_refresh_token:  # First time through, send password
        data = {'username': user, 'password': password, 'grant_type': 'password', 'takeExclusiveSignOnControl': True,
                'scope': scope}
        print("Sending authentication request with password to ", url, "...")
    else:  # Use the given refresh token
        data = {'username': user, 'refresh_token': current_refresh_token, 'grant_type': 'refresh_token',
                'takeExclusiveSignOnControl': True}
        print("Sending authentication request with refresh token to ", url, "...")

    try:
        r = requests.post(url,
                          headers={'Accept': 'application/json'},
                          data=data,
                          auth=(user, client_secret),
                          verify=True)

    except requests.exceptions.RequestException as e:
        print('EDP-GW authentication exception failure:', e)
        return None, None, None

    if r.status_code != 200:
        print('EDP-GW authentication result failure:', r.status_code, r.reason)
        print('Text:', r.text)
        if r.status_code == 401 and current_refresh_token:
            # Refresh token may have expired. Try using our password.
            return get_sts_token(None)
        return None, None, None

    auth_json = r.json()
    print("EDP-GW Authentication succeeded. RECEIVED:")
    print(json.dumps(auth_json, sort_keys=True, indent=2, separators=(',', ':')))

    return auth_json['access_token'], auth_json['refresh_token'], auth_json['expires_in']


if __name__ == "__main__":
    # Get command line parameters
    try:
        opts, args = getopt.getopt(sys.argv[1:], "", ["help", "app_id=", "user=", "password=",
                                                      "position=", "auth_hostname=", "auth_port=", "scope=",
                                                      "ric=", "hotstandby"])
    except getopt.GetoptError:
        print('Usage: market_price_edpgw_service_discovery.py [--app_id app_id] '
              '[--user user] [--password password] [--position position] [--auth_hostname auth_hostname] '
              '[--auth_port auth_port] [--scope scope] [--ric ric] [--hotstandby]'
              ' [--help]')
        sys.exit(2)
    for opt, arg in opts:
        if opt in "--help":
            print('Usage: market_price_edpgw_service_discovery.py [--app_id app_id] '
                  '[--user user] [--password password] [--position position] [--auth_hostname auth_hostname] '
                  '[--auth_port auth_port] [--scope scope] [--ric ric] [--hotstandby] [--help]')
            sys.exit(0)
        elif opt in "--app_id":
            app_id = arg
        elif opt in "--user":
            user = arg
        elif opt in "--password":
            password = arg
        elif opt in "--position":
            position = arg
        elif opt in "--auth_hostname":
            auth_hostname = arg
        elif opt in "--auth_port":
            auth_port = arg
        elif opt in "--scope":
            scope = arg
        elif opt in "--ric":
            ric = arg
        elif opt in "--hotstandby":
                hotstandby = True

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

    # Query VIPs from EDP service discovery
    if not query_service_discovery():
        print("Failed to retrieve endpoints from EDP Service Discovery. Exiting...")
        sys.exit(1)

    # Start websocket handshake; create two sessions when the hotstandby parameter is specified.
    session1 = WebSocketSession("session1", hostList[0], portList[0])
    session1.connect()

    if hotstandby:
        session2 = WebSocketSession("session2", hostList[1], portList[1])
        session2.connect()

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
            session1.refresh_token()
            if hotstandby:
                session2.refresh_token()

    except KeyboardInterrupt:
        session1.disconnect()
        if hotstandby:
            session2.disconnect()
