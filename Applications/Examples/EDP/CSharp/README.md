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
- Project files are included for Visual Studio 2012, 2013, and 2015. To compile the examples,
open the appropriate solution file and build it.

- This project uses WebSocketSharp to handle the WebSocket protocol, and JSON.NET to read
JSON messages. Both packages are retrieved via Visual Studio's NuGet extension; if enabled,
they will be downloaded automatically when the build is run.

- Note: Visual Studio 2012 may require updating its NuGet extension in order to download these 
packages.

## Command Line Usage
```MarketPriceEdpGwAuthenticationExample.exe [--appID appId] [--auth_hostname hostname] [--auth_port port] [--hostname hostname] [--password password] [--port port] [--ric ric] [--scope scope] [--user user]```
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option           |Description|
----------------:|-----------|
`--hostname`     | REQUIRED. Hostname of the Elektron Real-Time Service.
`--password`     | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--user`         | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--app_id`       | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_hostname`| OPTIONAL. Hostname of the EDP Gateway. Defaults to api.edp.thomsonreuters.com.
`--auth_port`    | OPTIONAL. Port of the EDP Gateway. Defaults to 443.
`--port`         | OPTIONAL. Port of the Elektron Real-Time Service. Defaults to 443.
`--ric`          | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`        | OPTIONAL. Identifier for a resource name. Defaults to trapi.

## Source File Description
* `MarketPriceEdpGwAuthenticationExample.cs` - Source file for the MarketPriceEdpGwAuthentication example.
* `MarketPriceEdpGwAuthenticationExample_VS140.csproj` - visual studio project


## Command Line Usage
```MarketPriceEdpGwServiceDiscoveryExample.exe [--app_id appID] [--auth_hostname hostname] [--auth_port port] [--hotstandby] [--password password] [--ric ric] [--scope scope] [--user user]```
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option           |Description|
----------------:|-----------|
`--password`     | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--user`         | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--app_id`       | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_hostname`| OPTIONAL. Hostname of the EDP Gateway. Defaults to api.edp.thomsonreuters.com.
`--auth_port`    | OPTIONAL. Port of the EDP Gateway. Defaults to 443.
`--hotstandby`   | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false.
`--ric`          | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`        | OPTIONAL. Identifier for a resource name. Defaults to trapi.

## Source File Description
* `MarketPriceEdpGwServiceDiscoveryExample.cs` - Source file for the MarketPriceEdpGwServiceDiscovery example.
* `MarketPriceEdpGwServiceDiscoveryExample_VS140.csproj` - visual studio project
