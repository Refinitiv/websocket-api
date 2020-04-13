#|-----------------------------------------------------------------------------
#|            This source code is provided under the Apache 2.0 license      --
#|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
#|                See the project's LICENSE.md for details.                  --
#|           Copyright (C) 2019-2020 Refinitiv. All rights reserved.         --
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
$ping_timeout_interval = 30;
$ping_timeout_time = 0;
$ping_send_time = 0;
$web_socket_open = 0;

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
				$ping_timeout_interval = int($message_json->{'Elements'}{'PingTimeout'});
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

		$ping_timeout_time = 0;
		$ping_send_time = Time::HiRes::time() + $ping_timeout_interval / 3;
	});

	print "WebSocket successfully connected!\n";
	$web_socket_open = 1;
	$ping_timeout_time = 0;
	$ping_send_time = Time::HiRes::time() + $ping_timeout_interval / 3;

	Mojo::IOLoop->recurring(0 => sub {
		if($ping_send_time > 0 and Time::HiRes::time() > $ping_send_time) {
			my %ping_json_hash = (
				'Type' => 'Ping'
			);

			my $json = encode_json \%ping_json_hash;
			my $json_pretty = JSON->new->pretty->encode(\%ping_json_hash);
			$tx->send($json);
			print "SENT:\n";
			print "$json_pretty\n";

			$ping_send_time = 0;
			$ping_timeout_time = Time::HiRes::time() + $ping_timeout_interval;
		}
		if($ping_timeout_time > 0 and Time::HiRes::time() > $ping_timeout_time) {
			print("No ping from server, timing out\n");
			$tx->finish();
			Mojo::IOLoop->stop_gracefully;
			$web_socket_open = 0;
		}
	});
	send_login_request($tx);
});

# Event loop
Mojo::IOLoop->start unless Mojo::IOLoop->is_running;
