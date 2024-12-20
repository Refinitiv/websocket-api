# Perl Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. LSEG MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

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

```perl market_price.pl [--hostname hostname] [--port port] [--app_id appID] [--user user] [--snapshot]```

```perl market_price_batch_view.pl [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

```perl market_price_authentication.pl [--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]```

```perl market_price_posting.pl [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

Pressing the CTRL+C buttons terminates any example.
## Compiling Source
### Windows
1. __Install Perl__
    - Go to: <http://strawberryperl.com/>
    - Click the Recommended version for your environment to download __Strawberry Perl__
    - Run the downloaded `strawberry-perl-<version>.msi` and follow installation instructions
      - If you run into permission issues:
        - Download the latest ZIP release for your environment from: 
          <http://strawberryperl.com/releases.html>
        - Extract zip into a directory within a path that contains _no spaces_
        - `cd` to the extracted directory and run:
          - `relocation.pl.bat`
          - `update_env.pl.bat`
2. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/perl`
    - To run `market_price.pl` with options:
      - `perl market_price.pl --hostname <hostname>`

### RedHat/Oracle Linux
1. __Install perl__
    - Run (as root):
      - `yum install perl perl-CPAN`
2. __Install needed modules via CPAN (as root):__
    - Run:
	    - `cpan JSON YAML Mojolicious LWP`
    - Answer "yes" to any questions about installing dependent packages.
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/perl`
    - To run `market_price.pl` with options:
      - `perl market_price.pl --hostname <hostname>`

## Source File Description

* `market_price.pl` - Source file for the market\_price example.

* `market_price_batch_view.pl` - Source file for the market\_price\_batch\_view example.

* `market_price_authentication.pl` - Source file for the market\_price\_authentication example.

* `market_price_posting.pl` - Source file for the market\_price\_posting example.

* `market_price_ping.pl` - Source file for the market\_price\_ping example.
