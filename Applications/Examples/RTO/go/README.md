# Golang Real-Time - Optimized Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. LSEG MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

__IMPORTANT NOTE__ regarding the following examples:
Version 2 authentication examples are available as Early Access to API developers 
to preview changes required to use this new authentication mechanism. Please note that 
ability to setup Service Accounts to use this authentication is forthcoming.

The purpose of these examples is to connect to Real-Time - Optimized (RTO) to
retrieve JSON-formatted market content over a Websocket connection from a 
LSEG Real-Time Service after authenticating via LSEG Delivery Platform (LDP). 

Explanation of the Examples:

* __market_price_rto_client_cred_auth__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to LSEG Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthClientCred' or LDP version2 (v2) auth which uses client credentials grant
  with LSEG provided Service Account credentials: clientid (username) and 
  clientsecret (password). 

* __market_price_rto_jwt_auth__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to LSEG Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthJwt' or LDP version2 (v2) auth which uses client credentials grant
  with LSEG provided Service Account credentials: clientid (username) and 
  JWT (JSON Web Token). 

These applications are intended as sample code. Some of the design choices
were made to favor simplicity and readability over performance. These applications
are not intended to be used for measuring performance.


## Setup 
### Windows
1. __Install Go__
    - Go to: <https://golang.org/dl/>
    - Follow installation instructions for your machine
    - Test installation as instructed on website
2. __Create new go.mod module market\_price\_rto\_examples__
    - Run:
      - `go mod init market_price_rto_examples`
3. __Install libraries__
    - Run:
      - `go mod tidy`

### RedHat/Oracle Linux
1. __Install Go via Yum__
    - Run:
      - `yum install golang`
      - If this package is not available on your system, it may be available in one of the following repositories:
          - (RedHat 6, Oracle 6) Extra Packages for Enterprise Linux (<https://fedoraproject.org/wiki/EPEL>):
            `rpm -Uvh http://download.fedoraproject.org/pub/epel/6/i386/epel-release-6-8.noarch.rpm`
	  - (Oracle 6, Oracle 7) Download the appropriate repository file for the system and enable the latest/addons repositories, as described here: <https://docs.oracle.com/cd/E37670_01/E37355/html/ol_downloading_yum_repo.html>
      
2. __Create new go.mod module market\_price\_rto\_examples__
    - Run:
      - `go mod init market_price_rto_examples`
3. __Install libraries__
    - Run:
      - `go mod tidy`

## Running the Examples

### Running the market_price_rto_client_cred_auth Example

  - Run: `go run market_price_rto_client_cred_auth.go --clientid <clientid> --clientsecret <clientsecret> --hostname <hostname>`
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

* `market_price_rto_client_cred_auth.go` - Source file for the market_price_rto_client_cred_auth example.

### Running the market_price_rto_jwt_auth Example

  - Run: `go run market_price_rto_jwt_auth.go --clientid <clientid> --jwkFile <JWK file> --hostname <hostname>`
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

* `market_price_rto_jwt_auth.go` - Source file for the market_price_rto_jwt_auth example.
