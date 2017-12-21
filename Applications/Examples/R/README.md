# R Examples
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

```RScript market_price.R [--hostname hostname] [--port port] [--app_id appID] [--user user]```

```RScript market_price_batch_view.R [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

```RScript market_price_authentication.R [--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]```

```RScript market_price_posting.R [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

Pressing the CTRL+C buttons terminates any example.
## Compiling Source

### Windows/macOS
1. __Install R__
    - Go to: <https://www.r-project.org/about.html>
    - Follow the installation instructions for your machine
2. __Install libraries__
    - Run "R" and in the prompt, enter:
      - `install.packages("jsonlite")`
      - `install.packages("curl")`
      - `install.packages("devtools")`
      - `install.packages("GetoptLong")`
      - `library(devtools)`
      - `install_github("brettjbush/R-Websockets")`
    - (A GUI window will open for you to choose a mirror to download from)
    - (You may be asked to create a personal package folder if you're not running as root)
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/R`
    - Command to run `market_price.R` with options:
      - `RScript market_price.R --hostname <hostname>`

### RedHat/Oracle Linux
1. __Install R via Yum__
    - Run:
      - `yum install R --nogpgcheck`
      - If this package is not available on your system, it may be available in one of the following repositories:
          - (RedHat 6, Oracle 6) Extra Packages for Enterprise Linux (<https://fedoraproject.org/wiki/EPEL>): 
            `rpm -Uvh http://download.fedoraproject.org/pub/epel/6/i386/epel-release-6-8.noarch.rpm`
		  - (Oracle 6, Oracle 7) Download the appropriate repository file for the system and enable the latest/addons repositories, as described here: <https://docs.oracle.com/cd/E37670_01/E37355/html/ol_downloading_yum_repo.html>
	- Install Yum packages "openssl-devel" and "libcurl-devel" (required by the R libraries to be installed in the next step):
      - `yum install openssl-devel` 
      - `yum install libcurl-devel`
      
2. __Install libraries__
    - Run "R" and in the prompt, enter:
      - `install.packages("jsonlite")`
      - `install.packages("curl")`
      - `install.packages("devtools")`
      - `install.packages("GetoptLong")`
      - `library(devtools)`
      - `install_github("brettjbush/R-Websockets")`
    - (A GUI window will open for you to choose a mirror to download from)
    - (You may be asked to create a personal package folder if you're not running as root)
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/R`
    - Command to run `market_price.R` with options:
      - `RScript market_price.R --hostname <hostname>`

## Source File Description

* `market_price.R` - Source file for the market\_price example.

* `market_price_batch_view.R` - Source file for the market\_price\_batch\_view example.

* `market_price_authentication.R` - Source file for the market\_price\_authentication example.

* `market_price_posting.R` - Source file for the market\_price\_posting example.

* `market_price_ping.R` - Source file for the market\_price\_ping example.
