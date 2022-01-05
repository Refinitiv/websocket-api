# C# Refinitiv Data Platfrom Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to connect to Refinitiv Real-Time - Optimized (RTO) to
retrieve JSON-formatted market content over a Websocket connection from a 
Refinitiv Real-Time Service after authenticating via Refinitiv Data Platform (RDP). 

Explanation of Example:

* __market_price_rdpgw_client_cred_auth__: Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with Refintiv Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to Refinitiv Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthClientCred' or RDP version2 (v2) auth which uses client credentials grant
  with Refintiv provided credentials: clientid (username) and clientsecret (password). 

This application is intended as sample code. Some of the design choices
were made to favor simplicity and readability over performance. This application
is not intended to be used for measuring performance.


## Setup 
### Windows
1. __Install Go__
    - Go to: <https://golang.org/dl/>
    - Follow installation instructions for your machine
    - Test installation as instructed on website
2. __Install libraries__
    - Run:
      - `go get github.com/gorilla/websocket`

### RedHat/Oracle Linux
1. __Install Go via Yum__
    - Run:
      - `yum install golang`
      - If this package is not available on your system, it may be available in one of the following repositories:
          - (RedHat 6, Oracle 6) Extra Packages for Enterprise Linux (<https://fedoraproject.org/wiki/EPEL>):
            `rpm -Uvh http://download.fedoraproject.org/pub/epel/6/i386/epel-release-6-8.noarch.rpm`
	  - (Oracle 6, Oracle 7) Download the appropriate repository file for the system and enable the latest/addons repositories, as described here: <https://docs.oracle.com/cd/E37670_01/E37355/html/ol_downloading_yum_repo.html>
      
2. __Install libraries__
    - Run:
      - `go get github.com/gorilla/websocket`

## Running the Example

### Running the market_price_rdpgw_client_cred_auth Example

  - Run: `go run market_price_rdpgw_client_cred_auth.go --clientid <clientid> --clientsecret <clientsecret> --hostname <hostname>`
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

* `market_price_rdpgw_client_cred_auth.go` - Source file for the market_price_rdpgw_client_cred_auth example.
