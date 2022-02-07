# Python Refinitiv Data Platform Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to connect to Refinitiv Real-Time - Optimized (RTO) to
retrieve JSON-formatted market content over a Websocket connection from a 
Refinitiv Real-Time Service after authenticating via Refinitiv Data Platform (RDP). 

The examples are:

* __market_price_rdpgw_authentication.py__: Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. The content is retrieved using endpoint information (host and port)
  supplied in input. This example maintains a session by proactively renewing access_token
  token before expiration. The Authentication is 'oAuthPasswordGrant' or RDP version1 (v1) auth
  which uses password grant or refresh_token grant with Refinitiv provided credentials:
  username and password. Also required is clientid which is generated using AppGenerator tool.

* __market_price_rdpgw_service_discovery.py__: Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. This example discovers the endpoint information using a service
  discovery URL using a region supplied as input. The content is retrieved using
  this endpoint information. This example maintains a session by proactively renewing
  authentication token before expiration. The Authentication is 'oAuthPasswordGrant' 
  or RDP version1 (v1) auth which uses password grant or refresh_token grant with Refinitiv
  provided credentials: username and password. Also required is clientid which is 
  generated using AppGenerator tool.

* __market_price_rdpgw_client_cred_auth.py__: Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with Refintiv Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to Refinitiv Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthClientCred' or RDP version2 (v2) auth which uses client credentials grant
  with Refintiv provided credentials: clientid (username) and clientsecret (password). 

These applications are intended as sample examples. Some of the design choices
were made to favor simplicity and readability over performance. These applications
are not intended to be used for measuring performance.

NOTE: All RDP examples must be run with Python3.0 or greater.

## Setup
### Windows/Linux/macOS
1. __Install Python__
    - Go to: <https://www.python.org/downloads/>
    - Select the __Download tile__ for the Python 3 version
    - Run the downloaded `python-<version>` file and follow installation instructions
2. __Install libraries__
    - Run (in order):
      - `pip install requests`
      - `pip install websocket-client`
	  **The websocket-client must be version 1.1.0 or greater**

## Running the Examples

### Running the market\_price\_rdpgw\_authentication Example

To run the example:
- Run `python3 market_price_rdpgw_authentication.py --user <username> --password <password> --clientid <clientid> --hostname <Refinitiv Real-Time Service host>`
- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating to Refinitiv Data Platform
`--hostname`      | REQUIRED. Hostname of the Refinitiv Real-Time Service.
`--password`      | REQUIRED. Password to use when authenticating to Refinitiv Data Platform.
`--user`          | REQUIRED. Username to use when authenticating to Refinitiv Data Platform.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via Refinitiv Data Platform.  Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--newPassword`   | OPTIONAL. New password provided by user to change password to.
`--port`          | OPTIONAL. Port of the Refinitiv Real-Time Service. Defaults to 443.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested Real-Time service name or service ID. Defaults to ELEKTRON_DD.

NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description

* `market_price_rdpgw_authentication.py` - Source file for the market\_price\_rdpgw\_authentication example.

### Running the market\_price\_rdpgw\_service\_discovery Example

To run the example:
- Run `python3 market_price_rdpgw_service_discovery.py --user <username> --password <password> --clientid <clientid>`
- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating to Refinitiv Data Platform.
`--password`      | REQUIRED. Password to use when authenticating to Refinitiv Data Platform.
`--user`          | REQUIRED. Username to use when authenticating to Refinitiv Data Platform.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via Refinitiv Data Platform.  Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--discovery_url` | OPTIONAL. URL of Service Discovery via Refinitiv Data Platform.  Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hotstandby`    | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false.
`--newPassword`   | OPTIONAL. New password provided by user to change password to.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.
NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.


#### Source File Description

* `market_price_rdpgw_service_discovery.py` - Source file for the market\_price\_rdpgw\_service\_discovery example.

### Running the market\_price\_rdpgw\_client\_cred\_auth Example

To run the example with specified endpoint:
- Run `python3 market_price_rdpgw_client_cred_auth.py --clientid <clientid> --clientsecret <clientsecret> --hostname <hostname>`

To run the example with discovered endpoint:
- Run `python3 market_price_rdpgw_client_cred_auth.py --clientid <clientid> --clientsecret <clientsecret>`

- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. An ID or username to use when authenticating to Refinitiv Data Platform.
`--clientsecret`  | REQUIRED. A password or secret to use when authenticating to Refinitiv Data Platform.

NOTE: REQUIRED are either 'region' OR 'hostname':
`--hostname`      | REQUIRED. Hostname of the Refinitiv Real-Time Service.
`--region`        | REQUIRED. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.

`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via Refinitiv Data Platform.  Defaults to https://api.refinitiv.com:443/auth/oauth2/v2/token.
`--discovery_url` | OPTIONAL. URL of Service Discovery via Refinitiv Data Platform.  Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hostname2`     | OPTIONAL. Hostname of secondary endpoint in RTO to use for Hot StandBy feature.
`--hotstandby`    | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false.
`--port`          | OPTIONAL. Port of the Refinitiv Real-Time Service. Defaults to 443.
`--port2`         | OPTIONAL. Port of the secondary endpoint in RTO to use for Hot StandBy feature. Defaults to 443.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.


#### Source File Description

* `market_price_rdpgw_client_cred_auth.py` - Source file for the market\_price\_rdpgw\_client\_cred\_auth example.
