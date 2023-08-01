# Java Refinitiv Data Platform Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## Summary


The purpose of these examples is to connect to Refinitiv Real-Time - Optimized (RTO) to
retrieve JSON-formatted market content over a Websocket connection from a 
Refinitiv Real-Time Service after authenticating via Refinitiv Data Platform (RDP). 

The examples are:

* __MarketPriceRdpGwAuthentication__:  Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. The content is retrieved using endpoint information (host and port)
  supplied in input. This example maintains a session by proactively renewing access_token
  token before expiration. The Authentication is 'oAuthPasswordGrant' or RDP version1 (v1) auth
  which uses password grant or refresh_token grant with Refinitiv provided Machine Account 
  credentials: username and password. Also required is clientid which is 
  generated using AppGenerator tool.

* __MarketPriceRdpGwServiceDiscovery__: Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v1/token) and using obtained tokens to keep
  the connection alive. This example discovers the endpoint information using a service
  discovery URL using a region supplied as input. The content is retrieved using
  this endpoint information. This example maintains a session by proactively renewing
  authentication token before expiration. The Authentication is 'oAuthPasswordGrant' 
  or RDP version1 (v1) auth which uses password grant or refresh_token grant with Refinitiv
  provided Machine Account credentials: username and password. Also required is clientid which is 
  generated using AppGenerator tool.

__IMPORTANT NOTE__ regarding the following example, MarketPriceRdpGwClientCredAuth: 
Version 2 authentication example is available as Early Access to API developers 
to preview changes required to use this new authentication mechanism. Please note that 
ability to setup Service Accounts to use this authentication is forthcoming.

* __MarketPriceRdpGwClientCredAuth__: Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with Refintiv Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to Refinitiv Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthClientCred' or RDP version2 (v2) auth which uses client credentials grant
  with Refintiv provided Service Account credentials: clientid (username) and clientsecret (password). 

__IMPORTANT NOTE__ regarding the following example, MarketPriceRdpGwJwtAuth: 
Version 2 authentication example is available as Early Access to API developers 
to preview changes required to use this new authentication mechanism. Please note that 
ability to setup Service Accounts to use this authentication is forthcoming.

* __MarketPriceRdpGwJwtAuth__: Retrieves market-price content for a RIC after
  authenticating with RDP (auth/oauth2/v2/token). The obtained access token is used in inital 
  authentication with Refintiv Real-Time - Optimized (RTO). New tokens are obtained if 
  reconnecting to Refinitiv Real-Time Server. During reconnection attempts, a new token 
  is obtained only if existing token has expired. This example connects to a specified 
  endpoint (host and port) or if unspecified will discover the endpoint information 
  using a service discovery URL using a region supplied as input. The Authentication 
  is 'oAuthJwt' or RDP version2 (v2) auth which uses client credentials grant
  with Refintiv provided Service Account credentials: clientid (username) and JWT (JSON Web Token). 

These applications are intended as sample examples. Some of the design choices
were made to favor simplicity and readability over performance. These applications
are not intended to be used for measuring performance.


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

### Running the MarketPriceRdpGwAuthentication Example

To run the example:
  - Set the classpath that Ant gave you:
	- `set CLASSPATH=<classpath from ant>` (Windows)
    - `export CLASSPATH=<classpath from ant>` (Linux)
  - Run: `java MarketPriceRdpGwAuthentication --user <username> --password <password> --clientid <clientid> --hostname <hostname>`
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating to Refinitiv Data Platform
`--hostname`      | REQUIRED. Hostname of the Refinitiv Real-Time Service.
`--password`      | REQUIRED. Machine Account Password to use when authenticating to Refinitiv Data Platform.
`--user`          | REQUIRED. Machine Account Username to use when authenticating to Refinitiv Data Platform.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via Refinitiv Data Platform. Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--newPassword`   | OPTIONAL. New password provided by user to change password to.
`--port`          | OPTIONAL. Port of the Refinitiv Real-Time Service. Defaults to 443.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested Real-Time service name or service ID. Defaults to ELEKTRON_DD.

NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description

* `MarketPriceRdpGwAuthentication.java` - Source file for the MarketPriceRdpGwAuthentication example.

### Running the MarketPriceRdpGwServiceDiscovery Example

To run the example:
  - Set the classpath that Ant gave you: `export CLASSPATH=<classpath from ant>`
  - Run: `java MarketPriceRdpGwServiceDiscovery --user <username> --password <password> --clientid <clientid>`
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option            |Description|
-----------------:|-----------|
`--clientid`      | REQUIRED. Client ID aka AppKey generated using AppGenerator, to use when authenticating to Refinitiv Data Platform.
`--password`      | REQUIRED. Machine Account Password to use when authenticating to Refinitiv Data Platform.
`--user`          | REQUIRED. Machine Account Username to use when authenticating to Refinitiv Data Platform.
`--app_id`        | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`      | OPTIONAL. URL of authentication via Refinitiv Data Platform. Defaults to https://api.refinitiv.com:443/auth/oauth2/v1/token.
`--discovery_url` | OPTIONAL. URL of Service Discovery via Refinitiv Data Platform. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hotstandby`    | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false. 
`--newPassword`   | OPTIONAL. New password provided by user to change password to.
`--position`      | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--region`        | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.
`--ric`           | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`         | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`       | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.
NOTE about newPassword: Acceptable passwords may be 15 characters long and have a mix of letters (upper/lower), numbers and special characters.

