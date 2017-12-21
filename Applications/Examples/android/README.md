# Android Example
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

This application is intended as a basic usage example. Some of the design choices
were made to favor simplicity and readability over performance. This application 
is not intended to be used for measuring performance. This application uses the
neovisionaries websocket-client library to handle the WebSocket protocol, and the
package for the library is already included in the "libs" directory of the example.

## Running Application
### Windows
1. __Install Android Studio__
  - Go to: <https://developer.android.com/studio/index.html>
  - Click green "__Download Android Studio__" button
  - Follow installation instructions on the resulting webpage
  - If running into permission issues:
    - Try Run as administrator
2. __Opening Project__
  - Open __Android Studio__
  - Select Open an existing Android Studio project
  - Find and select `streamingtools/Applications/Examples/android`
3. __Running Project__
  - Click the green "__Play__" arrow at the top of the Android Studio window
  - Select a virtual device for emulator
  - When app opens, pretend your mouse is your finger and interact with emulator like it is a normal phone
  - Delete the default hostname and enter your desired hostname
  - Select __Go__ to run and view the output in text area in bottom portion of screen
  - Other buttons and functions act as one would expect
