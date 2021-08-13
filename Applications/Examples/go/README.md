# Go Examples

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

```go run market_price.go [--hostname hostname] [--port port] [--app_id appID] [--user user]```

```go run market_price_batch_view.go [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

```go run market_price_authentication.go [--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]```

```go run market_price_posting.go [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

Pressing the CTRL+C buttons terminates any example.
## Compiling Source
### Windows/macOS
1. __Install Go__
    - Go to: <https://golang.org/dl/>
    - Follow installation instructions for your machine
    - Test installation as instructed on website
2. __Install libraries__
    - Run:
      - `go get github.com/gorilla/websocket`
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/go`
    - To run `market_price.go` with options:
      - `go run market_price.go --hostname <hostname>`

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
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/go`
    - To run `market_price.go` with options:
      - `go run market_price.go --hostname <hostname>`

## Source File Description

* `market_price.go` - Source file for the market\_price example.

* `market_price_batch_view.go` - Source file for the market\_price\_batch\_view example.

* `market_price_authentication.go` - Source file for the market\_price\_authentication example.

* `market_price_posting.go` - Source file for the market\_price example.

* `market_price_ping.go` - Source file for the market\_price\_ping example.
