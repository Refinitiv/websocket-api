# Java Elektron Real-Time Example
## Summary

This example demonstrates authenticating via the Elektron Real-Time Service and
Elektron Data Platform Gateway, and logging in with the retrieved token to
retrieve market content.

The example first sends an HTTP request to the EDP Gateway, using the specified
username and password. The Gateway provides an authentication token in
response.

The example then opens a WebSocket to the Elektron Real-Time Service at the
specified host, logs in using the authentication token, then retrieves market
content.

The example periodically retrieves new authentication tokens, using a refresh
token included in the response from the Gateway instead of the username and
password. Once the new token is retrieved, it sends a login request with this
token over its WebSocket to the Elektron Real-Time Service.

## Setup and Run Example
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
	- Ant should download the dependent libraries via Ivy, and compile the example.
    - NOTE: When finished, the build conveniently prints a classpath for use when running the 
      examples.
3. __Running The Example__
    - Set the classpath that Ant gave you and run:
    - `set CLASSPATH=<classpath from ant>`
    - `java MarketPriceEdpGwAuthentication --user <username> --password <password> --hostname <Elektron Real-Time Service host>`
    - Pressing the CTRL+C buttons terminates the example.

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
3. __Running The Example__
    - Set the classpath that Ant gave you and run:
    - `export CLASSPATH=<classpath from ant>`
    - `java MarketPriceEdpGwAuthentication --user <username> --password <password> --hostname <Elektron Real-Time Service host>`
    - Pressing the CTRL+C buttons terminates the example.

### Commandline Option Descriptions

Option           |Description|
----------------:|-----------|
`--auth_hostname`| OPTIONAL. Hostname of the EDP Gateway. Defaults to api.edp.thomsonreuters.com.
`--auth_port`    | OPTIONAL. Port of the EDP Gateway. Defaults to 443.
`--hostname`     | REQUIRED. Hostname of the Elektron Real-Time Service.
`--port`         | OPTIONAL. Port of the Elektron Real-Time Service. Defaults to 443.
`--user`         | REQUIRED. Username to use when authenticating via Username/Password to the Gateway.
`--password`     | REQUIRED. Password to use when authenticating via Username/Password to the Gateway.
`--scope`        | OPTIONAL. An authorization scope to include when authenticating. Defaults to 'trapi'.
`--ric`          | OPTIONAL. Name of the item to request from the Elektron Real-Time Service. If not specified, TRI.N is requested.
`--app_id`       | OPTIONAL. Application ID to use when logging in. If not specified, "256" is used.
`--position`     | OPTIONAL. Position to use when logging in. If not specified, the current host is used.


## Source File Description

* `MarketPriceEdpGwAuthentication.java` - Source file for the MarketPriceEdpGwAuthentication example.

