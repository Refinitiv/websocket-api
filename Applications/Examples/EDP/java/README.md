# Java Elektron Real-Time Examples
## Summary

These examples demonstrate authenticating via the Elektron Real-Time (ERT)
Service and Elektron Data Platform (EDP) Gateway, and consuming market content.

The MarketPriceEdpGwAuthentication example demonstrates authenticating via an
HTTPS request to the EDP-RT Gateway using a username, clientid and password. It then
opens a WebSocket to the ERT service using the specified hostname, logs in with
the retrieved token, and requests market content.

The MarketPriceEdpGwServiceDiscovery example demonstrates authenticating via an
HTTPS request to the EDP Gateway using a username, clientid and password, and discovering
endpoints of the EDP-RT service via an HTTPS request to EDP-RT Service
Discovery.  The example then opens a WebSocket to an endpoint (or optionally
two endpoints, if the --hotstandby option is used), logs in with the retrieved
token, and requests market content.

The examples periodically retrieve new authentication tokens, using a refresh
token included in the response from the Gateway instead of the username, clientid and
password. Once the new token is retrieved, they send a login request with this
token over their WebSockets to the ERT Service.

## Setup
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
  - Run: `java MarketPriceEdpGwAuthentication --user <username> --password <password> --clientid <client ID> --hostname <Elektron Real-Time Service host>`
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--user`          | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--password`      | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating with Gateway.
`--hostname`      | REQUIRED. Hostname of the Elektron Real-Time Service.
`--auth_url`      | OPTIONAL. URL of the EDP Gateway. Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--port`          | OPTIONAL. Port of the Elektron Real-Time Service. Defaults to 443.
`--scope`         | OPTIONAL. An authorization scope to include when authenticating. Defaults to 'trapi'.
`--ric`           | OPTIONAL. Name of the item to request from the Elektron Real-Time Service. If not specified, /TRI.N is requested.
`--app_id`        | OPTIONAL. Application ID to use when logging in. If not specified, "256" is used.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.
`--newPassword`   | OPTIONAL. New password provided by user to change. Current password policy may be found at https://confluence.refinitiv.com/display/AAAH/Machine+ID+Sessions+and+Passwords+Policies.

### Running the MarketPriceEdpGwServiceDiscovery Example

To run the example:
  - Set the classpath that Ant gave you: `export CLASSPATH=<classpath from ant>`
  - Run: `java MarketPriceEdpGwServiceDiscovery --user <username> --password <password> --clientid <client ID>`
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--user`          | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--password`      | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating with Gateway.
`--hotstandby`    | OPTIONAL. Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.
`--auth_url`      | OPTIONAL. URL of the EDP Gateway. Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--discovery_url` | OPTIONAL. URL of the Service Discovery EDP Gateway. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--scope`         | OPTIONAL. An authorization scope to include when authenticating. Defaults to 'trapi'.
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. The region is either "amer", "emea", or "apac". Defaults to "amer".
`--ric`           | OPTIONAL. Name of the item to request from the Elektron Real-Time Service. If not specified, /TRI.N is requested.
`--app_id`        | OPTIONAL. Application ID to use when logging in. If not specified, "256" is used.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.
`--newPassword`   | OPTIONAL. New password provided by user to change. Current password policy may be found at https://confluence.refinitiv.com/display/AAAH/Machine+ID+Sessions+and+Passwords+Policies.

## Source File Description

* `MarketPriceEdpGwAuthentication.java` - Source file for the MarketPriceEdpGwAuthentication example.

* `MarketPriceEdpGwServiceDiscovery.java` - Source file for the MarketPriceEdpGwServiceDiscovery example.
