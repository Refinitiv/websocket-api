# Java Examples
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

```java MarketPrice [--hostname hostname] [--port port] [--app_id appID] [--user user]```

```java MarketPriceBatchView [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

```java MarketPriceAuthentication [--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]```

```java MarketPricePosting [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

Pressing the CTRL+C buttons terminates any example.
## Compiling Source
### Windows
1. __Install Ant/Ivy__
    - Install __Ant__
      - Download from <http://ant.apache.org/bindownload.cgi>
      - Follow installation instructions in package (basically, copy the unzipped folder to a location of your choice, and set __ANT\_HOME__ to that location)
    - Install __Ivy__
      - Download from <https://ant.apache.org/ivy/download.cgi>
      - Follow installation instructions in package (basically, copy the `ivy-<version>.jar` file to the `lib/` subfolder of your copy of ant)
2. __Build__
    - Run:
	    -  `ant`
	- Ant should download the dependent libraries via Ivy, and compile the examples.
    - NOTE: When finished, the build conveniently prints a classpath for use when running the 
      examples.
3. __Running Examples__
    - Set the classpath that Ant gave you and run:
    - `set CLASSPATH=<classpath from ant>`
    - `java MarketPrice --hostname <hostname>`

### RedHat/Oracle Linux
1. __Install Ant/Ivy via Yum__
    - Run (as root):
	    - `yum install ant ivy`
        - If these packages are not available on your system, they may be available in one of the following repositories:
          - (RedHat 6, Oracle 6) Extra Packages for Enterprise Linux (<https://fedoraproject.org/wiki/EPEL>): 
            `rpm -Uvh http://download.fedoraproject.org/pub/epel/6/i386/epel-release-6-8.noarch.rpm`
		  - (Oracle 6, Oracle 7) Download the appropriate repository file for the system and enable the latest/addons repositories, as described here: <https://docs.oracle.com/cd/E37670_01/E37355/html/ol_downloading_yum_repo.html>
		    
2. __Build__
    - Run:
	    -  `ant`
    - Ant should download the dependent libraries via Ivy, and compile the examples.
    - NOTE: When finished, the build conveniently prints a classpath for use when running the 
      examples.
3. __Running Examples__
    - Set the classpath that Ant gave you and run:
    - `export CLASSPATH=<classpath from ant>`
    - `java MarketPrice --hostname <hostname>`

## Source File Description

* `MarketPrice` - Source file for the MarketPrice example.

* `MarketPriceBatchView` - Source file for the MarketPriceBatchView example.

* `MarketPriceAuthentication` - Source file for the MarketPriceAuthentication example.

* `MarketPricePosting` - Source file for the MarketPricePosting example.

* `MarketPricePing` - Source file for the MarketPricePing example.
