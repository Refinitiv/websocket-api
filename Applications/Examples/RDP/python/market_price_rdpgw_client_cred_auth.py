#!/usr/bin/env python
# |-----------------------------------------------------------------------------
# |            This source code is provided under the Apache 2.0 license      --
# |  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
# |                See the project's LICENSE.md for details.                  --
# |            Copyright (C) 2018-2021 Refinitiv. All rights reserved.        --
# |-----------------------------------------------------------------------------

"""
  This example demonstrates authenticating via Refinitiv Data Platform (RDP), using an
  authentication token to discover Refinitiv Real-Time service endpoint or use specified
  endpoint (host and port), and using the endpoint and authentitcation to 
  retrieve market content. Specifically for oAuthClientCred authentication, this 
  application uses the client credentials grant type in the auth request 
  RDP (auth/oauth2/v2/token) using Refinitiv provided credentials: client id (username) 
  and client secret (password).
 
  This example can run with optional hotstandby support. Without this support, the application
  will use a load-balanced interface with two hosts behind the load balancer. With hot standly
  support, the application will access two hosts and display the data (should be identical) from
  each of the hosts.
 
  It performs the following steps:
  - Authenticating via HTTP Post request to Refinitiv Data Platform
  - Retrieving service endpoints from Service Discovery via HTTP Get request,
    using the token retrieved from Refinitiv Data Platform
  - Opening a WebSocket (or two, if the --hotstandby option is specified) to
    a Refinitiv Real-Time Service endpoint, as retrieved from Service Discovery
  - Sending Login into the Real-Time Service using the token retrieved
    from Refinitiv Data Platform.
  - Requesting market-price content.
  - Printing the response content.
  - Upon disconnect, re-request authentication token to reconnect to Refinitiv Data 
    Platform endpoint(s) if it is no longer valid.
"""

import sys
import time
import getopt
import requests
import logging
import socket
import json
import websocket
import threading
from datetime import datetime

# Global Default Variables
app_id = '256'
auth_token = ''
auth_url = 'https://api.refinitiv.com/auth/oauth2/v2/token'
clientid = ''
client_secret = ''
discovery_url = 'https://api.refinitiv.com/streaming/pricing/v1/'
hostName = ''
hostName2 = ''
hostList = []
backupHostList = []
hotstandby = False
port = 443
port2 = 443
position = ''
region = 'us-east-1'
ric = '/TRI.N'
scope = 'trapi.streaming.pricing.read'
service = 'ELEKTRON_DD'
session2 = None
curTS = 0
tokenTS = 0

class WebSocketSession:
    session_name = ''
    web_socket_app = None
    web_socket_open = False
    host = ''
    disconnected_by_user = False
    reconnecting = True

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
        }
        self.web_socket_app.send(json.dumps(mp_req_json))
        print(str(datetime.now()) + " SENT on " + self.session_name + ":")
        print(json.dumps(mp_req_json, sort_keys=True, indent=2, separators=(',', ':')))

    def _send_login_request(self, authn_token):
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
        login_json['Key']['Elements']['AuthenticationToken'] = authn_token

        self.web_socket_app.send(json.dumps(login_json))
        print(str(datetime.now()) + " SENT on " + self.session_name + ":")
        print(json.dumps(login_json, sort_keys=True, indent=2, separators=(',', ':')))

    def _process_login_response(self, message_json):
        """ Send item request upon login success """
        if message_json['State']['Stream'] != "Open" or message_json['State']['Data'] != "Ok":
            print("Login failed.")
            sys.exit(1)

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
            print(str(datetime.now()) + " SENT on " + self.session_name + ":")
            print(json.dumps(pong_json, sort_keys=True, indent=2, separators=(',', ':')))

    # Callback events from WebSocketApp
    def _on_message(self, ws, message):
        """ Called when message received, parse message into JSON for processing """
        print(str(datetime.now()) + " RECEIVED on " + self.session_name + ":")
        message_json = json.loads(message)
        print(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))

        for singleMsg in message_json:
            self._process_message(singleMsg)

    def _on_error(self, ws, error):
        """ Called when websocket error has occurred """
        print(str(datetime.now()) + " Session: " + str(self.session_name) + ";  Error: "+ str(error))

    def _on_close(self, ws, close_status_code, close_message):
        """ Called when websocket is closed """
        self.web_socket_open = False
        print(str(datetime.now()) + " WebSocket Closed for " + self.session_name)

    def _on_open(self, ws):
        """ Called when handshake is complete and websocket is open, send login """

        print(str(datetime.now()) + " WebSocket successfully connected for " + self.session_name + "!")
        self.web_socket_open = True
        self.reconnecting = False
        self._send_login_request(auth_token)

    # Operations
    def connect(self):
        # Start websocket handshake
        ws_address = "wss://{}/WebSocket".format(self.host)
        print(str(datetime.now()) + " Connecting to WebSocket " + ws_address + " for " + self.session_name + "...")
        #websocket.enableTrace(True)
        self.web_socket_app = websocket.WebSocketApp(ws_address, 
                                                     on_message=self._on_message,
                                                     on_error=self._on_error,
                                                     on_close=self._on_close,
                                                     on_open=self._on_open,
                                                     subprotocols=['tr_json2'])
        # Event loop
        wst = threading.Thread(target=self.web_socket_app.run_forever, kwargs={'sslopt': {'check_hostname': False}})
        wst.daemon = True
        wst.start()


    def disconnect(self):
        print(str(datetime.now()) + " Closing the WebSocket connection for " + self.session_name)
        self.disconnected_by_user = True
        if self.web_socket_open:
            self.web_socket_app.close()


