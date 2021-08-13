#!/usr/bin/env python
# |-----------------------------------------------------------------------------
# |            This source code is provided under the Apache 2.0 license      --
# |  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
# |                See the project's LICENSE.md for details.                  --
# |            Copyright (C) 2018-2021 Refinitiv. All rights reserved.        --
# |-----------------------------------------------------------------------------

"""
  This example demonstrates authenticating via Refinitiv Data Platform, using an
  authentication token to discover Refinitiv Real-Time service endpoint, and
  using the endpoint and authentitcation to retrieve market content.
 
  This example maintains a session by proactively renewing the authentication
  token before expiration.
 
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
  - Periodically proactively re-authenticating to Refinitiv Data Platform, and
    providing the updated token to the Real-Time endpoint before token expiration.
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
auth_url = 'https://api.refinitiv.com:443/auth/oauth2/v1/token'
discovery_url = 'https://api.refinitiv.com/streaming/pricing/v1/'
password = ''
newPassword = ''
position = ''
sts_token = ''
refresh_token = ''
user = ''
clientid = ''
client_secret = ''
scope = 'trapi.streaming.pricing.read'
region = 'us-east-1'
ric = '/TRI.N'
service = 'ELEKTRON_DD'
hostList = []
hotstandby = False
# Global Variables
session2 = None

original_expire_time = '0'; 

# Global Variables for Password Policy Description
PASSWORD_LENGTH_MASK                = 0x1;
PASSWORD_UPPERCASE_LETTER_MASK      = 0x2;
PASSWORD_LOWERCASE_LETTER_MASK      = 0x4;
PASSWORD_DIGIT_MASK                 = 0x8;
PASSWORD_SPECIAL_CHARACTER_MASK     = 0x10;
PASSWORD_INVALID_CHARACTER_MASK     = 0x20;

PASSWORD_LENGTH_MIN                 = 30;
PASSWORD_UPPERCASE_LETTER_MIN       = 1;
PASSWORD_LOWERCASE_LETTER_MIN       = 1;
PASSWORD_DIGIT_MIN                  = 1;
PASSWORD_SPECIAL_CHARACTER_MIN      = 1;
PASSWORD_SPECIAL_CHARACTER_SET      = "~!@#$%^&*()-_=+[]{}|;:,.<>/?";
PASSWORD_MIN_NUMBER_OF_CATEGORIES   = 3;

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
        }
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
    def _on_message(self, ws, message):
        """ Called when message received, parse message into JSON for processing """
        print("RECEIVED on " + self.session_name + ":")
        message_json = json.loads(message)
        print(json.dumps(message_json, sort_keys=True, indent=2, separators=(',', ':')))

        for singleMsg in message_json:
            self._process_message(singleMsg)

    def _on_error(self, ws, error):
        """ Called when websocket error has occurred """
        print(error + " for " + self.session_name)

    def _on_close(self, ws):
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
        self._send_login_request(sts_token, False)

    # Operations
    def connect(self):
        # Start websocket handshake
        ws_address = "wss://{}/WebSocket".format(self.host)
        print("Connecting to WebSocket " + ws_address + " for " + self.session_name + "...")
        self.web_socket_app = websocket.WebSocketApp(ws_address, 
                                                     on_message=self._on_message,
                                                     on_error=self._on_error,
                                                     on_close=self._on_close,
                                                     on_open=self._on_open,
                                                     subprotocols=['tr_json2'])

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

    print("Sending Refinitiv Data Platform service discovery request to " + url)

    try:
        r = requests.get(url, headers={"Authorization": "Bearer " + sts_token}, params={"transport": "websocket"}, allow_redirects=False)

    except requests.exceptions.RequestException as e:
        print('Refinitiv Data Platform service discovery exception failure:', e)
        return False

    if r.status_code == 200:
        # Authentication was successful. Deserialize the response.
        response_json = r.json()
        print("Refinitiv Data Platform Service discovery succeeded. RECEIVED:")
        print(json.dumps(response_json, sort_keys=True, indent=2, separators=(',', ':')))

        for index in range(len(response_json['services'])):
            if not response_json['services'][index]['location'][0].startswith(region):
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
                print("Expected 2 hosts but received:", len(hostList), "or the region:", region, "is not present in list of endpoints")
                sys.exit(1)
        else:
            if len(hostList) == 0:
                print("The region:", region, "is not present in list of endpoints")
                sys.exit(1)

        return True

    elif r.status_code == 301 or r.status_code == 302 or r.status_code == 303 or r.status_code == 307 or r.status_code == 308:
        # Perform URL redirect
        print('Refinitiv Data Platform service discovery HTTP code:', r.status_code, r.reason)
        new_host = r.headers['Location']
        if new_host is not None:
            print('Perform URL redirect to ', new_host)
            return query_service_discovery(new_host)
        return False
    elif r.status_code == 403 or r.status_code == 451:
        # Stop trying with the request
        print('Refinitiv Data Platform service discovery HTTP code:', r.status_code, r.reason)
        print('Stop trying with the request')
        return False
    else:
        # Retry the service discovery request
        print('Refinitiv Data Platform service discovery HTTP code:', r.status_code, r.reason)
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
        data = {'username': user, 'password': password, 'client_id': clientid, 'grant_type': 'password', 'takeExclusiveSignOnControl': True,
                'scope': scope}
        print("Sending authentication request with password to", url, "...")
    else:  # Use the given refresh token
        data = {'username': user, 'client_id': clientid, 'refresh_token': current_refresh_token, 'grant_type': 'refresh_token'}
        print("Sending authentication request with refresh token to", url, "...")
    if client_secret != '':
        data['client_secret'] = client_secret;
        
    try:
        # Request with auth for https protocol    
        r = requests.post(url,
                          headers={'Accept': 'application/json'},
                          data=data,
                          auth=(clientid, client_secret),
                          verify=True,
                          allow_redirects=False)

    except requests.exceptions.RequestException as e:
        print('Refinitiv Data Platform authentication exception failure:', e)
        return None, None, None

    if r.status_code == 200:
        auth_json = r.json()
        print("Refinitiv Data Platform Authentication succeeded. RECEIVED:")
        print(json.dumps(auth_json, sort_keys=True, indent=2, separators=(',', ':')))

        return auth_json['access_token'], auth_json['refresh_token'], auth_json['expires_in']
    elif r.status_code == 301 or r.status_code == 302 or r.status_code == 307 or r.status_code == 308:
        # Perform URL redirect
        print('Refinitiv Data Platform authentication HTTP code:', r.status_code, r.reason)
        new_host = r.headers['Location']
        if new_host is not None:
            print('Perform URL redirect to ', new_host)
            return get_sts_token(current_refresh_token, new_host)
        return None, None, None
    elif r.status_code == 400 or r.status_code == 401:
        # Retry with username and password
        print('Refinitiv Data Platform authentication HTTP code:', r.status_code, r.reason)
        if current_refresh_token:
            # Refresh token may have expired. Try using our password.
            print('Retry with username and password')
            return get_sts_token(None)
        return None, None, None
    elif r.status_code == 403 or r.status_code == 451:
        # Stop retrying with the request
        print('Refinitiv Data Platform authentication HTTP code:', r.status_code, r.reason)
        print('Stop retrying with the request')
        return None, None, None
    else:
        # Retry the request to Refinitiv Data Platform 
        print('Refinitiv Data Platform authentication HTTP code:', r.status_code, r.reason)
        print('Retry the request to Refinitiv Data Platform')
        return get_sts_token(current_refresh_token)


def print_commandline_usage_and_exit(exit_code):
    print('Usage: market_price_rdpgw_service_discovery.py [--app_id app_id] '
          '[--user user] [--clientid clientid] [--password password] [--newPassword new_password] [--position position] [--auth_url auth_url] '
          '[--discovery_url discovery_url] [--scope scope] [--service service] [--region region] [--ric ric] [--hotstandby] [--help]')
    sys.exit(exit_code)

def check_new_password(pwd):
    result = 0;

    countUpper = 0;
    countLower = 0;
    countDigit = 0;
    countSpecial = 0;

    if len(pwd) < PASSWORD_LENGTH_MIN :
        result |= PASSWORD_LENGTH_MASK;
    
    for c in pwd :
        # This long condition is used in order not to import re library
        # If re will be imported for some other purpose this condition should be
        # refactored using regular expression
        if not ((c >= 'A' and c <= 'Z') or (c >= 'a' and c <= 'z') \
              or (c >= '0' and c <= '9') or (c in  PASSWORD_SPECIAL_CHARACTER_SET)) :
            result |= PASSWORD_INVALID_CHARACTER_MASK;
        
        if (c >= 'A' and c <= 'Z') :
           countUpper += 1;
        if (c >= 'a' and c <= 'z') :
           countLower += 1;
        if (c >= '0' and c <= '9') :
            countDigit += 1;
        if (c in  PASSWORD_SPECIAL_CHARACTER_SET) :
            countSpecial += 1;

    if (countUpper < PASSWORD_UPPERCASE_LETTER_MIN) :        
        result |= PASSWORD_UPPERCASE_LETTER_MASK;
    if (countLower < PASSWORD_LOWERCASE_LETTER_MIN) : 
        result |= PASSWORD_LOWERCASE_LETTER_MASK;
    if (countDigit < PASSWORD_DIGIT_MIN) :
        result |= PASSWORD_DIGIT_MASK;       
    if (countSpecial < PASSWORD_SPECIAL_CHARACTER_MIN) :        
        result |= PASSWORD_SPECIAL_CHARACTER_MASK;
           
    return result
 
 
def changePassword():

    data = {'username': user, 'password': password, 'client_id': clientid, 'grant_type': 'password', 'takeExclusiveSignOnControl': True,
                    'scope': scope, 'newPassword' : newPassword}
    print("Sending changing password request to", auth_url, "...")

    try:
        # Request with auth for https protocol
        r = requests.post(auth_url,
                          headers={'Accept': 'application/json'},
                          data=data,
                          auth=(clientid, client_secret),
                          verify=True,
                          allow_redirects=False)

    except requests.exceptions.RequestException as e:
        print('Changing password exception failure:', e)
        return False

    if r.status_code == 200:
        auth_json = r.json()
        print("Password successfully changed.")
        print(json.dumps(auth_json, sort_keys=True, indent=2, separators=(',', ':')))
        return True
    elif r.status_code == 301 or r.status_code == 302 or r.status_code == 307 or r.status_code == 308:
        # Perform URL redirect
        print('Changing password response HTTP code:', r.status_code, r.reason)
        new_host = r.headers['Location']
        if new_host is not None:
            print('Perform URL redirect to ', new_host)
            return changePassword()
        return False
    elif r.status_code >= 400 :
        # Error during change password attempt
        auth_json = r.json()
        print('Changing password response HTTP code:', r.status_code, r.reason)
        print(json.dumps(auth_json, sort_keys=True, indent=2, separators=(',', ':')))
        return False
    else:
        # Retry the request to the API gateway
        print('Changing password response HTTP code:', r.status_code, r.reason)
        print('Retry change request')
        return changePassword()
    

if __name__ == "__main__":
    # Get command line parameters
    opts = []
    try:
        opts, args = getopt.getopt(sys.argv[1:], "", ["help", "app_id=", "user=", "clientid=", "password=", "newPassword=", 
                                                      "position=", "auth_url=", "discovery_url=", "scope=", "service=", "region=", "ric=",
                                                      "hotstandby"])
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
        elif opt in "--newPassword":
            newPassword = arg    
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

    if user == '' or password == '' or clientid == '':
        print("user, clientid and password are required options")
        sys.exit(2)
        
    if (newPassword != '') :
        policyResult = check_new_password(newPassword);
    
        if (policyResult & PASSWORD_INVALID_CHARACTER_MASK != 0) :
            print("New password contains invalid symbol");
            print("valid symbols are [A-Z][a-z][0-9]", PASSWORD_SPECIAL_CHARACTER_SET, sep = '');       
            sys.exit(2);
        
        if (policyResult & PASSWORD_LENGTH_MASK != 0) :
            print("New password length should be at least ", PASSWORD_LENGTH_MIN, " characters"); 
            sys.exit(2);
        
        countCategories = 0;
        if (policyResult & PASSWORD_UPPERCASE_LETTER_MASK == 0) :
            countCategories += 1;
        if (policyResult & PASSWORD_LOWERCASE_LETTER_MASK == 0) :
            countCategories += 1;
        if (policyResult & PASSWORD_DIGIT_MASK == 0) :
            countCategories += 1;
        if (policyResult & PASSWORD_SPECIAL_CHARACTER_MASK == 0) :
            countCategories += 1;        
    
        if (countCategories < PASSWORD_MIN_NUMBER_OF_CATEGORIES) :    
            print ("Password must contain characters belonging to at least three of the following four categories:\n"
		    	 "uppercase letters, lowercase letters, digits, and special characters.\n");
            sys.exit(2);     
    
        if (not changePassword()):
            sys.exit(2); 
            
        password = newPassword;
        newPassword = '';    

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

    original_expire_time = expire_time

    # Query VIPs from Refinitiv Data Platform service discovery
    if not query_service_discovery():
        print("Failed to retrieve endpoints from Refinitiv Data Platform Service Discovery. Exiting...")
        sys.exit(1)

    # Start websocket handshake; create two sessions when the hotstandby parameter is specified.
    session1 = WebSocketSession("session1", hostList[0])
    session1.connect()

    if hotstandby:
        session2 = WebSocketSession("session2", hostList[1])
        session2.connect()

    try:
        while True:
            #  Continue using current token until 90% of initial time before it expires.
            time.sleep(int(float(expire_time) * 0.90))

            sts_token, refresh_token, expire_time = get_sts_token(refresh_token)
            if not sts_token:
                sys.exit(1)

            if int(expire_time) != int(original_expire_time):
               print('expire time changed from ' + str(original_expire_time) + ' sec to ' + str(expire_time) + ' sec; retry with password')
               sts_token, refresh_token, expire_time = get_sts_token(None)
               if not sts_token:
                   sys.exit(1) 
               original_expire_time = expire_time

            # Update token.
            session1.refresh_token()
            if hotstandby:
                session2.refresh_token()

    except KeyboardInterrupt:
        session1.disconnect()
        if hotstandby:
            session2.disconnect()
