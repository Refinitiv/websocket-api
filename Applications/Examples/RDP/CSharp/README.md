# C# Refinitiv Data Platfrom Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to connect to the Refinitiv Data Platform to retrieve
JSON-formatted market content over a Websocket connection from a Refinitiv Real-Time Service.

The examples are:

* __MarketPriceRdpGwAuthenticationExample__: Retrieves market-price content for a RIC after
  authenticating with an authentication server and using tokens sent by that server to keep
  the connection alive. The content is retrieved using endpoint information (host and port)
  supplied as input. This example maintains a session by proactively renewing authentication
  token before expiration.

* __MarketPriceRdpGwServiceDiscoveryExample__: Retrieves market-price content for a RIC after
  authenticating with an authentication server and using tokens sent by that server to keep
  the connection alive. This example discovers the endpoint information using a service 
  discovery URL using a region supplied as input. The content is retrieved using 
  this endpoint information. This example maintains a session by proactively renewing 
  authentication token before expiration. 

These applications are intended as basic usage examples. Some of the design choices
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
  - Run: `dotnet MarketPriceRdpGwAuthenticationExample.dll --user <username> --password <password> --clientid clientID --hostname <Refinitiv Real-Time Service host>`
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

  - Run: `dotnet MarketPriceRdpGwServiceDiscoveryExample.dll --user <username> --password <password> --clientid <clientID>`
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
