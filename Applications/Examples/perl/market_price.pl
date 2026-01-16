#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.
#|                See the project's LICENSE.md for details.
#|           Copyright (C) 2017-2020,2024,2026 LSEG. All rights reserved.
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
$snapshot = 0;
$service = "";
$help = "";

$updateTypeFilter = -1;		# Determines what Update Types are desired. See more details below.
$negativeUpdateTypeFilter = -1; # Determines what Update Types are NOT desired. See more details below.

#### Update Type Filters ####
# A single value, converted into binary, can be used to determine what Update Types
#    you want filtered in your responses.
# UPDATE TYPE FILTERS
# Unspecified = 0x001
# Quote = 0x002
# Trade = 0x004
# News Alert = 0x008
# Volume Alert = 0x010
# Order Indication = 0x020
# Closing Run = 0x040
# Correction = 0x080
# Market Digest = 0x100
# Quotes Trade = 0x200
# Multiple = 0x400
# Verify = 0x800

# Global Variables
$web_socket_open = 0;

use sigtrap 'handler' => \&myhand, 'INT';

sub myhand
{
    kill 6, $$; # ABRT = 6
}

# Get command line parameters
GetOptions ('hostname=s' => \$hostname, 'port=s' => \$port, 'user=s' => \$user, 'app_id=s' => \$app_id, 'position=s' => \$position, 'snapshot+' => \$snapshot, 'updateTypeFilter=i' => \$updateTypeFilter, 'negativeUpdateTypeFilter=i' => \$negativeUpdateTypeFilter, 'service=s' => \$service, 'help' => \$help);

if(not $help eq "") {
	print "Usage: market_price.pl [--hostname hostname] [--port port] [--app_id app_id] [--user user] [--position position] [--snapshot] [--updateTypeFilter] [--negativeUpdateTypeFilter] [--service] [--help]\n";
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
					'Position' => '',
			}
		}
	);

	$login_json_hash{'Key'}{'Name'} = $user;
	$login_json_hash{'Key'}{'Elements'}{'ApplicationId'} = $app_id;
	$login_json_hash{'Key'}{'Elements'}{'Position'} = $position;
	if ($updateTypeFilter ne -1) {
		$login_json_hash{'Key'}{'Elements'}{'UpdateTypeFilter'} = int($updateTypeFilter);
	}
	if ($negativeUpdateTypeFilter ne -1) {
		$login_json_hash{'Key'}{'Elements'}{'NegativeUpdateTypeFilter'} = int($negativeUpdateTypeFilter);
	}
	
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
	if (length($service) <= 0) {
		$service = 'ELEKTRON_DD'
	}
	my ($tx) = @_;
	my %mp_req_json_hash = (
		'ID' => 2,
		'Key' => {
			'Name' => 'TRI.N',
			'Service' => '',
		},
		'Streaming' => ($snapshot) >= 1 ? 0 : 1,
	);
	$mp_req_json_hash{'Key'}{'Service'} = $service;
	my $json = encode_json \%mp_req_json_hash;
	my $json_pretty = JSON->new->pretty->encode(\%mp_req_json_hash);
	$tx->send($json);
	print "SENT:\n";
	print "$json_pretty\n";
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
	send_login_request($tx);
});

# Event loop
Mojo::IOLoop->start unless Mojo::IOLoop->is_running;
