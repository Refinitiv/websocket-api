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
auth_url = 'https://api.refinitiv.com:443/auth/oauth2/beta1/token'
discovery_url = 'https://api.refinitiv.com/streaming/pricing/v1/'
password = ''
position = ''
sts_token = ''
refresh_token = ''
user = ''
clientid = ''
client_secret = ''
scope = 'trapi'
region = 'amer'
ric = '/TRI.N'
service = 'ELEKTRON_DD'
hostList = []
hotstandby = False
view = []
no_streaming = False
# Global Variables
session2 = None


class WebSocketSession:
    logged_in = False
    session_name = ''
    web_socket_app = None
    web_socket_open = False
    host = ''
    disconnected_by_user = False

    def __init__(self, name, host):
        self.session_name = name
        self.host = host

    def _send_market_price_request(self, ric_name):
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

        self.web_socket_app.send(json.dumps(mp_req_json))
        print("SENT on " + self.session_name + ":")
        print(json.dumps(mp_req_json, sort_keys=True, indent=2, separators=(',', ':')))

    def _send_login_request(self, auth_token, is_refresh_token):
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

        self.web_socket_app.send(json.dumps(login_json))
        print("SENT on " + self.session_name + ":")
        print(json.dumps(login_json, sort_keys=True, indent=2, separators=(',', ':')))

    def _process_login_response(self, message_json):
        """ Send item request """
        if message_json['State']['Stream'] != "Open" or message_json['State']['Data'] != "Ok":
            print("Login failed.")
            sys.exit(1)

        self.logged_in = True
        self._send_market_price_request(ric)

    def _process_message(self, message_json):
        """ Parse at high level and output JSON of message """
        message_type = message_json['Type']

        if message_type == "Refresh":
            if 'Domain' in message_json:
                message_domain = message_json['Domain']
                if message_domain == "Login":
                    self._process_login_response(message_json)
        elif message_type == "Ping":
            pong_json = {'Type': 'Pong'}
            self.web_socket_app.send(json.dumps(pong_json))
            print("SENT on " + self.session_name + ":")
            print(json.dumps(pong_json, sort_keys=True, indent=2, separators=(',', ':')))

    # Callback events from WebSocketApp
    def _on_message(self, message):
        """ Called when message received, parse message into JSON for processing """
        print("RECEIVED on " + self.session_name + ":")
        message_json = json.loads(message)
        print(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))

        for singleMsg in message_json:
            self._process_message(singleMsg)

    def _on_error(self, error):
        """ Called when websocket error has occurred """
        print(error + " for " + self.session_name)

    def _on_close(self):
        """ Called when websocket is closed """
        self.web_socket_open = False
        self.logged_in = False
        print("WebSocket Closed for " + self.session_name)

        if not self.disconnected_by_user:
            print("Reconnect to the endpoint for " + self.session_name + " after 3 seconds... ")
            time.sleep(3)
            self.connect()

    def _on_open(self):
        """ Called when handshake is complete and websocket is open, send login """

        print("WebSocket successfully connected for " + self.session_name + "!")
        self.web_socket_open = True
        self._send_login_request(sts_token, False)

    # Operations
    def connect(self):
        # Start websocket handshake
        ws_address = "wss://{}/WebSocket".format(self.host)
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
            self._send_login_request(sts_token, True)


