# Python Refinitiv Data Platform Examples
## Summary

The purpose of these examples is to connect to the Refinitiv Data Platform to retrieve
JSON-formatted market content over a Websocket connection from a Refinitiv Real-Time Service.

The examples are:

* __market_price_edpgw_authentication.py__: Retrieves market-price content for a RIC after
  authenticating with an authentication server and using tokens sent by that server to keep
  the connection alive. The content is retrieved using endpoint information (host and port)
  supplied as input. This example maintains a session by proactively renewing authentication
  token before expiration.

* __market_price_edpgw_service_discovery.py__: Retrieves market-price content for a RIC after
  authenticating with an authentication server and using tokens sent by that server to keep
  the connection alive. This example discovers the endpoint information using a service
  discovery URL using a region supplied as input. The content is retrieved using
  this endpoint information. This example maintains a session by proactively renewing
  authentication token before expiration.

These applications are intended as basic usage examples. Some of the design choices
were made to favor simplicity and readability over performance. These applications
are not intended to be used for measuring performance.

## Installation
### Windows/Linux/macOS
1. __Install Python__
    - Go to: <https://www.python.org/downloads/>
    - Select the __Download tile__ for the Python 3 version
    - Run the downloaded `python-<version>` file and follow installation instructions
2. __Install libraries__
    - Run (in order):
      - `pip install requests`
      - `pip install websocket-client`
	  **The websocket-client must be version 0.49 or greater**

## Running the Examples

### Running the market\_price\_edpgw\_authentication Example

To run the example:
- Run `python3 market_price_edpgw_authentication.py --user <username> --password <password> --clientid <client ID> --hostname <Refinitiv Real-Time Service host>`
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
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.
`--service`       | OPTIONAL. The requested Real-Time service name or service ID. Defaults to ELEKTRON_DD.

NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

### Running the market\_price\_edpgw\_service\_discovery Example

To run the example:
- Run `python3 market_price_edpgw_service_discovery.py --user <username> --password <password> --clientid <client ID>`
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
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery.  The region is either "amer", "emea", or "apac". Defaults to "amer".
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.
NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.


#### Source File Description

* `market_price_edpgw_service_discovery.py` - Source file for the market\_price\_edpgw\_service\_discovery example.
