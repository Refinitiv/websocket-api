# Java Refinitiv Data Platform Examples
## Summary


The purpose of these examples is to connect to the Refinitiv Data Platform to retrieve
JSON-formatted market content over a Websocket connection from a Refinitiv Real-Time Service.

The examples are:

* __MarketPriceEdpGwAuthentication__: Retrieves market-price content for a RIC after
  authenticating with an authentication server and using tokens sent by that server to keep
  the connection alive. The content is retrieved using endpoint information (host and port)
  supplied as input. This example maintains a session by proactively renewing authentication
  token before expiration.

* __MarketPriceEdpGwServiceDiscovery__: Retrieves market-price content for a RIC after
  authenticating with an authentication server and using tokens sent by that server to keep
  the connection alive. This example discovers the endpoint information using a service
  discovery URL using a region supplied as input. The content is retrieved using
  this endpoint information. This example maintains a session by proactively renewing 
  authentication token before expiration.

These applications are intended as basic usage examples. Some of the design choices
were made to favor simplicity and readability over performance. These applications
are not intended to be used for measuring performance.


## Installing and Compiling Source 
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

## Running the Examples

### Running the MarketPriceEdpGwAuthentication Example

To run the example:
  - Set the classpath that Ant gave you:
	- `set CLASSPATH=<classpath from ant>` (Windows)
    - `export CLASSPATH=<classpath from ant>` (Linux)
  - Run: `java MarketPriceEdpGwAuthentication --user <username> --password <password> --clientid <client ID> --hostname <Refinitiv Real-Time Service host>`
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating to Refinitiv Data Platform
`--hostname`      | REQUIRED. Hostname of the Refinitiv Real-Time Service.
`--password`      | REQUIRED. Password to use when authenticating to Refinitiv Data Platform.
`--user`          | REQUIRED. Username to use when authenticating to Refinitiv Data Platform.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via Refinitiv Data Platform.  Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--newPassword`   | OPTIONAL. New password provided by user to change password to.
`--port`          | OPTIONAL. Port of the Refinitiv Real-Time Service. Defaults to 443.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.
`--service`       | OPTIONAL. The requested Real-Time service name or service ID. Defaults to ELEKTRON_DD.

NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

### Running the MarketPriceEdpGwServiceDiscovery Example

To run the example:
  - Set the classpath that Ant gave you: `export CLASSPATH=<classpath from ant>`
  - Run: `java MarketPriceEdpGwServiceDiscovery --user <username> --password <password> --clientid <client ID>`
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating to Refinitiv Data Platform.
`--password`      | REQUIRED. Password to use when authenticating to Refinitiv Data Platform.
`--user`          | REQUIRED. Username to use when authenticating to Refinitiv Data Platform.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via Refinitiv Data Platform.  Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--discovery_url` | OPTIONAL. URL of Service Discovery via Refinitiv Data Platform.  Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hotstandby`    | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false. 
`--newPassword`   | OPTIONAL. New password provided by user to change password to.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery.  The region is either "amer", "emea", or "apac". Defaults to "amer".
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.
NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description

* `MarketPriceEdpGwServiceDiscovery.java` - Source file for the MarketPriceEdpGwServiceDiscovery example.
