//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright (C) Refinitiv 2019. All rights reserved.              --
//|-----------------------------------------------------------------------------


import com.neovisionaries.ws.client.WebSocket;
import com.neovisionaries.ws.client.WebSocketAdapter;
import com.neovisionaries.ws.client.WebSocketException;
import com.neovisionaries.ws.client.WebSocketExtension;
import com.neovisionaries.ws.client.WebSocketFactory;
import com.neovisionaries.ws.client.WebSocketFrame;

import java.io.IOException;
import java.net.Inet4Address;
import java.net.UnknownHostException;
import java.util.List;
import java.util.Map;

import org.json.*;
import org.apache.commons.cli.*;

public class MarketPrice {
	
    public static String server;
    public static String hostname = "127.0.0.1";
    public static String port = "15000";
    public static String user = "root";
    public static String position = "";
    public static String appId = "256";
    public static WebSocket ws = null;

	public static void main(String[] args) {
		
		Options options = new Options();
		
        options.addOption(Option.builder().longOpt("hostname").hasArg().desc("hostname").build());
        options.addOption(Option.builder().longOpt("port").hasArg().desc("port").build());
        options.addOption(Option.builder().longOpt("app_id").hasArg().desc("app_id").build());
        options.addOption(Option.builder().longOpt("user").hasArg().desc("user").build());
        options.addOption(Option.builder().longOpt("position").hasArg().desc("position").build());
		options.addOption(Option.builder().longOpt("help").desc("help").build());
		
        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        CommandLine cmd;

        try {
            cmd = parser.parse(options, args);
        } catch (ParseException e) {
            System.out.println(e.getMessage());
            formatter.printHelp("MarketPrice", options);
            System.exit(1);
            return;
        }

        if(cmd.hasOption("help"))
        {
        	 formatter.printHelp("MarketPrice", options);
        	 System.exit(0);
        }
		if(cmd.hasOption("hostname"))
        	hostname = cmd.getOptionValue("hostname");
        if(cmd.hasOption("port"))
        	port = cmd.getOptionValue("port");
        if(cmd.hasOption("app_id"))
        	appId = cmd.getOptionValue("app_id");
        if(cmd.hasOption("user"))
        	user = cmd.getOptionValue("user");
        if(cmd.hasOption("position"))
        {
        	position = cmd.getOptionValue("position");
        }
        else
        {
    		try {
				position = Inet4Address.getLocalHost().getHostAddress();
			} catch (UnknownHostException e) {
				e.printStackTrace();
			}
        }

        server = String.format("ws://%s:%s/WebSocket", hostname, port);
        System.out.println("Connecting to WebSocket " + server + " ...");
        try {
			ws = connect();
			
			while(true) {
                Thread.sleep(1000);
            }
		} catch (IOException e) {
			e.printStackTrace();
		} catch (WebSocketException e) {
			e.printStackTrace();
		} catch (InterruptedException e) {
            e.printStackTrace();
		}
	}
	
    /**
     * Connect to the server.
     */
	public static WebSocket connect() throws IOException, WebSocketException
    {
        return new WebSocketFactory()
                //.setConnectionTimeout(TIMEOUT)
                .createSocket(server)
                .addProtocol("tr_json2")
                .addListener(new WebSocketAdapter() {

                	/**
                	 * Called when message received, parse message into JSON for processing
                	 */
                    public void onTextMessage(WebSocket websocket, String message) throws JSONException {
                        if(!message.isEmpty()) {
                        	System.out.println("RECEIVED:");

                            JSONArray jsonArray = new JSONArray(message);

                            System.out.println(jsonArray.toString(2));

                            for (int i = 0; i < jsonArray.length(); ++i)
                                processMessage(websocket, jsonArray.getJSONObject(i));
                        }
                    }

                    /**
                     * Called when handshake is complete and websocket is open, send login
                     */
                    public void onConnected(WebSocket websocket, Map<String, List<String>> headers) throws JSONException {
                    	System.out.println("WebSocket successfully connected!");
                        sendLoginRequest(websocket);
                    }
                })
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE)
                .connect();
    }

	/**
	 * Generate a login request from command line data (or defaults) and send
	 * @param websocket
	 * @throws JSONException
	 */
    public static void sendLoginRequest(WebSocket websocket) throws JSONException {
        String loginJsonString = "{\"ID\":1,\"Domain\":\"Login\",\"Key\":{\"Name\":\"\",\"Elements\":{\"ApplicationId\":\"\",\"Position\":\"\"}}}";
        JSONObject loginJson = new JSONObject(loginJsonString);
		loginJson.getJSONObject("Key").put("Name", user);
        loginJson.getJSONObject("Key").getJSONObject("Elements").put("ApplicationId", appId);
        loginJson.getJSONObject("Key").getJSONObject("Elements").put("Position", position);
        websocket.sendText(loginJson.toString());
        System.out.println("SENT:\n" + loginJson.toString(2));
    }

    /**
     * Parse at high level and output JSON of message
     * @param websocket
     * @param messageJson
     * @throws JSONException
     */
    public static void processMessage(WebSocket websocket, JSONObject messageJson) throws JSONException {
        String messageType = messageJson.getString("Type");

        switch(messageType)
        {
            case "Refresh":
                if(messageJson.has("Domain")) {
                    String messageDomain = messageJson.getString("Domain");
                    if(messageDomain.equals("Login")) {
                        sendRequest(websocket);
                    }
                }
                break;

            case "Ping":
                String pongJsonString = "{\"Type\":\"Pong\"}";
                JSONObject pongJson = new JSONObject(pongJsonString);
                websocket.sendText(pongJsonString);
                System.out.println("SENT:\n" + pongJson.toString(2));
                break;
            default:
                break;
        }
    }

    /**
     * Create and send simple Market Price request
     * @param websocket
     * @throws JSONException
     */
    public static void sendRequest(WebSocket websocket) throws JSONException {
        String requestJsonString;
        requestJsonString = "{\"ID\":2,\"Key\":{\"Name\":\"TRI.N\"}}";
        JSONObject mpRequestJson = new JSONObject(requestJsonString);
        websocket.sendText(requestJsonString);
        System.out.println("SENT:\n" + mpRequestJson.toString(2));
    }
}
