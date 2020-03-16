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
import java.security.KeyManagementException;
import java.security.KeyStoreException;
import java.security.NoSuchAlgorithmException;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import org.json.*;
import org.apache.commons.cli.*;
import org.apache.http.*;
import org.apache.http.client.HttpClient;
import org.apache.http.client.entity.UrlEncodedFormEntity;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.conn.ssl.NoopHostnameVerifier;
import org.apache.http.conn.ssl.SSLConnectionSocketFactory;
import org.apache.http.conn.ssl.TrustSelfSignedStrategy;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.message.BasicNameValuePair;
import org.apache.http.ssl.SSLContextBuilder;
import org.apache.http.util.EntityUtils;

public class MarketPriceAuthentication {
	
    public static String server;
    public static String hostname = "127.0.0.1";
    public static String port = "15000";
    public static String user = "root";
    public static String position = "";
    public static String appId = "555";
    public static WebSocket ws = null;
    public static String authToken = "";
    public static String password = "";
	public static String authHostname = "127.0.0.1";
    public static String authPort = "8443";

	public static void main(String[] args) {
		
		System.setProperty("org.apache.commons.logging.Log", "org.apache.commons.logging.impl.SimpleLog");
		System.setProperty("org.apache.commons.logging.simplelog.log.org.apache.http", "ERROR");
		
		Options options = new Options();
		
        options.addOption(Option.builder().longOpt("hostname").hasArg().desc("hostname").build());
        options.addOption(Option.builder().longOpt("port").hasArg().desc("port").build());
        options.addOption(Option.builder().longOpt("app_id").hasArg().desc("app_id").build());
        options.addOption(Option.builder().longOpt("user").hasArg().desc("user").build());
        options.addOption(Option.builder().longOpt("position").hasArg().desc("position").build());
        options.addOption(Option.builder().longOpt("password").hasArg().desc("password").build());
		options.addOption(Option.builder().longOpt("auth_hostname").hasArg().desc("auth_hostname").build());
        options.addOption(Option.builder().longOpt("auth_port").hasArg().desc("auth_port").build());
        options.addOption(Option.builder().longOpt("help").desc("help").build());


        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        CommandLine cmd;

        try {
            cmd = parser.parse(options, args);
        } catch (org.apache.commons.cli.ParseException e) {
            System.out.println(e.getMessage());
            formatter.printHelp("MarketPriceAuthentication", options);
            System.exit(1);
            return;
        }

		if(cmd.hasOption("help"))
        {
        	 formatter.printHelp("MarketPriceAuthentication", options);
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
        if(cmd.hasOption("password"))
        	password = cmd.getOptionValue("password");
		if(cmd.hasOption("auth_hostname"))
        	authHostname = cmd.getOptionValue("auth_hostname");
        if(cmd.hasOption("auth_port"))
        	authPort = cmd.getOptionValue("auth_port");
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
		
        SSLContextBuilder builder = new SSLContextBuilder();
        try {
        	
        	System.out.println("Sending authentication request...");
        	
			builder.loadTrustMaterial(null, new TrustSelfSignedStrategy());
			SSLConnectionSocketFactory sslsf = new SSLConnectionSocketFactory(builder.build(), NoopHostnameVerifier.INSTANCE);

			HttpClient httpclient = HttpClients.custom().setSSLSocketFactory(sslsf).build();
			HttpPost httppost = new HttpPost("https://" + authHostname + ":" + authPort + "/getToken");
	
			// Request parameters and other properties.
			List<NameValuePair> params = new ArrayList<NameValuePair>(2);
			params.add(new BasicNameValuePair("username", user));
			params.add(new BasicNameValuePair("password", password));
	
			httppost.setEntity(new UrlEncodedFormEntity(params, "UTF-8"));
	
			//Execute and get the response.
			HttpResponse response = httpclient.execute(httppost);
			HttpEntity entity = response.getEntity();
			
			String authResponse = EntityUtils.toString(entity);
			
			JSONObject authJson = new JSONObject(authResponse);
			
			System.out.println("RECEIVED:");
			System.out.println(authJson.toString(2));
			
			boolean success = authJson.getBoolean("success");
			
			if(success) {
				for(Header cookieHeader : response.getHeaders("set-cookie"))
					for(HeaderElement cookieHeaderElement : cookieHeader.getElements())
						if(cookieHeaderElement.getName().equals("AuthToken"))
							authToken = cookieHeaderElement.getValue();
				
				System.out.println("Authentication Succeeded. Received AuthToken: " + authToken);
				
		        server = String.format("ws://%s:%s/WebSocket", hostname, port);
		        System.out.println("Connecting to WebSocket " + server + " ...");
				ws = connect();
				
				while(true) {
                    Thread.sleep(1000);
				}
			}
			else {
				System.out.println("Authentication failed");
			}
	
		} catch (IOException e) {
			e.printStackTrace();
		} catch (WebSocketException e) {
			e.printStackTrace();
		} catch (NoSuchAlgorithmException e1) {
			e1.printStackTrace();
		} catch (KeyStoreException e1) {
			e1.printStackTrace();
		} catch (KeyManagementException e) {
			e.printStackTrace();
		} catch (JSONException e) {
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
                .addHeader("Cookie", String.format("AuthToken=%s;AuthPosition=%s;applicationId=%s;", authToken, position, appId))
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
                    }
                })
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE)
                .connect();
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
