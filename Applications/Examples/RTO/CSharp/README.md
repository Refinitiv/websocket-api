# C# Real-Time - Optimized Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. LSEG MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to connect to Real-Time - Optimized (RTO) to
retrieve JSON-formatted market content over a Websocket connection from a 
LSEG Real-Time Service after authenticating via LSEG Delivery Platform (LDP). 

The examples are:

* __MarketPriceRTOAuthenticationExample__:  Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. The content is retrieved using endpoint information (host and port)
  supplied in input. This example maintains a session by proactively renewing access_token
  token before expiration. The Authentication is 'oAuthPasswordGrant' or LDP version1 (v1) auth
  which uses password grant or refresh_token grant with LSEG provided Machine Account 
  credentials: username and password. Also required is clientid which is 
  generated using AppGenerator tool.

* __MarketPriceRTOServiceDiscoveryExample__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. This example discovers the endpoint information using a service
  discovery URL using a region supplied as input. The content is retrieved using
  this endpoint information. This example maintains a session by proactively renewing
  authentication token before expiration. The Authentication is 'oAuthPasswordGrant' 
  or LDP version1 (v1) auth which uses password grant or refresh_token grant with LSEG
  provided Machine Account credentials: username and password. Also required is clientid which is 
  generated using AppGenerator tool.

__IMPORTANT NOTE__ regarding the following example, MarketPriceRTOClientCredAuthExample: 
Version 2 authentication example is available as Early Access to API developers 
to preview changes required to use this new authentication mechanism. Please note that 
ability to setup Service Accounts to use this authentication is forthcoming.

* __MarketPriceRTOClientCredAuthExample__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with LSEG Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to LSEG Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthClientCred' or LDP version2 (v2) auth which uses client credentials grant
  with LSEG provided Service Account credentials: clientid (username) and 
  clientsecret (password). 

__IMPORTANT NOTE__ regarding the following example, MarketPriceRTOJwtAuthExample: 
Version 2 authentication example is available as Early Access to API developers 
to preview changes required to use this new authentication mechanism. Please note that 
ability to setup Service Accounts to use this authentication is forthcoming.

* __MarketPriceRTOJwtAuthExample__: Retrieves market-price content for a RIC after
  authenticating with LDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with LSEG Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to LSEG Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthClientCred' or LDP version2 (v2) auth which uses client credentials grant
  with LSEG provided Service Account credentials: clientid (username) and 
  JWT (JSON Web Token). 

These applications are intended as sample examples. Some of the design choices
were made to favor simplicity and readability over performance. These applications
are not intended to be used for measuring performance.

## Setup 
### Windows
- Project files are included for Visual Studio 2022. To compile the examples, open the solution
file and build it.

- This project uses Newtonsoft.Json to read JSON messages. The package is retrieved via Visual
Studio's NuGet extension; if enabled, they will be downloaded automatically when the build is run.

### Linux
To build: `dotnet build CSharpRTOExamples.sln`

## Running the Examples

### Running the MarketPriceRTOAuthentication Example

To run the example:
  - Run: `dotnet MarketPriceRTOAuthenticationExample.dll --user <username> --password <password> --clientid clientid --hostname <hostname>`
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
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested Real-Time service name or service ID. Defaults to ELEKTRON_DD.

NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description
* `MarketPriceRTOAuthenticationExample.cs` - Source file for the MarketPriceRTOAuthentication example.
* `MarketPriceRTOAuthenticationExample.csproj` - visual studio project

### Running the MarketPriceRTOServiceDiscovery Example

  - Run: `dotnet MarketPriceRTOServiceDiscoveryExample.dll --user <username> --password <password> --clientid <clientid>`
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
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.
NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description

* `MarketPriceRTOServiceDiscoveryExample.cs` - Source file for the MarketPriceRTOServiceDiscovery example.
* `MarketPriceRTOServiceDiscoveryExample.csproj` - visual studio project

### Running the MarketPriceRTOClientCredAuthExample Example

  - Run: `dotnet MarketPriceRTOClientCredAuthExample.dll --clientid <clientid> --clientsecret <clientsecret> --hostname <hostname>`
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

* `MarketPriceRTOClientCredAuthExample.cs` - Source file for the MarketPriceRTOClientCredAuthExample example.
* `MarketPriceRTOClientCredAuthExample.csproj` - visual studio project

### Running the MarketPriceRTOJwtAuthExample Example

  - Run: `dotnet MarketPriceRTOJwtAuthExample.dll --clientid <clientid> --jwkFile <client JWK> --hostname <hostname>`
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

* `MarketPriceRTOJwtAuthExample.cs` - Source file for the MarketPriceRTOJwtAuthExample example.
* `MarketPriceRTOJwtAuthExample.csproj` - visual studio project
