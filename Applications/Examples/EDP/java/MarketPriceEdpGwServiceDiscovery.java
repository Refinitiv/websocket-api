//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright Thomson Reuters 2018. All rights reserved.            --
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
import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

import org.json.*;
import org.apache.commons.cli.*;
import org.apache.http.*;
import org.apache.http.client.HttpClient;
import org.apache.http.client.entity.UrlEncodedFormEntity;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.client.utils.URIBuilder;
import org.apache.http.conn.ssl.SSLConnectionSocketFactory;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.message.BasicNameValuePair;
import org.apache.http.ssl.SSLContextBuilder;
import org.apache.http.util.EntityUtils;

/** 
 * This example demonstrates authenticating via the Elektron Real-Time Service and Elektron Data Platform Gateway, using the token to query VIPs 
 * from EDP service discovery, and logging in with the retrieved token to retrieve market content.
 * It does so by:
 * - Authenticating via HTTP Post request to the Gateway
 * - Retrieving service endpoints from Service Discovery via HTTP Get request, using the token retrieved from the Gateway
 * - Opening a WebSocket (or two, if the --hotstandby option is specified) to an Elektron Real-Time Service endpoint, as retrieved from Service Discovery
 * - Logging into the Real-Time Service using the token retrieved from the Gateway
 * - Requesting market content
 * - Periodically re-authenticating to the Gateway, and providing the updated token to the Real-Time Service.
 */
public class MarketPriceEdpGwServiceDiscovery {
	
    public static String port = "443";
    public static String user = "root";

    public static String position = "";
    public static String appId = "256";
    public static String password = "";
	public static String authHostname = "api.edp.thomsonreuters.com";
    public static String authPort = "443";
    public static String ric = "/TRI.N";
    public static String scope = "trapi";
    public static JSONObject authJson = null;
    public static JSONObject serviceJson = null;
    public static List<String> hostList = new LinkedList<String>();
    public static WebSocketFactory websocketFactory = new WebSocketFactory();
    public static WebSocketSession webSocketSession1 = null;
    public static WebSocketSession webSocketSession2 = null;
    public static boolean hotstandby = false;
    public static String discoveryPath = "/streaming/pricing/v1/";

    /**
     * Class representing a session over a WebSocket.
     */ 
    public static class WebSocketSession
    {
        /** Name to use when printing messages sent/received over this WebSocket. */
        String _name;

        /** Current WebSocket associated with this session. */
        WebSocket _websocket;           

        /** URL to connect the websocket to. */
        String _url;

        /** Copy of the current authentication token. */
        String _authToken;

        /** Whether the session has successfully logged in. */
        boolean _isLoggedIn = false;
        
        /** Static map used by WebSocketAdapter callbacks to find the associated WebSocketSession object. */
        public static Map<WebSocket, WebSocketSession> webSocketSessionMap = new ConcurrentHashMap<WebSocket, WebSocketSession>();

        public WebSocketSession(String name, String host, String authToken)
        {
            _name = name;
            _url = String.format("wss://%s/WebSocket", host);
            _authToken = authToken;
            connect();
        }

        /** Connect a WebSocket (and reconnect if a previous connection failed). */
        public synchronized void connect()
        {
            if (_websocket != null)
            {
                /* Remove websocket from map, and create a new one based on the previous websocket. */
                webSocketSessionMap.remove(this);
                try {
					_websocket = _websocket.recreate();
				} catch (IOException e) {
					e.printStackTrace();
					System.exit(1);
				}
            }
            else
            {
                /* Create new websocket. */
                System.out.println("Connecting to WebSocket " + _url + " for " + _name + "...");

                try {
                    _websocket = websocketFactory.createSocket(_url)
                        .addProtocol("tr_json2")
                        .addListener(new WebSocketAdapter() {

                                /**
                                 * Called when message received, parse message into JSON for processing
                                 */
                                public void onTextMessage(WebSocket websocket, String message) throws JSONException {
                                    System.out.println("RECEIVED on " + _name +":");
                                    WebSocketSession webSocketSession = webSocketSessionMap.get(websocket);

                                    JSONArray jsonArray = new JSONArray(message);
                                    System.out.println(jsonArray.toString(2));

                                    for (int i = 0; i < jsonArray.length(); ++i)
                                        webSocketSession.processMessage(jsonArray.getJSONObject(i));
                                }

                                /**
                                 * Called when handshake is complete and websocket is open, send login
                                 */
                                public void onConnected(WebSocket websocket, Map<String, List<String>> headers) throws JSONException {
                                    System.out.println("WebSocket successfully connected for " + _name + "!");
                                    sendLoginRequest(true);
                                }

                                /**
                                 * Called when an error occurs while attempting to connect the WebSocket.
                                 */
                                public void onConnectError(WebSocket websocket, WebSocketException e)
                                {
                                    System.out.println("Connect error for " + _name + ":" + e);
                                    reconnect(websocket);
                                }

                                /**
                                 * Called when the WebSocket is disconnected.
                                 */
                                public void onDisconnected(WebSocket websocket,
                                        WebSocketFrame serverCloseFrame,
                                        WebSocketFrame clientCloseFrame,
                                        boolean closedByServer)

                                {
                                    System.out.println("WebSocket disconnected for " + _name + ".");
                                    reconnect(websocket);
                                }

                                /**
                                 * Reconnect after a delay.
                                 */
                                public void reconnect(WebSocket websocket)
                                {
                                    WebSocketSession webSocketSession = webSocketSessionMap.get(websocket);
                                    webSocketSession.isLoggedIn(false);

                                    System.out.println("Reconnecting to " + _name + " in 3 seconds...");

                                    try {
                                        Thread.sleep(3000);
                                    } catch (InterruptedException e) {
                                        e.printStackTrace();
                                        System.exit(1);
                                    }

                                    webSocketSession.connect();
                                }
                        })
                    .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE);
                } catch (IOException e) {
                    e.printStackTrace();
                    System.exit(1);
                }
            }