def query_service_discovery(url=None):

    if url is None:
        url = discovery_url

    print("Sending EDP-GW service discovery request to " + url)

    try:
        r = requests.get(url, headers={"Authorization": "Bearer " + sts_token}, params={"transport": "websocket"}, allow_redirects=False)

    except requests.exceptions.RequestException as e:
        print('EDP-GW service discovery exception failure:', e)
        return False

    if r.status_code == 200:
        # Authentication was successful. Deserialize the response.
        response_json = r.json()
        print("EDP-GW Service discovery succeeded. RECEIVED:")
        print(json.dumps(response_json, sort_keys=True, indent=2, separators=(',', ':')))

        for index in range(len(response_json['services'])):

            if region == "amer":
                if not response_json['services'][index]['location'][0].startswith("us-"):
                    continue
            elif region == "emea":
                if not response_json['services'][index]['location'][0].startswith("eu-"):
                    continue
            elif region == "apac":
                if not response_json['services'][index]['location'][0].startswith("ap-"):
                    continue

            if not hotstandby:
                if len(response_json['services'][index]['location']) == 2:
                    hostList.append(response_json['services'][index]['endpoint'] + ":" +
                                    str(response_json['services'][index]['port']))
                    break
            else:
                if len(response_json['services'][index]['location']) == 1:
                    hostList.append(response_json['services'][index]['endpoint'] + ":" +
                                    str(response_json['services'][index]['port']))

        if hotstandby:
            if len(hostList) < 2:
                print("hotstandby support requires at least two hosts")
                sys.exit(1)
        else:
            if len(hostList) == 0:
                print("No host found from EDP service discovery")
                sys.exit(1)

        return True

    elif r.status_code == 301 or r.status_code == 302 or r.status_code == 303 or r.status_code == 307 or r.status_code == 308:
        # Perform URL redirect
        print('EDP-GW service discovery HTTP code:', r.status_code, r.reason)
        new_host = r.headers['Location']
        if new_host is not None:
            print('Perform URL redirect to ', new_host)
            return query_service_discovery(new_host)
        return False
    elif r.status_code == 403 or r.status_code == 451:
        # Stop trying with the request
        print('EDP-GW service discovery HTTP code:', r.status_code, r.reason)
        print('Stop trying with the request')
        return False
    else:
        # Retry the service discovery request
        print('EDP-GW service discovery HTTP code:', r.status_code, r.reason)
        print('Retry the service discovery request')
        return query_service_discovery()


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


def print_commandline_usage_and_exit(exit_code):
    print('Usage: market_price_edpgw_service_discovery.py [--app_id app_id] '
          '[--user user] [--clientid clientid] [--password password] [--position position] [--auth_url auth_url] '
          '[--discovery_url discovery_url] [--scope scope] [--service service] [--region region] [--ric ric] [--hotstandby] '
          '[--view view] [--no_streaming] [--help]')
    sys.exit(exit_code)


if __name__ == "__main__":
    # Get command line parameters
    opts = []
    try:
        opts, args = getopt.getopt(sys.argv[1:], "", ["help", "app_id=", "user=", "clientid=", "password=",
                                                      "position=", "auth_url=", "discovery_url=", "scope=", "service=", "region=", "ric=",
                                                      "hotstandby", "view=", "no_streaming"])
    except getopt.GetoptError:
        print_commandline_usage_and_exit(2)
    for opt, arg in opts:
        if opt in "--help":
            print_commandline_usage_and_exit(0)
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
        elif opt in "--discovery_url":
            discovery_url = arg
        elif opt in "--scope":
            scope = arg
        elif opt in "--service":
            service = arg
        elif opt in "--region":
            region = arg
            if region != "amer" and region != "emea" and region != 'apac':
                print("Unknown region \"" + region + "\". The region must be either \"amer\", \"emea\", or \"apac\".")
                sys.exit(1)
        elif opt in "--ric":
            ric = arg.split(',')
        elif opt in "--hotstandby":
            hotstandby = True
        elif opt in "--view":
            view = arg.split(',')
        elif opt in "--no_streaming":
            no_streaming = True

    if user == '' or password == '' or clientid == '':
        print("user, clientid and password are required options")
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

    # Query VIPs from EDP service discovery
    if not query_service_discovery():
        print("Failed to retrieve endpoints from EDP Service Discovery. Exiting...")
        sys.exit(1)

    # Start websocket handshake; create two sessions when the hotstandby parameter is specified.
    session1 = WebSocketSession("session1", hostList[0])
    session1.connect()

    if hotstandby:
        session2 = WebSocketSession("session2", hostList[1])
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