def query_service_discovery(url=None):

    if url is None:
        url = discovery_url

    print("\n" + str(datetime.now()) + \
            " Sending Refinitiv Data Platform service discovery request to ", url, "...\n" )

    try:
        r = requests.get(url, headers={"Authorization": "Bearer " + auth_token}, params={"transport": "websocket"}, allow_redirects=False)

    except requests.exceptions.RequestException as e:
        print('Refinitiv Data Platform service discovery exception failure:', e)
        return False

    if r.status_code == 200:
        # Authentication was successful. Deserialize the response.
        response_json = r.json()
        print(str(datetime.now()) + " Refinitiv Data Platform Service discovery succeeded." + \
                " RECEIVED:")
        print(json.dumps(response_json, sort_keys=True, indent=2, separators=(',', ':')))
        
        for index in range(len(response_json['services'])):
            if not response_json['services'][index]['location'][0].startswith(region):
                continue

            if not hotstandby:
                if len(response_json['services'][index]['location']) >= 2:
                    hostList.append(response_json['services'][index]['endpoint'] + ":" +
                                    str(response_json['services'][index]['port']))
                    break
                if len(response_json['services'][index]['location']) == 1:
                    backupHostList.append(response_json['services'][index]['endpoint'] + ":" +
                                    str(response_json['services'][index]['port']))
                    break
            else:
                if len(response_json['services'][index]['location']) == 1:
                    hostList.append(response_json['services'][index]['endpoint'] + ":" +
                                    str(response_json['services'][index]['port']))

        if hotstandby:
            if len(hostList) < 2:
                print("Expected 2 hosts but received:", len(hostList), "or the region:", region, "is not present in list of endpoints")
                sys.exit(1)
        else:
            if len(hostList) == 0:
                if len(backupHostList) > 0:
                    for hostIndex in range(len(backupHostList)):
                        hostList.append(backupHostList[hostIndex])

        if len(hostList) == 0:
            print("The region:", region, "is not present in list of endpoints")
            sys.exit(1)

        return True

    elif r.status_code in [ 301, 302, 307, 308 ]:
        # Perform URL redirect
        print('Refinitiv Data Platform service discovery HTTP code:', r.status_code, r.reason)
        new_host = r.headers['Location']
        if new_host != None:
            print('Perform URL redirect to ', new_host)
            return query_service_discovery(new_host)
        return False
    elif r.status_code in [ 403, 404, 410, 451 ]:
        # Stop trying the request
        print('Refinitiv Data Platform service discovery HTTP code:', r.status_code, r.reason)
        print('Unrecoverable error when performing service discovery: stopped retrying request')
        return False
    else:
        # Retry request with an appropriate delay: 
        print('Refinitiv Data Platform service discovery HTTP code:', r.status_code, r.reason)
        time.sleep(5)
        # CAUTION: This is sample code with infinite retries.
        print('Retrying the service discovery request')
        return query_service_discovery()


def get_auth_token(url=None):
    """
        Retrieves an authentication token.
    """

    if url is None:
        url = auth_url

    data = {'grant_type': 'client_credentials', 'scope': scope, 'client_id': clientid, 'client_secret': client_secret}

    print("\n" + str(datetime.now()) + \
            " Sending authentication request with client credentials to ", url, "...\n")
    try:
        # Request with auth for https protocol    
        r = requests.post(url,
                headers={'Accept' : 'application/json'},
                          data=data,
                          verify=True,
                          allow_redirects=False)

    except requests.exceptions.RequestException as e:
        print('Refinitiv Data Platform authentication exception failure:', e)
        return None, None

    if r.status_code == 200:
        auth_json = r.json()
        print(str(datetime.now()) + " Refinitiv Data Platform Authentication succeeded. RECEIVED:")
        print(json.dumps(auth_json, sort_keys=True, indent=2, separators=(',', ':')))
        return auth_json['access_token'], auth_json['expires_in']
    elif r.status_code in [ 301, 302, 307, 308 ]:
        # Perform URL redirect
        print('Refinitiv Data Platform authentication HTTP code:', r.status_code, r.reason)
        new_host = r.headers['Location']
        if new_host != None:
            print('Perform URL redirect to ', new_host)
            return get_auth_token(new_host)
        return None, None
    elif r.status_code in [ 400, 401, 403, 404, 410, 451 ]:
        # Stop trying the request
        # NOTE: With 400 and 401, there is not retry to keep this sample code simple
        print('Refinitiv Data Platform authentication HTTP code:', r.status_code, r.reason)
        print('Unrecoverable error: stopped retrying request')
        return None, None
    else:
        print('Refinitiv Data Platform authentication failed. HTTP code:', r.status_code, r.reason)
        time.sleep(5)
        # CAUTION: This is sample code with infinite retries.
        print('Retrying auth request')
        return get_auth_token()