            webSocketSessionMap.put(_websocket, this);
            _websocket.connectAsynchronously();
        }

        /**
         * Generate a login request from command line data (or defaults) and send
         * Used for both the initial login and subsequent logins that send updated access tokens.
         * @param authToken 
         * @throws JSONException
         */
        private void sendLoginRequest(boolean isFirstLogin) throws JSONException {
            String loginJsonString = "{\"ID\":1,\"Domain\":\"Login\",\"Key\":{\"Elements\":{\"ApplicationId\":\"\",\"Position\":\"\",\"AuthenticationToken\":\"\"},\"NameType\":\"AuthnToken\"}}";
            JSONObject loginJson = new JSONObject(loginJsonString);
            loginJson.getJSONObject("Key").getJSONObject("Elements").put("AuthenticationToken", _authToken);
            loginJson.getJSONObject("Key").getJSONObject("Elements").put("ApplicationId", appId);
            loginJson.getJSONObject("Key").getJSONObject("Elements").put("Position", position);

            if (!isFirstLogin) // If this isn't our first login, we don't need another refresh for it.
                loginJson.put("Refresh", false);

            _websocket.sendText(loginJson.toString());
            System.out.println("SENT on " + _name + ": \n" + loginJson.toString(2));
        }

        /**
         * Create and send simple Market Price request
         * @throws JSONException
         */
        private void sendRequest() throws JSONException {
            String requestJsonString;
            requestJsonString = "{\"ID\":2,\"Key\":{\"Name\":\"" + ric + "\"}}";
            JSONObject mpRequestJson = new JSONObject(requestJsonString);
            _websocket.sendText(requestJsonString);
            System.out.println("SENT on " + _name + ": \n" + mpRequestJson.toString(2));
        }

        /**
         * Process a message received over the WebSocket
         * @param messageJson
         * @throws JSONException
         */
        public void processMessage(JSONObject messageJson) throws JSONException {
            String messageType = messageJson.getString("Type");

            switch(messageType)
            {
                case "Refresh":
                case "Status":
                    if(messageJson.has("Domain")) {
                        String messageDomain = messageJson.getString("Domain");
                        if(messageDomain.equals("Login")) {
                            // Check message state to see if login succeeded. If so, send item request. Otherwise stop.
                            JSONObject messageState = messageJson.optJSONObject("State");
                            if (messageState != null)
                            {
                                if (!messageState.getString("Stream").equals("Open") || !messageState.getString("Data").equals("Ok"))
                                {
                                    System.out.println("Login failed.");
                                    System.exit(1);
                                }

                                // Login succeeded, send item request.
                                isLoggedIn(true);
                                sendRequest();
                            }

                        }
                    }
                    break;

                case "Ping":
                    String pongJsonString = "{\"Type\":\"Pong\"}";
                    JSONObject pongJson = new JSONObject(pongJsonString);
                    _websocket.sendText(pongJsonString);
                    System.out.println("SENT on " + _name + ": \n" + pongJson.toString(2));
                    break;
                default:
                    break;
            }
        }

        /** 
         * Send a login request on the websocket that includes our updated access token.
         */
        public synchronized void updateToken(String updatedAuthToken)
        {
            _authToken = updatedAuthToken;

            if (!_isLoggedIn)
                return; // Websocket not connected or logged in yet. Initial login will include the access token.

            System.out.println("Refreshing the access token for " + _name);
            sendLoginRequest(false);
        }

