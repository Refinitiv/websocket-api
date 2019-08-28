# Python Elektron Real-Time Examples
## Summary

These examples demonstrate authenticating via the Elektron Real-Time (ERT)
Service and Elektron Data Platform (EDP) Gateway, and consuming market content.

The market\_price\_edpgw\_authentication.py example demonstrates authenticating via an
HTTPS request to the EDP-RT Gateway using a username, clientid and password. It then
opens a WebSocket to the ERT service using the specified hostname, logs in with
the retrieved token, and requests market content.

The market\_price\_edpgw\_service\_discovery.py example demonstrates authenticating via an
HTTPS request to the EDP Gateway using a username, clientid and password, and discovering
endpoints of the EDP-RT service via an HTTPS request to EDP-RT Service
Discovery.  The example then opens a WebSocket to an endpoint (or optionally
two endpoints, if the --hotstandby option is used), logs in with the retrieved
token, and requests market content.

The examples periodically retrieve new authentication tokens, using a refresh
token included in the response from the Gateway instead of the username, clientid and
password. Once the new token is retrieved, they send a login request with this
token over their WebSockets to the ERT Service.

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
	  **The websocket-client must be version 0.49 or greater**

## Running the Examples

### Running the market\_price\_edpgw\_authentication Example

To run the example:
- Run `python market_price_edpgw_authentication.py --user <username> --password <password> --clientid <client ID> --hostname <Elektron Real-Time Service host>`
- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--user`          | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--password`      | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating with Gateway.
`--hostname`      | REQUIRED. Hostname of the Elektron Real-Time Service.
`--auth_url`      | OPTIONAL. URL of the EDP Gateway. Defaults to https://api.refinitiv.com:443/auth/oauth2/beta1/token.
`--port`          | OPTIONAL. Port of the Elektron Real-Time Service. Defaults to 443.
`--scope`         | OPTIONAL. An authorization scope to include when authenticating. Defaults to 'trapi'.
`--ric`          | OPTIONAL. Name of the item to request from the Elektron Real-Time Service. Can provide several rics if they are separated by comma (','). If not specified, /TRI.N is requested.
`--app_id`        | OPTIONAL. Application ID to use when logging in. If not specified, "256" is used.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.
`--view`         | OPTIONAL. List of fields to show, separated by comma (','). If not specified, all fields with be returned.
`--no_streaming` | OPTIONAL. Returns only one response and doest not stream the new events.

### Running the market\_price\_edpgw\_service\_discovery Example

To run the example:
- Run `python market_price_edpgw_service_discovery.py --user <username> --password <password> --clientid <client ID>`
- Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--user`          | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--password`      | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating with Gateway.
`--hotstandby`    | OPTIONAL. Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.
`--auth_url`      | OPTIONAL. URL of the EDP Gateway. Defaults to https://api.refinitiv.com:443/auth/oauth2/beta1/token.
`--discovery_url` | OPTIONAL. URL of the Service Discovery EDP Gateway. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--scope`         | OPTIONAL. An authorization scope to include when authenticating. Defaults to 'trapi'.
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. The region is either "amer" or "emea". Defaults to "amer".
`--ric`          | OPTIONAL. Name of the item to request from the Elektron Real-Time Service. Can provide several rics if they are separated by comma (','). If not specified, /TRI.N is requested.
`--app_id`        | OPTIONAL. Application ID to use when logging in. If not specified, "256" is used.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.
`--view`         | OPTIONAL. List of fields to show, separated by comma (','). If not specified, all fields with be returned.
`--no_streaming` | OPTIONAL. Returns only one response and doest not stream the new events.

## Source File Description

* `market_price_edpgw_authentication.py` - Source file for the market\_price\_edpgw\_authentication example.

* `market_price_edpgw_service_discovery.py` - Source file for the market\_price\_edpgw\_service\_discovery example.
