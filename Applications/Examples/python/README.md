# Python Examples
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

NOTE: The posting example must be run with Python3.0 or greater. The other examples may be
run with Python2.7 or greater.

## Command Line Usage

```python market_price.py [--hostname hostname] [--port port] [--app_id appID] [--user user]```

```python market_price_batch_view.py [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

```python market_price_authentication.py [--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]```

```python market_price_posting.py [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

Pressing the CTRL+C buttons terminates any example.
## Compiling Source
### Windows/Linux/macOS
1. __Install Python__
    - Go to: <https://www.python.org/downloads/>
    - Select the __Download tile__ for the Python 3 version
    - Run the downloaded `python-<version>` file and follow installation instructions
2. __Install libraries__
    - Run (in order):
      - `pip install requests`
      - `pip install websocket-client`
	    **The websocket-client must be version 0.48**
      - (Only for Python versions less than 3.3) `pip install ipaddress` 
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/python`
    - To run `market_price.py` with options:
      - `python market_price.py --hostname <hostname>`

## Source File Description
* `market_price.py` - Source file for the market\_price example.

* `market_price_batch_view.py` - Source file for the market\_price\_batch\_view example.

* `market_price_authentication.py` - Source file for the market\_price\_authentication example.

* `market_price_posting.py` - Source file for the market\_price example.

* `market_price_ping.py` - Source file for the market\_price\_ping example.
