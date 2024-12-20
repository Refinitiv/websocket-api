# Websocket API for Pricing Streaming and Real-Time Services #

This API consists of a protocol specification. Also included is a set of example programs used to illustrate implementation of the protocol to make websocket connections to LSEG's Real-Time Distribution Systems, and, to Real-Time -- Optimized (RTO). RTO examples authenticate via LSEG's Delivery Platform (LDP). These examples are built using certain widely available Websocket frameworks and follow the [protocol specification](https://github.com/Refinitiv/websocket-api/blob/master/WebsocketAPI_ProtocolSpecification.pdf) to demonstrate how to setup a websocket connection and use message constructs to receive Real-Time content. This API is governed by the same Apache 2 open source license as defined in the LICENSE.md file.

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. LSEG MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

## What's New

This release rebrands both the specification and examples to LSEG.

The prior release introduced oAuthClientCredentials authentication in Real-Time - Optimized (RTO) examples. A new RTO example written in Go has been added with support for oAuthClientCredentials. Please note that oAuthClientCredentials authentication works with Service Account credentials.

## Real-Time Distribution System Examples
These examples demonstrate the following usage:

* Connecting, logging in, and requesting a single item
* Requesting multiple items with a view in one message via a Batch Request
* Posting content to an item
* Logging in via Authentication
* Monitoring connection health via Ping messages

The examples are found under the Applications/Examples folder. Examples are provided in the following languages:

* C#
* Java
* Python
* Perl
* Node.js
* Go
* R
* Ruby

More details for each example are included in a __README.md__ file in its respective folder.

## Real-Time - Optimized Examples
These are examples written in Python, CSharp, Go and Java that demonstrate consuming content from Real-Time - Optimized (RTO).

The examples are found in the Applications/Examples/RTO folder with language specific sub-folders. More details are included in a __README.md__ file in each example folder.

* Authenticating via LDP, Connecting to a Real-Time service available in RTO, and requesting a single item; examples handle session management or abiltiy to re-authenticate to renew authentication tokens
* Discover which RTO endpoint to connect to by making a service discovery request to RDP and using this information to connect to RTO to receive Real-Time content.

IMPORTANT NOTE: When specifying a 'region' as input to LDP Service Discovery applications, please consult [RTO documentation available on LSEG Developer Community] (https://developers.lseg.com/en/api-catalog/real-time-opnsrc/websocket-api/documentation) for a valid list of available regions. Default has been changed to "us-east-1" for region in sample applications starting with tag WSA-1.1.7.


# Contributing
In the event you would like to contribute to this repository, it is required that you read and sign the following:

- [Individual Contributor License Agreement](https://github.com/Refinitiv/websocket-api/blob/master/Individual%20Contributor%20License%20Agreement.pdf)
- [Entity Contributor License Agreement](https://github.com/Refinitiv/websocket-api/blob/master/Entity%20Contributor%20License%20Agreement.pdf)

Please email a signed and scanned copy to `sdkagreement@lseg.com`. If you require that a signed agreement has to be physically mailed to us, please email the request for a mailing address and we will get back to you on where you can send the signed documents.

Documentation for the WebSocket API and a Question & Answer forum are available at the [WebSocket API Section of the LSEG Developer Community](https://developers.lseg.com/en/api-catalog/real-time-opnsrc/websocket-api). 

# Support SLA
Issues raised via GitHub will be addressed in a best-effort manner. For broad questions regarding Websocket API, please refer to documentation and [Q&A forum on Developer Community](https://community.developers.lseg.com/index.html) which is supported by an existing active community of API users.
