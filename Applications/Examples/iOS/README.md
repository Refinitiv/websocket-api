# iOS Example
## Summary

The purpose of this application is to show retrieving JSON-formatted market content
from a WebSocket server. The application functions are as follows:

* __MarketPrice__: Retrieves market-price content for TRI.N.
* __MarketPriceBatchView__: Retrieves market-price content for TRI.N, IBM.N, and T.N,
  using a batch request. It also specifies a view that requests content only for fields
  BID, ASK, and BIDSIZE.
* __MarketPriceAuthentication__: Retrieves market-price content for TRI.N, after
  authenticating with an authentication server.
* __MarketPricePosting__: Retrieves market-price content for TRI.N, and posts
  market-price content back to it.

This application is intended as a basic usage example. Some of the design choices were made to favor simplicity and readability over performance. This application is not intended to be used for measuring performance. This application makes use of CocoaPods for dependency management, Starscream to handle
the WebSocket protocol, and SwiftyJSON to handle JSON data in Swift- each package is included as
a part of the iOS example already.

## Running Application
### macOS
1. __Install iOS__
  - Go to: <https://developer.apple.com/xcode/> 
  - Download Xcode version 8 or later
  - Follow installation instructions
2. __Running Project__
  - Open Applications/Examples/iOS/iOS.xcworkspace in Xcode
  - Select the Run (Play Arrow) button in the top left hand corner
  - Wait for Simulator window to load
  - When app opens, pretend your mouse is your finger and interact with emulator like it is a normal phone
  - Delete the default hostname and enter your desired hostname
  - Select Go to run and view the output in text area in bottom portion of screen
  - Other buttons and functions act as one would expect

