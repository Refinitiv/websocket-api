# Java Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to show retrieving JSON-formatted market content
from a WebSocket server. The examples are as follows:

* __MarketPrice__: Retrieves market-price content for TRI.N.
* __MarketPriceBatchView__: Retrieves market-price content for TRI.N, IBM.N, and T.N, 
  using a batch request. It also specifies a view that requests content only for fields 
  BID, ASK, and BIDSIZE.
* __MarketPriceAuthentication__: Retrieves market-price content for TRI.N, after 
  authenticating with an authentication server.
* __MarketPricePosting__: Retrieves market-price content for TRI.N, and posts
  market-price content back to it.
* __MarketPricePing__: Retrieves market-price content for TRI.N, and monitors
  connection health by sending ping messages.

__MarketPrice__ forms the basis of the other examples, which implement additional
features. To see the code specific to each feature, a diff tool can be used to compare
the __MarketPrice__ source file with that of the appropriate example.

These applications are intended as basic usage examples. Some of the design choices
were made to favor simplicity and readability over performance. This application 
is not intended to be used for measuring performance.

## Command Line Usage

in Windows,replace gradle with ./gradlew

```
gradle run -DmainClass=MarketPrice --args="[--hostname hostname] [--port port] [--app_id appID] [--user user] [--snapshot]"

gradle run -DmainClass=MarketPriceBatchView --args="[--hostname hostname ] [--port port] [--app_id appID] [--user user]"

gradle run -DmainClass=MarketPriceAuthentication --args="[--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]"

gradle run -DmainClass=MarketPricePosting --args="[--hostname hostname ] [--port port] [--app_id appID] [--user user]"
```

Pressing the CTRL+C buttons terminates any example.


## Source File Description

* `MarketPrice` - Source file for the MarketPrice example.

* `MarketPriceBatchView` - Source file for the MarketPriceBatchView example.

* `MarketPriceAuthentication` - Source file for the MarketPriceAuthentication example.

* `MarketPricePosting` - Source file for the MarketPricePosting example.

* `MarketPricePing` - Source file for the MarketPricePing example.
