#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license      --
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
#|                See the project's LICENSE.md for details.                  --
#|           Copyright (C) Refinitiv 2019. All rights reserved.              --
#|-----------------------------------------------------------------------------


#!/usr/bin/perl
# Simple example of outputting Market Price JSON data using Websockets

use feature qw(say);
use JSON;
use Mojo::UserAgent;
use Getopt::Long;
use Time::HiRes;
use Socket;
use Sys::Hostname;
use utf8;
use Encode;

# Global Default Variables
$hostname = "127.0.0.1";
$port = "15000";
$user = "root";
$app_id = "256";
$position = inet_ntoa(scalar(gethostbyname(hostname())) || 'localhost');
$help = "";

# Global Variables
$web_socket_open = 0;
$next_post_time = 0;
$post_id = 1;

use sigtrap 'handler' => \&myhand, 'INT';

sub myhand
{
    kill 6, $$; # ABRT = 6
}

# Get command line parameters
GetOptions ('hostname=s' => \$hostname, 'port=s' => \$port, 'user=s' => \$user, 'app_id=s' => \$app_id, 'position=s' => \$position, 'help' => \$help);

if(not $help eq "") {
	print "Usage: market_price.pl [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--help]\n";
	exit 0;
}

# Generate a login request from command line data (or defaults) and send
sub send_login_request {
	my ($tx) = @_;
	my %login_json_hash = (
		'ID' => 1,
		'Domain' => 'Login',
		'Key' => {
			'Name' => '',
			'Elements' => {
					'ApplicationId' => '',
					'Position' => ''
			}
		}
	);

	$login_json_hash{'Key'}{'Name'} = $user;
	$login_json_hash{'Key'}{'Elements'}{'ApplicationId'} = $app_id;
	$login_json_hash{'Key'}{'Elements'}{'Position'} = $position;
	
	my $json = encode_json \%login_json_hash;
	my $json_pretty = JSON->new->pretty->encode(\%login_json_hash);
	$tx->send($json);
	print "SENT:\n";
	print "$json_pretty\n";
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

		if ($message_json->{'ID'} == 2)
		{
			if ($next_post_time == 0 and 
					($message->{'State'} eq "" or $message->{'State'}{'Stream'} eq "Open" and $message->{'State'}{'Data'} eq "Ok")) {
				# Item stream is open. We can start posting.
				$next_post_time = Time::HiRes::time() + 3;
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


# Create and send simple Market Price post
sub send_market_price_post {
	my ($tx) = @_;

    my %mp_post_json_hash = (
        'ID' =>  2,
        'Type' => 'Post',
        'Domain' => 'MarketPrice',
		'Ack' => JSON::true,
		'PostID' => $post_id,
        'PostUserInfo' =>  {
			# Use the IP address as the Post User Address
            'Address' => $position, 

			# Use the current process ID as the Post User Id
            'UserID' => int($$)
        },
		'Message' => {
			'ID' => 0,
			'Type' => 'Update',
			'Domain' => 'MarketPrice',
			'Fields' => {'BID' => 45.55,'BIDSIZE' => 18,'ASK' => 45.57,'ASKSIZE' => 19}
		}
    );

	my $json = encode_json \%mp_post_json_hash;
	my $json_pretty = JSON->new->pretty->encode(\%mp_post_json_hash);
	$tx->send($json);
	print "SENT:\n";
	print "$json_pretty\n";

	$post_id += 1
}

# Start websocket handshake
my $ua = Mojo::UserAgent->new;
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
	Mojo::IOLoop->recurring(0 => sub {
		if($web_socket_open and $next_post_time > 0 and Time::HiRes::time() >= $next_post_time) {
			send_market_price_post($tx);
			$next_post_time = Time::HiRes::time() + 3;
		}
	});
	send_login_request($tx);
});

# Event loop
Mojo::IOLoop->start unless Mojo::IOLoop->is_running;
