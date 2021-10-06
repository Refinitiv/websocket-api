# Ruby Examples

Example Code Disclaimer:
ALL EXAMPLE CODE IS PROVIDED ON AN “AS IS” AND “AS AVAILABLE” BASIS FOR ILLUSTRATIVE PURPOSES ONLY. REFINITIV MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED, AS TO THE OPERATION OF EXAMPLE CODE, OR THE INFORMATION, CONTENT OR MATERIALS USED IN CONNECTION WITH EXAMPLE CODE. YOU EXPRESSLY AGREE THAT YOUR USE OF EXAMPLE CODE IS AT YOUR SOLE RISK

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

```ruby market_price.rb [--hostname hostname] [--port port] [--app_id appID] [--user user] [--snapshot]```

```ruby market_price_batch_view.rb [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

```ruby market_price_authentication.rb [--hostname hostname ] [--port port] [--app_id appID] [--user user] [--password password] [--auth_port port]```

```ruby market_price_posting.rb [--hostname hostname ] [--port port] [--app_id appID] [--user user]```

Pressing the CTRL+C buttons terminates any example.
## Compiling Source
### Windows
1. __Install Ruby__
    - Go to: <https://rubyinstaller.org/downloads/>
    - Select the latest release under __RubyInstallers__
    - Run the downloaded `rubyinstaller-<version>.exe` and follow installation instructions
    - Add `C:\Ruby23\bin` (or path you decided during installation) to your __PATH__ environment 
      variable
    - From the same page <https://rubyinstaller.org/downloads/>, scroll to __DEVELOPMENT KIT__ and 
      download the exe "For use with Ruby 2.0 and above (__32bits__ version only)"
      - Do not run the exe yet
      - __IMPORTANT NOTE__: Do not download the 64 bit one, even if your machine is 64 bit. This
        one has some bugs in it. 32 will work just fine and is free from any bugs that will
        affect you
    - Create a directory called `C:\Ruby23\DevKit`
    - Now run the `DevKit...exe` file you just downloaded, when asked where to extract, use
      the `C:\Ruby23\DevKit` directory you just created as the destination
    - Now `cd` into `C:\Ruby23\DevKit` from a command prompt
    - Run:
      - `ruby dk.rb init`
      - `ruby dk.rb review`
      - `ruby dk.rb install`
2. __Install libraries__
    - Run:
      - `gem install websocket-client-simple`
      - `gem install ipaddress`
      - `gem install http`
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/ruby`
    - Command to run `market_price.rb` with options:
      - `ruby market_price.rb --hostname <hostname>`

### Linux/macOS
1. __Install Ruby__
    - Go to: <https://www.ruby-lang.org/en/>
    - Follow installation instructions of Ruby for your system
2. __Install libraries__
    - Run:
      - `gem install websocket-client-simple`
      - `gem install ipaddress`
      - `gem install http`
3. __Run examples__
    - `cd` to `streamingtools/Applications/Examples/ruby`
    - Command to run `market_price.rb` with options:
      - `ruby market_price.rb --hostname <hostname>`

## Source File Description

* `market_price.rb` - Source file for the market\_price example.

* `market_price_batch_view.rb` - Source file for the market\_price\_batch\_view example.

* `market_price_authentication.rb` - Source file for the market\_price\_authentication example.

* `market_price_posting.rb` - Source file for the market\_price\_posting example.

* `market_price_ping.rb` - Source file for the market\_price\_ping example.