        /**
         * Mark whether we are connected and logged in so that the updateToken method knows whether or not to 
         * reissue the login.
         */
        public synchronized void isLoggedIn(boolean isLoggedIn)
        {
            _isLoggedIn = isLoggedIn;
        }
    }

	public static void main(String[] args) {
		
		Options options = new Options();
		
        options.addOption(Option.builder().longOpt("port").hasArg().desc("port").build());
        options.addOption(Option.builder().longOpt("app_id").hasArg().desc("app_id").build());
        options.addOption(Option.builder().longOpt("user").hasArg().desc("user").build());
        options.addOption(Option.builder().longOpt("position").hasArg().desc("position").build());
        options.addOption(Option.builder().longOpt("password").hasArg().desc("password").build());
		options.addOption(Option.builder().longOpt("auth_hostname").hasArg().desc("auth_hostname").build());
        options.addOption(Option.builder().longOpt("auth_port").hasArg().desc("auth_port").build());
        options.addOption(Option.builder().longOpt("ric").hasArg().desc("ric").build());
        options.addOption(Option.builder().longOpt("scope").hasArg().desc("scope").build());
        options.addOption(Option.builder().longOpt("hotstandby").desc("hotstandby").build());
        options.addOption(Option.builder().longOpt("help").desc("help").build());

        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        CommandLine cmd;

        try {
            cmd = parser.parse(options, args);
        } catch (org.apache.commons.cli.ParseException e) {
            System.out.println(e.getMessage());
            formatter.printHelp("MarketPriceEdpGwServiceDiscovery", options);
            System.exit(1);
            return;
        }

		if(cmd.hasOption("help"))
        {
        	 formatter.printHelp("MarketPriceEdpGwServiceDiscovery", options);
        	 System.exit(0);
        }
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
                // If localhost isn't defined, use 127.0.0.1.
				position = "127.0.0.1/net";
			}
        }
        if(cmd.hasOption("ric"))
        	ric = cmd.getOptionValue("ric");
        if(cmd.hasOption("scope"))
        	scope = cmd.getOptionValue("scope");
        if(cmd.hasOption("hotstandby"))
        	hotstandby = true;
		
        try {

            // Connect to the gateway and authenticate (using our username and password)
            authJson = getAuthenticationInfo(null);
            if (authJson == null)
                System.exit(1);
            
            // Get service information.
            serviceJson = queryServiceDiscovery();
            if (serviceJson == null)
            	System.exit(1);

            // Create a host list based on the retrieved service information.
            // If failing over on disconnect, get an endpoint with two locations.
            // If opening multiple connections, get all endpoints that are in one location.
            JSONArray endpointArray = serviceJson.getJSONArray("services");
            for (int i = 0; i < endpointArray.length(); ++i)
            {
                JSONObject endpoint = endpointArray.getJSONObject(i);

                if (!hotstandby)
                {
                    if (endpoint.getJSONArray("location").length() == 2)
                    {
                        hostList.add(endpoint.getString("endpoint") + ":" + endpoint.getInt("port"));
                        break;
                    }
                }
                else
                {
                    if (endpoint.getJSONArray("location").length() == 1)
                        hostList.add(endpoint.getString("endpoint") + ":" + endpoint.getInt("port"));
                }
            }

            // Determine when the access token expires. We will re-authenticate before then.
            int expireTime = Integer.parseInt(authJson.getString("expires_in"));
			
            if (hostList.size() < 1)
            {
                System.out.println("Error: No endpoints in response.");
                System.exit(1);
            }
            else if (hotstandby && hostList.size() < 2)
            {
                System.out.println("Error: Expected 2 hosts but received " + hostList.size());
                System.exit(1);
            }
            
            // Connect WebSocket(s).
            webSocketSession1 = new WebSocketSession("session1", hostList.get(0), authJson.getString("access_token"));
            if (hotstandby)
                webSocketSession2 = new WebSocketSession("session2", hostList.get(1), authJson.getString("access_token"));
            
            while(true) {
                if (expireTime < 30)
                {
                    System.out.println("Expire time is too small, exiting.");
                    System.exit(1);
                }

                // Continue using current token until 30 seconds before it expires.
                Thread.sleep((expireTime - 30) * 1000);

                // Connect to the gateway and re-authenticate, using the refresh token provided in the previous response
                authJson = getAuthenticationInfo(authJson);
                if (authJson == null)
                    System.exit(1);

                // Send the updated access token over our WebSockets.
                webSocketSession1.updateToken(authJson.getString("access_token"));
                if (hotstandby)
                	webSocketSession2.updateToken(authJson.getString("access_token"));
            }
		} catch (JSONException e) {
			e.printStackTrace();
		} catch (InterruptedException e) {
			e.printStackTrace();
		}
	}
	
	/**
	 * Authenticate to the gateway via an HTTP post request. 
	 * Initially authenticates using the specified password. If information from a previous authentication response is provided, it instead authenticates using
	 * the refresh token from that response.
	 * @param previousAuthResponseJson Information from a previous authentication, if available
	 * @return A JSONObject containing the authentication information from the response.
	 */
	public static JSONObject getAuthenticationInfo(JSONObject previousAuthResponseJson) {
		try
		{
			SSLConnectionSocketFactory sslsf = new SSLConnectionSocketFactory(new SSLContextBuilder().build());

			String url = "https://" + authHostname + ":" + authPort + "/auth/oauth2/beta1/token";
			HttpClient httpclient = HttpClients.custom().setSSLSocketFactory(sslsf).build();
			HttpPost httppost = new HttpPost(url);

			// Set request parameters.
			List<NameValuePair> params = new ArrayList<NameValuePair>(2);
			params.add(new BasicNameValuePair("client_id", user));
			params.add(new BasicNameValuePair("username", user));
			params.add(new BasicNameValuePair("takeExclusiveSignOnControl", "true"));

			if (previousAuthResponseJson == null)
			{
				// First time through, send password.
				params.add(new BasicNameValuePair("grant_type", "password"));
				params.add(new BasicNameValuePair("password", password));
				params.add(new BasicNameValuePair("scope", scope));
				System.out.println("Sending authentication request with password to " + url + "...");

			}
			else
			{
				// Use the refresh token we got from the last authentication response.
				params.add(new BasicNameValuePair("grant_type", "refresh_token"));
				params.add(new BasicNameValuePair("refresh_token", previousAuthResponseJson.getString("refresh_token")));
				System.out.println("Sending authentication request with refresh token to " + url + "...");
			}

			httppost.setEntity(new UrlEncodedFormEntity(params, "UTF-8"));

			//Execute and get the response.
			HttpResponse response = httpclient.execute(httppost);

			if (response.getStatusLine().getStatusCode() != HttpStatus.SC_OK)
			{
				// Authentication failed.
				System.out.println("EDP-GW authentication failure: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
				System.out.println("Text: " + EntityUtils.toString(response.getEntity()));

				if (response.getStatusLine().getStatusCode() == HttpStatus.SC_UNAUTHORIZED && previousAuthResponseJson != null)
				{
					// If we got a 401 response (unauthorized), our refresh token may have expired. Try again using our password.
					return getAuthenticationInfo(null);
				}

				return null;
			}

			// Authentication was successful. Deserialize the response and return it.
			JSONObject responseJson = new JSONObject(EntityUtils.toString(response.getEntity()));
			System.out.println("EDP-GW Authentication succeeded. RECEIVED:");
			System.out.println(responseJson.toString(2));
			return responseJson;

		} catch (Exception e) {
			System.out.println("EDP-GW authentication failure:");
			e.printStackTrace();
			return null;
		}
	}

	/**
     * Retrive service information indicating locations to connect to.
	 * @return A JSONObject containing the service information.
	 */
	public static JSONObject queryServiceDiscovery() {
		try
		{
			SSLConnectionSocketFactory sslsf = new SSLConnectionSocketFactory(new SSLContextBuilder().build());

			String host =  authHostname + ":" + authPort;
			HttpClient httpclient = HttpClients.custom().setSSLSocketFactory(sslsf).build();
            HttpGet httpget = new HttpGet(new URIBuilder().setScheme("https").setHost(host).setPath(discoveryPath).setParameter("transport", "websocket").build());
            
            httpget.setHeader("Authorization", "Bearer " + authJson.getString("access_token"));
		
            System.out.println("Sending EDP-GW service discovery request to https://" + host + "/" + discoveryPath  + "...");

			//Execute and get the response.
			HttpResponse response = httpclient.execute(httpget);

			if (response.getStatusLine().getStatusCode() != HttpStatus.SC_OK)
			{
				// Discovery request failed.
				System.out.println("EDP-GW Service discovery result failure: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
				System.out.println("Text: " + EntityUtils.toString(response.getEntity()));
				return null;
			}

			// Discovery request was successful. Deserialize the response and return it.
			JSONObject responseJson = new JSONObject(EntityUtils.toString(response.getEntity()));
			System.out.println("EDP-GW Service discovery succeeded. RECEIVED:");
			System.out.println(responseJson.toString(2));
			return responseJson;

		} catch (Exception e) {
			System.out.println("EDP-GW Service discovery failure:");
			e.printStackTrace();
			return null;
		}
	}
}
