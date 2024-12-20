# Python Real-Time - Optimized Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. LSEG MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to connect to LSEG Real-Time - Optimized (RTO) to
retrieve JSON-formatted market content over a Websocket connection from a 
LSEG Real-Time Service after authenticating via LSEG Delivery Platform (LDP). 

The examples are:

* __market_price_rto_authentication.py__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. The content is retrieved using endpoint information (host and port)
  supplied in input. This example maintains a session by proactively renewing access_token
  token before expiration. The Authentication is 'oAuthPasswordGrant' or LDP version1 (v1) auth
  which uses password grant or refresh_token grant with LSEG provided Machine Account 
  credentials: username and password. Also required is clientid which is 
  generated using AppGenerator tool.

* __market_price_rto_service_discovery.py__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. This example discovers the endpoint information using a service
  discovery URL using a region supplied as input. The content is retrieved using
  this endpoint information. This example maintains a session by proactively renewing
  authentication token before expiration. The Authentication is 'oAuthPasswordGrant' 
  or LDP version1 (v1) auth which uses password grant or refresh_token grant with LSEG
  provided Machine Account credentials: username and password. Also required is clientid which is 
  generated using AppGenerator tool.

__IMPORTANT NOTE__ regarding the following example, market_price_rto_client_cred_auth.py: 
Version 2 authentication example is available as Early Access to API developers 
to preview changes required to use this new authentication mechanism. Please note that 
ability to setup Service Accounts to use this authentication is forthcoming.

* __market_price_rto_client_cred_auth.py__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with LSEG Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to LSEG Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthClientCred' or LDP version2 (v2) auth which uses client credentials grant
  with LSEG provided Service Account credentials: clientid (username) and 
  clientsecret (password). 

__IMPORTANT NOTE__ regarding the following example, market_price_rto_jwt_auth.py: 
Version 2 authentication example is available as Early Access to API developers 
to preview changes required to use this new authentication mechanism. Please note that 
ability to setup Service Accounts to use this authentication is forthcoming.

* __market_price_rto_jwt_auth.py__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with LSEG Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to LSEG Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthJwt' or LDP version2 (v2) auth which uses client credentials grant
  with LSEG provided Service Account credentials: clientid (username) and 
  JWT (JSON Web Token). 

These applications are intended as sample examples. Some of the design choices
were made to favor simplicity and readability over performance. These applications
are not intended to be used for measuring performance.

NOTE: All LDP examples must be run with Python3.0 or greater.

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
      - `pip install jwcrypto`

## Running the Examples

### Running the market\_price\_rto\_authentication Example

To run the example:
- Run `python3 market_price_rto_authentication.py --user <username> --password <password> --clientid <clientid> --hostname <LSEG Real-Time Service host>`
- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating to LSEG Delivery Platform
`--hostname`      | REQUIRED. Hostname of the LSEG Real-Time Service.
`--password`      | REQUIRED. Machine Account Password to use when authenticating to LSEG Delivery Platform.
`--user`          | REQUIRED. Machine Account Username to use when authenticating to LSEG Delivery Platform.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via LSEG Delivery Platform. Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--newPassword`   | OPTIONAL. New password provided by user to change password to.
`--port`          | OPTIONAL. Port of the LSEG Real-Time Service. Defaults to 443.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested Real-Time service name or service ID. Defaults to ELEKTRON_DD.

NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description

* `market_price_rto_authentication.py` - Source file for the market\_price\_rto\_authentication example.

### Running the market\_price\_rto\_service\_discovery Example

To run the example:
- Run `python3 market_price_rto_service_discovery.py --user <username> --password <password> --clientid <clientid>`
- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating to LSEG Delivery Platform.
`--password`      | REQUIRED. Machine Account Password to use when authenticating to LSEG Delivery Platform.
`--user`          | REQUIRED. Machine Account Username to use when authenticating to LSEG Delivery Platform.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via LSEG Delivery Platform. Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--discovery_url` | OPTIONAL. URL of Service Discovery via LSEG Delivery Platform. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
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

* `market_price_rto_service_discovery.py` - Source file for the market\_price\_rto\_service\_discovery example.

### Running the market\_price\_rto\_client\_cred\_auth Example

To run the example with specified endpoint:
- Run `python3 market_price_rto_client_cred_auth.py --clientid <clientid> --clientsecret <clientsecret> --hostname <hostname>`

To run the example with discovered endpoint:
- Run `python3 market_price_rto_client_cred_auth.py --clientid <clientid> --clientsecret <clientsecret>`

- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option              |Description|
-------------------:|-----------|
`--clientid`        | REQUIRED. Service Account ClientID to use when authenticating to LSEG Delivery Platform.
`--clientsecret`    | REQUIRED. Service Account ClientSecret to use when authenticating to LSEG Delivery Platform.
`--app_id`          | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`        | OPTIONAL. V2 URL for authentication via LSEG Delivery Platform. Defaults to https://api.refinitiv.com:443/auth/oauth2/v2/token.
`--discovery_url`   | OPTIONAL. URL of Service Discovery via LSEG Delivery Platform. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hostname`        | OPTIONAL. Hostname of the LSEG Real-Time Service. If unspecified, service discovery will be used.
`--standbyhostname` | OPTIONAL. Hostname of secondary endpoint in RTO to use for Hot StandBy feature.
`--hotstandby`      | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false.
`--port`            | OPTIONAL. Port of the LSEG Real-Time Service. Defaults to 443.
`--standbyport`     | OPTIONAL. Port of the secondary endpoint in RTO to use for Hot StandBy feature. Defaults to 443.
`--position`        | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--region`          | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.
`--ric`             | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`           | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`         | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.


#### Source File Description

* `market_price_rto_client_cred_auth.py` - Source file for the market\_price\_rto\_client\_cred\_auth example.

### Running the market\_price\_rto\_jwt\_auth Example

To run the example with specified endpoint:
- Run `python3 market_price_rto_jwt_auth.py --clientid <clientid> --jwkFile <JWK file> --hostname <hostname>`

To run the example with discovered endpoint:
- Run `python3 market_price_rto_jwt_auth.py --clientid <clientid> --jwkFile <JWK file>`

- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option              |Description|
-------------------:|-----------|
`--clientid`        | REQUIRED. Service Account ClientID to use when authenticating to LSEG Delivery Platform.
`--jwkFile`         | REQUIRED. Service Account JWK file to sign JWT when authenticating to LSEG Delivery Platform.
`--aud`             | OPTIONAL. JWT Audience to use when authenticating to LSEG Delivery Platform. Defaults to https://login.ciam.refinitiv.com/as/token.oauth2.
`--app_id`          | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`        | OPTIONAL. V2 URL for authentication via LSEG Delivery Platform. Defaults to https://api.refinitiv.com:443/auth/oauth2/v2/token.
`--discovery_url`   | OPTIONAL. URL of Service Discovery via LSEG Delivery Platform. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hostname`        | OPTIONAL. Hostname of the LSEG Real-Time Service. If unspecified, service discovery will be used.
`--standbyhostname` | OPTIONAL. Hostname of secondary endpoint in RTO to use for Hot StandBy feature.
`--hotstandby`      | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false.
`--port`            | OPTIONAL. Port of the LSEG Real-Time Service. Defaults to 443.
`--standbyport`     | OPTIONAL. Port of the secondary endpoint in RTO to use for Hot StandBy feature. Defaults to 443.
`--position`        | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--region`          | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.
`--ric`             | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`           | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`         | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.


#### Source File Description

* `market_price_rto_jwt_auth.py` - Source file for the market\_price\_rto\_jwt\_auth example.