#### Source File Description

* `MarketPriceRdpGwServiceDiscovery.java` - Source file for the MarketPriceRdpGwServiceDiscovery example.

### Running the MarketPriceRdpGwClientCredAuth Example

To run the example:
  - Set the classpath that Ant gave you: `export CLASSPATH=<classpath from ant>`
  - Run: `java MarketPriceRdpGwClientCredAuth --clientid <clientid> --clientsecret <clientsecret> --hostname <hostname>` 
  - Pressing the CTRL+C buttons terminates the example.

The command line options are:

Option              |Description|
-------------------:|-----------|
`--clientid`        | REQUIRED. Service Account ClientID to use when authenticating to Refinitiv Data Platform.
`--clientsecret`    | REQUIRED. Service Account ClientSecret to use when authenticating to Refinitiv Data Platform.
`--app_id`          | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`        | OPTIONAL. V2 URL for authentication via Refinitiv Data Platform. Defaults to https://api.refinitiv.com:443/auth/oauth2/v2/token.
`--discovery_url`   | OPTIONAL. URL of Service Discovery via Refinitiv Data Platform. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hostname`        | OPTIONAL. Hostname of the Refinitiv Real-Time Service. If unspecified, service discovery will be used.
`--standbyhostname` | OPTIONAL. Hostname of secondary endpoint in RTO to use for Hot StandBy feature.
`--hotstandby`      | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false. 
`--port`            | OPTIONAL. Port of the Refinitiv Real-Time Service. Defaults to 443.
`--standbyport`     | OPTIONAL. Port of the secondary endpoint in RTO to use for Hot StandBy feature. Defaults to 443.
`--position`        | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--region`          | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.
`--ric`             | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`           | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`         | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.

#### Source File Description

* `MarketPriceRdpGwClientCredAuth.java` - Source file for the MarketPriceRdpGwClientCredAuth example.

### Running the MarketPriceRdpGwJwtAuth Example

To run the example:
  - Set the classpath that Ant gave you: `export CLASSPATH=<classpath from ant>`
  - Run: `java MarketPriceRdpGwJwtAuth --clientid <clientid> --jwkFile <client JWK> --hostname <hostname>` 
  - Pressing the CTRL+C buttons terminates the example.
  - To turn on debug, specify appropriate loglevel in logback.xml 

The command line options are:

Option              |Description|
-------------------:|-----------|
`--clientid`        | REQUIRED. Service Account ClientID to use when authenticating to Refinitiv Data Platform.
`--jwkFile`         | REQUIRED. Service Account JWK file to sign JWT when authenticating to Refinitiv Data Platform.
`--aud`             | OPTIONAL. JWT Audience to use when authenticating to Refinitiv Data Platform. Defaults to https://login.ciam.refinitiv.com/as/token.oauth2.
`--app_id`          | OPTIONAL. Application ID to use when logging in. Defaults to 256.
`--auth_url`        | OPTIONAL. V2 URL for authentication via Refinitiv Data Platform. Defaults to https://api.refinitiv.com:443/auth/oauth2/v2/token.
`--discovery_url`   | OPTIONAL. URL of Service Discovery via Refinitiv Data Platform. Defaults to https://api.refinitiv.com/streaming/pricing/v1/.
`--hostname`        | OPTIONAL. Hostname of the Refinitiv Real-Time Service. If unspecified, service discovery will be used.
`--standbyhostname` | OPTIONAL. Hostname of secondary endpoint in RTO to use for Hot StandBy feature.
`--hotstandby`      | OPTIONAL. Indicates whether or not the example operates in hot standby mode. Defaults to false. 
`--port`            | OPTIONAL. Port of the Refinitiv Real-Time Service. Defaults to 443.
`--standbyport`     | OPTIONAL. Port of the secondary endpoint in RTO to use for Hot StandBy feature. Defaults to 443.
`--position`        | OPTIONAL. Position to use when logging in. If not specified, the current host is used.
`--region`          | OPTIONAL. Specifies a region to get endpoint(s) from the service discovery. Default is "us-east-1". See RTO documentation for all valid regions.
`--ric`             | OPTIONAL. Symbol used in price server request. Defaults to /TRI.N.
`--scope`           | OPTIONAL. Identifier for a resource name. Defaults to trapi.streaming.pricing.read.
`--service`         | OPTIONAL. The requested service name or service ID. Defaults to ELEKTRON_DD.

NOTE about hotstandby: Specifies the hotstandby mechanism to create two connections and subscribe identical items for service resiliency.

#### Source File Description

* `MarketPriceRdpGwJwtAuth.java` - Source file for the MarketPriceRdpGwJwtAuth example.
