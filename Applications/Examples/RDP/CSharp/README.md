# C# Refinitiv Data Platfrom Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to connect to Refinitiv Real-Time - Optimized (RTO) to
retrieve JSON-formatted market content over a Websocket connection from a 
Refinitiv Real-Time Service after authenticating via Refinitiv Data Platform (RDP). 

The examples are:

* __MarketPriceRdpGwAuthenticationExample__:  Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. The content is retrieved using endpoint information (host and port)
  supplied in input. This example maintains a session by proactively renewing access_token
  token before expiration. The Authentication is 'oAuthPasswordGrant' or RDP version1 (v1) auth
  which uses password grant or refresh_token grant with Refinitiv provided credentials:
  username and password. Also required is clientid which is generated using AppGenerator tool.

* __MarketPriceRdpGwServiceDiscoveryExample__: Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. This example discovers the endpoint information using a service
  discovery URL using a region supplied as input. The content is retrieved using
  this endpoint information. This example maintains a session by proactively renewing
  authentication token before expiration. The Authentication is 'oAuthPasswordGrant' 
  or RDP version1 (v1) auth which uses password grant or refresh_token grant with Refinitiv
  provided credentials: username and password. Also required is clientid which is 
  generated using AppGenerator tool.

* __MarketPriceRdpGwClientCredAuthExample__: Retrieves market-price content for a RIC after
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


## Setup 
### Windows
- Project files are included for Visual Studio 2017. To compile the examples, open the solution
file and build it.

- This project uses Newtonsoft.Json to read JSON messages. The package is retrieved via Visual
Studio's NuGet extension; if enabled, they will be downloaded automatically when the build is run.

### Linux
To build: `dotnet build CSharpRdpGwExamples_VS150.sln`

## Running the Examples

### Running the MarketPriceRdpGwAuthentication Example

To run the example:
  - Run: `dotnet MarketPriceRdpGwAuthenticationExample.dll --user <username> --password <password> --clientid clientid --hostname <hostname>`
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
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested Real-Time service name or service ID. Defaults to ELEKTRON_DD.

NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description
* `MarketPriceRdpGwAuthenticationExample.cs` - Source file for the MarketPriceRdpGwAuthentication example.
* `MarketPriceRdpGwAuthenticationExample.csproj` - visual studio project

### Running the MarketPriceRdpGwServiceDiscovery Example

  - Run: `dotnet MarketPriceRdpGwServiceDiscoveryExample.dll --user <username> --password <password> --clientid <clientid>`
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
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.
NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description

* `MarketPriceRdpGwServiceDiscoveryExample.cs` - Source file for the MarketPriceRdpGwServiceDiscovery example.
* `MarketPriceRdpGwServiceDiscoveryExample.csproj` - visual studio project

### Running the MarketPriceRdpGwClientCredAuthExample Example

  - Run: `dotnet MarketPriceRdpGwClientCredAuthExample.dll --clientid <clientid> --clientsecret <clientsecret> --hostname <hostname>`
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

* `MarketPriceRdpGwClientCredAuthExample.cs` - Source file for the MarketPriceRdpGwClientCredAuthExample example.
* `MarketPriceRdpGwClientCredAuthExample.csproj` - visual studio project
