# Node.js Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary

The purpose of these examples is to show retrieving JSON-formatted market content
from a WebSocket server. The examples are as follows:

* __market\_price__: Retrieves market-price content for TRI.N.
* __market\_price\_batch\_view__: Retrieves market-price content for TRI.N, IBM.N, and T.N, 
  using a batch request. It also specifies a view that requests content only for fields 
  BID, ASK, and BIDSIZE.
* __market\_price\_authentication__: Retrieves market-price content for TRI.N, after 
  authenticating with an authentication server.
* __market\_price\_posting__: Retrieves market-price content for TRI.N, and posts
  market-price content back to it.
* __market\_price\_ping__: Retrieves market-price content for TRI.N, and monitors
  connection health by sending ping messages.

__market\_price__ forms the basis of the other examples, which implement additional
features. To see the code specific to each feature, a diff tool can be used to compare
the __market\_price__ source file with that of the appropriate example.

These applications are intended as basic usage examples. Some of the design choices
were made to favor simplicity and readability over performance. This application 
is not intended to be used for measuring performance.
## Command Line Usage

```node market_price.js [--hostname hostname] [--port port] [--app_id appID] [--user user]```

```node market_price_batch_view.js [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

```node market_price_authentication.js [--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]```

```node market_price_posting.js [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

Pressing the CTRL+C buttons terminates any example.
## Compiling Source
### Windows/macOS
1. __Install Node.js__
    - Go to: <https://nodejs.org/en/download/>
    - Follow installation instructions for your machine
2. __Install libraries__
    - `cd` to `streamingtools/Applications/Examples/node`
    - Run:
      - `npm install ip optimist ws request`
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/node`
    - To run `market_price.js` with options:
      - `node market_price.js --hostname <hostname>`

### RedHat/Oracle Linux
1. __Install Node.js package__
    - (Instructions are from <https://nodejs.org/en/download/package-manager/> )
    - Run (as root):
      - `curl --silent --location https://rpm.nodesource.com/setup_6.x | bash -`
      - `yum -y install nodejs`
2. __Install libraries__
    - `cd` to `streamingtools/Applications/Examples/node`
    - Run:
      - `npm install ip optimist ws request`
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/node`
    - To run `market_price.js` with options:
      - `node market_price.js --hostname <hostname>`

## Source File Description

* `market_price.js` - Source file for the market\_price example.

* `market_price_batch_view.js` - Source file for the market\_price\_batch\_view example.

* `market_price_authentication.js` - Source file for the market\_price\_authentication example.

* `market_price_posting.js` - Source file for the market\_price\_posting example.

* `market_price_ping.js` - Source file for the market\_price\_ping example.

