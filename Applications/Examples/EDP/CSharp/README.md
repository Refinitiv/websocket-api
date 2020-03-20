# C# Examples
## Summary

The purpose of these examples is to show retrieving JSON-formatted market content
from a WebSocket server using authentication tokens and to show retrieving the location
of the market price server using service discovery. The examples are:

* __MarketPriceEdpGwAuthenticationExample__: Retrieves market-price content for TRI.N after
  authenticating with an authentication server and using tokens sent by that server to keep
  the connection alive.

* __MarketPriceEdpGwServiceDiscoveryExample__: Retrieves market-price content for TRI.N after
  authenticating with an authentication server and using tokens sent by that server to keep
  the connection alive and after using service discovery to find the location of the service
  providing the market-price content.

These applications are intended as basic usage examples. Some of the design choices
were made to favor simplicity and readability over performance. These applications
are not intended to be used for measuring performance.

## Compiling Source
### Windows
- Project files are included for Visual Studio 2017. To compile the examples, open the solution
file and build it.

- This project uses Newtonsoft.Json to read JSON messages. The package is retrieved via Visual
Studio's NuGet extension; if enabled, they will be downloaded automatically when the build is run.

### Linux
```dotnet build CSharpEdpGwExamples_VS150.sln```

## Command Line Usage
```dotnet MarketPriceEdpGwAuthenticationExample.dll --user <username> --password <password> --clientid clientID --hostname <Elektron Real-Time Service host>```
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--hostname`      | REQUIRED. Hostname of the Elektron Real-Time Service.
`--user`          | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--password`      | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating with Gateway.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of the EDP Gateway. Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--port`          | OPTIONAL. Port of the Elektron Real-Time Service. Defaults to 443.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

## Source File Description
* `MarketPriceEdpGwAuthenticationExample.cs` - Source file for the MarketPriceEdpGwAuthentication example.
* `MarketPriceEdpGwAuthenticationExample.csproj` - visual studio project

## Command Line Usage
```dotnet MarketPriceEdpGwServiceDiscoveryExample.dll --user <username> --password <password> --clientid <clientID>```
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--user`          | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--password`      | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating with Gateway.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of the EDP Gateway. Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--discovery_url` | OPTIONAL. URL of the Service Discovery EDP Gateway. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hotstandby`    | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false.
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. The region is either "amer", "emea", or "apac". Defaults to "amer".
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

## Source File Description
* `MarketPriceEdpGwServiceDiscoveryExample.cs` - Source file for the MarketPriceEdpGwServiceDiscovery example.
* `MarketPriceEdpGwServiceDiscoveryExample.csproj` - visual studio project
