# C# Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to show retrieving JSON-formatted market content
from a WebSocket server. The examples are as follows:

* __MarketPriceExample__: Retrieves market-price content for TRI.N.
* __MarketPriceBatchViewExample__: Retrieves market-price content for TRI.N, IBM.N, and T.N, 
  using a batch request. It also specifies a view that requests content only for fields 
  BID, ASK, and BIDSIZE.
* __MarketPriceAuthenticationExample__: Retrieves market-price content for TRI.N, after 
  authenticating with an authentication server.
* __MarketPricePostingExample__: Retrieves market-price content for TRI.N, and posts
  market-price content back to it.
* __MarketPricePingExample__: Retrieves market-price content for TRI.N, and monitors
  connection health by sending ping messages.

__MarketPriceExample__ forms the basis of the other examples, which implement additional
features. To see the code specific to each feature, a diff tool can be used to compare
the __MarketPriceExample__ source file with that of the appropriate example.

These applications are intended as basic usage examples. Some of the design choices
were made to favor simplicity and readability over performance. This application 
is not intended to be used for measuring performance.

## Compiling Source
### Windows
- Project files are included for Visual Studio 2015. To compile the examples,
open the appropriate solution file and build it.

- This project uses WebSocketSharp to handle the WebSocket protocol, and JSON.NET to read
JSON messages. Both packages are retrieved via Visual Studio's NuGet extension; if enabled,
they will be downloaded automatically when the build is run.

## Command Line Usage

``` MarketPriceExample.exe [--hostname hostname] [--port port] [--app_id appID] [--user user] [--snapshot]```

``` MarketPriceBatchViewExample.exe [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

``` MarketPriceAuthenticationExample.exe [--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]```

``` MarketPricePostingExample.exe [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

Pressing the CTRL+C buttons terminates any example.

## Source File Description

* `MarketPriceExample.cs` - Source file for the MarketPriceExample example.

* `MarketPriceBatchViewExample.cs` - Source file for the MarketPriceBatchViewExample example.

* `MarketPriceAuthenticationExample.cs` - Source file for the MarketPriceAuthenticationExample example.

* `MarketPricePostingExample.cs` - Source file for the MarketPricePostingExample example.

* `MarketPricePingExample.cs` - Source file for the MarketPricePingExample example.
