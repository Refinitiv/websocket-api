# Python Elektron Real-Time Example
## Summary

This example demonstrates authenticating via the Elektron Real-Time Service and
Elektron Data Platform Gateway, and logging in with the retrieved token to
retrieve market content.

The example first sends an HTTP request to the EDP Gateway, using the specified
username and password. The Gateway provides an authentication token in
response.

The example then opens a WebSocket to the Elektron Real-Time Service at the
specified host, logs in using the authentication token, then retrieves market
content.

The example periodically retrieves new authentication tokens, using a refresh
token included in the response from the Gateway instead of the username and
password. Once the new token is retrieved, it sends a login request with this
token over its WebSocket to the Elektron Real-Time Service.

## Setup and Run Example
### Windows/Linux/macOS
1. __Install Python__
    - Go to: <https://www.python.org/downloads/>
    - Select the __Download tile__ for the Python 3 version
    - Run the downloaded `python-<version>` file and follow installation instructions
2. __Install libraries__
    - Run (in order):
      - `pip install requests`
      - `pip install websocket-client`
3. __Run examples__
    - `cd` to `Applications/Examples/EDP/python`
    - To run `market_price_edpgw_authentication.py` with options:
      - `python market_price_edpgw_authentication.py --user <username> --password <password> --hostname <Elektron Real-Time Service host>`
	  - Pressing the CTRL+C buttons terminates the example.

### Commandline Option Descriptions

Option           |Description|
----------------:|-----------|
`--auth_hostname`| OPTIONAL. Hostname of the EDP Gateway. Defaults to api.edp.thomsonreuters.com.
`--auth_port`    | OPTIONAL. Port of the EDP Gateway. Defaults to 443.
`--hostname`     | REQUIRED. Hostname of the Elektron Real-Time Service.
`--port`         | OPTIONAL. Port of the Elektron Real-Time Service. Defaults to 443.
`--user`         | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--password`     | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--scope`        | OPTIONAL. An authorization scope to include when authenticating. Defaults to 'trapi'.
`--ric`          | OPTIONAL. Name of the item to request from the Elektron Real-Time Service. If not specified, TRI.N is requested.
`--app_id`       | OPTIONAL. Application ID to use when logging in. If not specified, "256" is used.
`--position`     | OPTIONAL. Position to use when logging in. If not specified, the current host is used.

## Source File Description

* `market_price_edpgw_authentication.py` - Source file for the market\_price\_edpgw\_authentication example.

