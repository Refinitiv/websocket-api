#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license      --
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
#|                See the project's LICENSE.md for details.                  --
#|           Copyright Thomson Reuters 2017. All rights reserved.            --
#|-----------------------------------------------------------------------------


#!/usr/bin/perl
# Simple example of outputting Market Price JSON data using Websockets with authentication

use feature qw(say);
use JSON;
use Mojo::UserAgent;
use Getopt::Long;
use Time::HiRes;
use Socket;
use Sys::Hostname;
use LWP::UserAgent;
use HTTP::Cookies;
use utf8;
use Encode;

# Global Default Variables
$hostname = "127.0.0.1";
$port = "15000";
$user = "root";
$app_id = "555";
$position = inet_ntoa(scalar(gethostbyname(hostname())) || 'localhost');
$auth_hostname = "127.0.0.1";
$auth_port = "8443";
$auth_token = "";
$help = "";

# Global Variables
$web_socket_open = 0;

use sigtrap 'handler' => \&myhand, 'INT';

sub myhand
{
    kill 6, $$; # ABRT = 6
}

# Get command line parameters
GetOptions ('hostname=s' => \$hostname, 'port=s' => \$port, 'user=s' => \$user, 'app_id=s' => \$app_id, 'position=s' => \$position, 'password=s' => \$password, 'auth_hostname=s' => \$auth_hostname, 'auth_port=s' => \$auth_port, 'help' => \$help);

if(not $help eq "") {
	print "Usage: market_price_authentication.pl [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--password password] [--position position] [--auth_hostname auth_hostname] [--auth_port auth_port] [--help]\n";
	exit 0;
}

# Parse at high level and output JSON of message
sub process_message {
	my ($tx, $message_json) = @_;
	
	my $message_type = $message_json->{'Type'};
	
	if ($message_type eq "Refresh") {
		my $message_domain = $message_json->{'Domain'};
		if (not($message_domain eq "")) {
			if ($message_domain eq "Login") {
				send_market_price_request($tx);
			}
		}
	} elsif ($message_type eq "Ping") {

		my %pong_json_hash = (
			'Type' => 'Pong'
		);

		my $json = encode_json \%pong_json_hash;
		my $json_pretty = JSON->new->pretty->encode(\%pong_json_hash);
		$tx->send($json);
		print "SENT:\n";
		print "$json_pretty\n";
	}
}

# Create and send simple Market Price request
sub send_market_price_request {
	my ($tx) = @_;
	my %mp_req_json_hash = (
        'ID' => 2,
        'Key' => {
            'Name' => 'TRI.N',
        },
	);

	my $json = encode_json \%mp_req_json_hash;
	my $json_pretty = JSON->new->pretty->encode(\%mp_req_json_hash);
	$tx->send($json);
	print "SENT:\n";
	print "$json_pretty\n";
}

# Send login info for authentication token

print "Sending authentication request...\n";
use LWP::UserAgent;
my $ua = LWP::UserAgent->new;
$ua->agent("LWP::UserAgent (Perl)");
my $cookie_jar = HTTP::Cookies->new();
$ua->cookie_jar( $cookie_jar );
my $req = HTTP::Request->new(POST => "https://$auth_hostname:$auth_port/getToken");
$req->content_type('application/x-www-form-urlencoded');
$req->content("username=$user&password=$password");
$ua->ssl_opts( verify_hostname => 0 ,SSL_verify_mode => 0x00); 

# Pass request to the user agent and get a response back
my $res = $ua->request($req);
$cookie_jar->extract_cookies( $res );

# Check the outcome of the response
if ($res->is_success) {
	$res_json = decode_json $res->content;
	my $json_pretty = JSON->new->pretty->encode($res_json);
	print "RECEIVED:\n";
	print $json_pretty;
	
	if( $res_json->{'success'} ) {
	
		$cookie_jar->scan(sub  
        {  
          if ($_[1] eq "AuthToken") 
          { 
            $auth_token = $_[2];
          };
        });
		
		print("Authentication Succeeded. Received AuthToken: $auth_token\n");
		
		# Start websocket handshake
		my $ua = Mojo::UserAgent->new;
		
		$ua->on(start => sub {
		  my ($ua, $tx) = @_;
		  $tx->req->headers->header('Cookie' => "AuthToken=$auth_token;AuthPosition=$position;applicationId=$app_id;");
		});
		
		my $ws_address = "ws://$hostname:$port/WebSocket";
		print "Connecting to WebSocket $ws_address ...\n";
		$ua->inactivity_timeout(0);
		$ua->websocket($ws_address => ['tr_json2'] => sub {
			my ($ua, $tx) = @_;
			say 'WebSocket handshake failed!' and return unless $tx->is_websocket;
			say 'Subprotocol negotiation failed!' and return unless $tx->protocol;
			# Called when websocket is closed
			$tx->on(finish => sub {
				my ($tx, $code, $reason) = @_;
				say "WebSocket closed with status $code.";
			});
			# Called when message received, parse message into JSON for processing
			$tx->on(message => sub {
				my ($tx, $message) = @_;
				print "RECEIVED:\n";
				my @json_array = @{decode_json encode_utf8($message)};

				my $json_pretty = JSON->new->pretty->encode(\@json_array);
				print(encode_utf8($json_pretty));

				foreach my $single_message (@json_array) {
				process_message($tx, $single_message);
				}
			});
			
			print "WebSocket successfully connected!\n";
			$web_socket_open = 1;
		});

		# Event loop
		Mojo::IOLoop->start unless Mojo::IOLoop->is_running;
	}
	else {
		print("Authentication failed");
	}
}
else {
	print("Token request failed");
}