def print_commandline_usage_and_exit(exit_code):
    print('Usage: market_price_rdpgw_client_cred_auth.py [--app_id app_id] '
          '--clientid clientid --clientsecret client secret [--position position] [--auth_url auth_url] '
          '[--hostname hostname] [--port port] [--standbyhostname hostname] [--standbyport port] ' 
          '[--discovery_url discovery_url] [--scope scope] [--service service]'
          '[--region region] [--ric ric] [--hotstandby] [--help]')
    sys.exit(exit_code)


if __name__ == "__main__":
    # Get command line parameters
    opts = []
    try:
        opts, args = getopt.getopt(sys.argv[1:], "", [
            "help", "app_id=", "clientsecret=", "clientid=", 
            "hostname=", "port=", "standbyhostname=", "standbyport=", 
            "position=", "auth_url=", "discovery_url=", 
            "scope=", "service=", "region=", "ric=", "hotstandby"])
    except getopt.GetoptError:
        print_commandline_usage_and_exit(2)
    for opt, arg in opts:
        if opt in "--help":
            print_commandline_usage_and_exit(0)
        elif opt in "--app_id":
            app_id = arg
        elif opt in "--clientsecret":
            client_secret = arg
        elif opt in "--clientid":
            clientid = arg
        elif opt in "--hostname":
            hostName = arg
        elif opt in "--standbyhostname":
            hostName2= arg
        elif opt in "--port":
            port = arg
        elif opt in "--standbyport":
            port2= arg
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
        elif opt in "--ric":
            ric = arg
        elif opt in "--hotstandby":
                hotstandby = True

    if clientid == '' or client_secret == '':
        print("clientid and clientsecret are required options")
        sys.exit(2)
        
    if position == '':
        # Populate position if possible
        try:
            position_host = socket.gethostname()
            position = socket.gethostbyname(position_host) + "/" + position_host
        except socket.gaierror:
            position = "127.0.0.1/net"

    auth_token, expire_time = get_auth_token()
    if not auth_token:
        print("Failed initial authentication with Refinitiv Data Platform. Exiting...")
        sys.exit(1)

    tokenTS = time.time()

    # If hostname is specified, use it for the connection
    if hostName != '':
        hostList.append(hostName + ':' + str(port))
        if hostName2 != '':
            hostList.append(hostName2 + ':' + str(port2))
    else:
        # Query VIPs from Refinitiv Data Platform service discovery if user did not specify hostname
        if not query_service_discovery():
            print("Failed to retrieve endpoints from Refinitiv Data Platform Service Discovery. Exiting...")
            sys.exit(1)

    # Start websocket handshake; create two sessions when the hotstandby parameter is specified.
    session1 = WebSocketSession("session1", hostList[0])
    session1.connect()

    if hotstandby and len(hostList) > 1:
        session2 = WebSocketSession("session2", hostList[1])
        session2.connect()

    try:
        while True:
            # NOTE about connection recovery: When connecting or reconnecting 
            #   to the server, a valid token must be used. Upon being disconnecting, initial 
            #   reconnect attempt must be done with  a new token.
            #   If a successful reconnect takes longer than token expiration time, 
            #   a new token must be obtained proactively. 

            # Waiting a few seconds before checking for connection down and attempting reconnect
            time.sleep(4)
            if not session1.web_socket_open or ( session2 and not session2.web_socket_open ) :
                if session1.reconnecting or ( session2 and session2.reconnecting ) :
                    curTS = time.time()
                    if (int(expire_time) < 600):
                        deltaTime = float(expire_time) * 0.90
                    else:
                        deltaTime = 300
                    if (int(curTS) >= int(float(tokenTS) + float(deltaTime))):
                        auth_token, expire_time = get_auth_token() 
                        tokenTS = time.time()
                else:
                    auth_token, expire_time = get_auth_token() 
                    tokenTS = time.time()
                if auth_token is not None:
                    if not session1.disconnected_by_user:
                        session1.reconnecting = True
                        session1.connect()
                    if session2 and not session2.disconnected_by_user:
                        session2.reconnecting = True
                        session2.connect()
                else:
                    print("Failed authentication with Refinitiv Data Platform. Exiting...")
                    sys.exit(1) 


    except KeyboardInterrupt:
        session1.disconnect()
        if hotstandby:
            session2.disconnect()
