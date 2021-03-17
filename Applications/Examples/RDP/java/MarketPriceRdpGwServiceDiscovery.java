//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|            Copyright (C) 2018-2021 Refinitiv. All rights reserved.        --
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
import org.apache.http.client.params.ClientPNames;
import org.apache.http.params.HttpParams;
import org.apache.http.params.BasicHttpParams;
import org.apache.http.conn.ssl.SSLConnectionSocketFactory;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.message.BasicNameValuePair;
import org.apache.http.ssl.SSLContextBuilder;
import org.apache.http.util.EntityUtils;

/*
 * This example demonstrates authenticating via Refinitiv Data Platform, using an
 * authentication token to discover Refinitiv Real-Time service endpoint, and
 * using the endpoint and authentitcation to retrieve market content.
 *
 * This example maintains a session by proactively renewing the authentication
 * token before expiration.
 *
 * This example can run with optional hotstandby support. Without this support, the application
 * will use a load-balanced interface with two hosts behind the load balancer. With hot standly
 * support, the application will access two hosts and display the data (should be identical) from
 * each of the hosts.
 *
 * It performs the following steps:
 * - Authenticating via HTTP Post request to Refinitiv Data Platform
 * - Retrieving service endpoints from Service Discovery via HTTP Get request,
 *   using the token retrieved from Refinitiv Data Platform
 * - Opening a WebSocket (or two, if the --hotstandby option is specified) to
 *   a Refinitiv Real-Time Service endpoint, as retrieved from Service Discovery
 * - Sending Login into the Real-Time Service using the token retrieved
 *   from Refinitiv Data Platform.
 * - Requesting market-price content.
 * - Printing the response content.
 * - Periodically proactively re-authenticating to Refinitiv Data Platform, and
 *   providing the updated token to the Real-Time endpoint before token expiration.
 */

public class MarketPriceRdpGwServiceDiscovery {

    public static String port = "443";
    public static String user = "root";
    public static String clientid = "";
    public static String position = "";
    public static String appId = "256";
    public static String password = "";
    public static String newPassword = "";
    public static String authUrl = "https://api.refinitiv.com:443/auth/oauth2/v1/token";
    public static String discoveryUrl = "https://api.refinitiv.com/streaming/pricing/v1/";
    public static String ric = "/TRI.N";
    public static String service = "ELEKTRON_DD";
    public static String scope = "trapi.streaming.pricing.read";
    public static JSONObject authJson = null;
    public static JSONObject serviceJson = null;
    public static List<String> hostList = new LinkedList<String>();
    public static WebSocketFactory websocketFactory = new WebSocketFactory();
    public static WebSocketSession webSocketSession1 = null;
    public static WebSocketSession webSocketSession2 = null;
    public static boolean hotstandby = false;
    public static String region = "amer";
    
    final private static int passwordLengthMask               = 0x1;
    final private static int passwordUppercaseLetterMask      = 0x2;
    final private static int passwordLowercaseLetterMask      = 0x4;
    final private static int passwordDigitMask                = 0x8;
    final private static int passwordSpecialCharacterMask     = 0x10;
    final private static int passwordInvalidCharacterMask     = 0x20;


    // Default password policy
    final private static int passwordLengthMin                = 30;
    final private static int passwordUppercaseLetterMin       = 1;
    final private static int passwordLowercaseLetterMin       = 1;
    final private static int passwordDigitMin                 = 1;
    final private static int passwordSpecialCharacterMin      = 1;
    final private static String passwordSpecialCharacterSet   = "~!@#$%^&*()-_=+[]{}|;:,.<>/?";
    final private static int passwordMinNumberOfCategories    = 3;

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
            requestJsonString = "{\"ID\":2,\"Key\":{\"Name\":\"" + ric + "\",\"Service\":\"" + service + "\"}}";
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
        options.addOption(Option.builder().longOpt("user").required().hasArg().desc("user").build());
        options.addOption(Option.builder().longOpt("clientid").required().hasArg().desc("clientid").build());
        options.addOption(Option.builder().longOpt("position").hasArg().desc("position").build());
        options.addOption(Option.builder().longOpt("password").required().hasArg().desc("password").build());
        options.addOption(Option.builder().longOpt("newPassword").hasArg().desc("newPassword").build());
        options.addOption(Option.builder().longOpt("auth_url").hasArg().desc("auth_url").build());
        options.addOption(Option.builder().longOpt("discovery_url").hasArg().desc("discovery_url").build());
        options.addOption(Option.builder().longOpt("ric").hasArg().desc("ric").build());
        options.addOption(Option.builder().longOpt("service").hasArg().desc("service").build());
        options.addOption(Option.builder().longOpt("scope").hasArg().desc("scope").build());
        options.addOption(Option.builder().longOpt("hotstandby").desc("hotstandby").build());
        options.addOption(Option.builder().longOpt("region").hasArg().desc("region").build());
        options.addOption(Option.builder().longOpt("help").desc("help").build());

        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        CommandLine cmd;

        try {
            cmd = parser.parse(options, args);
        } catch (org.apache.commons.cli.ParseException e) {
            System.out.println(e.getMessage());
            formatter.printHelp("MarketPriceRdpGwServiceDiscovery", options);
            System.exit(1);
            return;
        }

        if(cmd.hasOption("help"))
        {
             formatter.printHelp("MarketPriceRdpGwServiceDiscovery", options);
             System.exit(0);
        }
        if(cmd.hasOption("port"))
            port = cmd.getOptionValue("port");
        if(cmd.hasOption("app_id"))
            appId = cmd.getOptionValue("app_id");
        if(cmd.hasOption("user"))
            user = cmd.getOptionValue("user");
        if(cmd.hasOption("clientid"))
            clientid = cmd.getOptionValue("clientid");
        if(cmd.hasOption("password"))
            password = cmd.getOptionValue("password");
        if(cmd.hasOption("auth_url"))
            authUrl = cmd.getOptionValue("auth_url");
        if(cmd.hasOption("discovery_url"))
            discoveryUrl = cmd.getOptionValue("discovery_url");
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
        if(cmd.hasOption("service"))
            service = cmd.getOptionValue("service");
        if(cmd.hasOption("scope"))
            scope = cmd.getOptionValue("scope");
        if(cmd.hasOption("hotstandby"))
            hotstandby = true;
        if(cmd.hasOption("region"))
        {
            region = cmd.getOptionValue("region");
            if(!region.equals("amer") && !region.equals("emea") && !region.equals("apac"))
            {
                System.out.println("Unknown region \"" + region + "\". The region must be either \"amer\", \"emea\", or \"apac\".");
                System.exit(1);
            }
        }
        if(cmd.hasOption("newPassword")) {
        	newPassword = cmd.getOptionValue("newPassword");
        	if ((newPassword == null) || (newPassword.length() == 0)) {
        		System.out.println("Value of the option newPassword cannot be empty");
        		System.exit(1);
        	}
        	
        	int result = checkPassword(newPassword);
        	if ((result & passwordInvalidCharacterMask) != 0) {
        		System.out.println("New password contains invalid symbol(s)");
        		System.out.println("Valid symbols are [A-Z][a-z][0-9]" + passwordSpecialCharacterSet);
        		System.exit(0);
        	}
        	
        	if ((result & passwordLengthMask) != 0) {
        		System.out.println("New password length should be at least " 
        	            + passwordLengthMin
        	            + " characters");
        		System.exit(0);
        	}
        	int countCategories = 0;
        	for (int mask = passwordUppercaseLetterMask; mask <= passwordSpecialCharacterMask; mask <<= 1) {
        		if ((result & mask) == 0) {
        			countCategories++;
        		}
        	}
        	if (countCategories < passwordMinNumberOfCategories) {
        		System.out.println("Password must contain characters belonging to at least three of the following four categories:\n"
	    				+ "uppercase letters, lowercase letters, digits, and special characters.\n");
        		System.exit(0);
        	}
         	if (!changePassword(authUrl)) {
         		System.exit(0);
         	}
         	password = newPassword;
         	newPassword = "";
        }

        try {

            // Connect to Refinitiv Data Platform and authenticate (using our username and password)
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

                if(region.equals("amer"))
                {
                    if ( endpoint.getJSONArray("location").getString(0).startsWith("us-") == false )
                        continue;
                }
                else if(region.equals("emea"))
                {
                    if ( endpoint.getJSONArray("location").getString(0).startsWith("eu-") == false )
                        continue;
                }
                else if(region.equals("apac"))
                {
                    if ( endpoint.getJSONArray("location").getString(0).startsWith("ap-") == false )
                        continue;
                }

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

            if(hotstandby)
            {
                if(hostList.size() < 2)
                {
                    System.out.println("Error: Expected 2 hosts but received " + hostList.size());
                    System.exit(1);
                }
            }
            else
            {
                if (hostList.size() == 0)
                {
                    System.out.println("Error: No endpoints in response.");
                    System.exit(1);
                }
            }

            // Connect WebSocket(s).
            webSocketSession1 = new WebSocketSession("session1", hostList.get(0), authJson.getString("access_token"));
            if (hotstandby)
                webSocketSession2 = new WebSocketSession("session2", hostList.get(1), authJson.getString("access_token"));

            while(true) {
                // Continue using current token until 90% of initial time before it expires.
                Thread.sleep(expireTime * 900);  // The value 900 means 90% of expireTime in milliseconds

                // Connect to Refinitiv Data Platform and re-authenticate, using the refresh token provided in the previous response
                authJson = getAuthenticationInfo(authJson);
                if (authJson == null)
                    System.exit(1);
 
                // If expiration time returned by refresh request is less then initial expiration time,
                // re-authenticate using password
                int refreshingExpireTime = Integer.parseInt(authJson.getString("expires_in"));
                if (refreshingExpireTime != expireTime) {
                   	System.out.println("expire time changed from " + expireTime + " sec to " + refreshingExpireTime + 
                    		" sec; retry with password");
                    authJson = getAuthenticationInfo(null);
                    if (authJson == null)
                        System.exit(1);
                    expireTime = Integer.parseInt(authJson.getString("expires_in"));
                }

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

    public static int checkPassword(String pwd) {
    	int result = 0;
    	
    	if (pwd.length() < passwordLengthMin) {
    		result |= passwordLengthMask;
    	} 
    	
    	int countUpper = 0;
    	int countLower = 0;
        int countDigit = 0;
        int countSpecial = 0;
        
        for (int i = 0; i < pwd.length(); i++) {
        	char c = pwd.charAt(i);
        	StringBuffer currentSymbol = new StringBuffer(1);
        	currentSymbol.append(c);

        	String charAsString = new String(currentSymbol);
        	if ((!charAsString.matches("[A-Za-z0-9]")) && (!passwordSpecialCharacterSet.contains(currentSymbol))) {
        		result |= passwordInvalidCharacterMask;
        	}
        	
        	if (Character.isUpperCase(c)) {
        		countUpper++;
        	}
        	if (Character.isLowerCase(c)) {
        		countLower++;
        	}
        	if (Character.isDigit(c)) {
        		countDigit++;
        	}        	
        	if (passwordSpecialCharacterSet.contains(currentSymbol))  {
        		countSpecial++;
        	}
        }
        
        if (countUpper < passwordUppercaseLetterMin) {
        	result |= passwordUppercaseLetterMask;
        }
        if (countLower < passwordLowercaseLetterMin) {
        	result |= passwordLowercaseLetterMask;
        }
        if (countDigit < passwordDigitMin) {
        	result |= passwordDigitMask;
        }
        if (countSpecial < passwordSpecialCharacterMin) {
        	result |= passwordSpecialCharacterMask;
        }
        
    	return result;
    }
    
    /**
     * Send a request to change password and receive an answer.
     */   
    public static boolean changePassword(String authServer) {
    	boolean result = false;
        try
        {
            SSLConnectionSocketFactory sslsf = new SSLConnectionSocketFactory(new SSLContextBuilder().build());

            HttpClient httpclient = HttpClients.custom().setSSLSocketFactory(sslsf).build();
            HttpPost httppost = new HttpPost(authServer);
            HttpParams httpParams = new BasicHttpParams();

            // Disable redirect
            httpParams.setParameter(ClientPNames.HANDLE_REDIRECTS, false);

            // Set request parameters.
            List<NameValuePair> params = new ArrayList<NameValuePair>(2);
            params.add(new BasicNameValuePair("client_id", clientid));
            params.add(new BasicNameValuePair("username", user));
            params.add(new BasicNameValuePair("grant_type", "password"));
            params.add(new BasicNameValuePair("password", password));
            params.add(new BasicNameValuePair("newPassword", newPassword));
            params.add(new BasicNameValuePair("scope", scope));
            params.add(new BasicNameValuePair("takeExclusiveSignOnControl", "true"));
            System.out.println("Sending password change request to " + authUrl);

            httppost.setParams(httpParams);
            httppost.setEntity(new UrlEncodedFormEntity(params, "UTF-8"));

            //Execute and get the response.
            HttpResponse response = httpclient.execute(httppost);

            int statusCode = response.getStatusLine().getStatusCode();

            JSONObject responseJson = new JSONObject(EntityUtils.toString(response.getEntity()));
            switch ( statusCode ) {
            case HttpStatus.SC_OK:                  // 200
                // Password change was successful.
                System.out.println("Password was successfully changed:");
                result = true;
                break;

            case HttpStatus.SC_MOVED_PERMANENTLY:              // 301
            case HttpStatus.SC_MOVED_TEMPORARILY:              // 302
            case HttpStatus.SC_TEMPORARY_REDIRECT:             // 307
            case 308:                                          // 308 HttpStatus.SC_PERMANENT_REDIRECT
                // Perform URL redirect
                System.out.println("Password change HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                Header header = response.getFirstHeader("Location");
                if( header != null )
                {
                    String newHost = header.getValue();
                    if ( newHost != null )
                    {
                        System.out.println("Perform URL redirect to " + newHost);
                        result = changePassword(newHost);
                    } else {
                    	result = false;
                    }
                }
                break;
            default:
                // Error 4XX or 5XX
                System.out.println("Password change failure\n" 
                		+ response.getStatusLine().getStatusCode() + " " 
                		+ response.getStatusLine().getReasonPhrase());
                System.out.println(responseJson.toString(2));
                result = false;
            }
        } catch (Exception e) {
            System.out.println("Password change failure:");
            e.printStackTrace();
            result = false;
        }
        return result;
    }
    
    
    /**
     * Authenticate to Refinitiv Data Platform via an HTTP post request.
     * Initially authenticates using the specified password. If information from a previous authentication response is provided, it instead authenticates using
     * the refresh token from that response. Uses authUrl as url.
     * @param previousAuthResponseJson Information from a previous authentication, if available
     * @return A JSONObject containing the authentication information from the response.
     */
    public static JSONObject getAuthenticationInfo(JSONObject previousAuthResponseJson) {
        String url = authUrl;
        return getAuthenticationInfo(previousAuthResponseJson, url);
    }

    /**
     * Authenticate to Refinitiv Data Platform via an HTTP post request.
     * Initially authenticates using the specified password. If information from a previous authentication response is provided, it instead authenticates using
     * the refresh token from that response.
     * @param previousAuthResponseJson Information from a previous authentication, if available
     * @param url The HTTP post url
     * @return A JSONObject containing the authentication information from the response.
     */
     public static JSONObject getAuthenticationInfo(JSONObject previousAuthResponseJson, String url) {
         try
         {
             SSLConnectionSocketFactory sslsf = new SSLConnectionSocketFactory(new SSLContextBuilder().build());

             HttpClient httpclient = HttpClients.custom().setSSLSocketFactory(sslsf).build();
             HttpPost httppost = new HttpPost(url);
             HttpParams httpParams = new BasicHttpParams();

             // Disable redirect
             httpParams.setParameter(ClientPNames.HANDLE_REDIRECTS, false);

             // Set request parameters.
             List<NameValuePair> params = new ArrayList<NameValuePair>(2);
             params.add(new BasicNameValuePair("client_id", clientid));
             params.add(new BasicNameValuePair("username", user));

             if (previousAuthResponseJson == null)
             {
                 // First time through, send password.
                 params.add(new BasicNameValuePair("grant_type", "password"));
                 params.add(new BasicNameValuePair("password", password));
                 params.add(new BasicNameValuePair("scope", scope));
                 params.add(new BasicNameValuePair("takeExclusiveSignOnControl", "true"));
                 System.out.println("Sending authentication request with password to " + url + "...");

             }
             else
             {
                 // Use the refresh token we got from the last authentication response.
                 params.add(new BasicNameValuePair("grant_type", "refresh_token"));
                 params.add(new BasicNameValuePair("refresh_token", previousAuthResponseJson.getString("refresh_token")));
                 System.out.println("Sending authentication request with refresh token to " + url + "...");
             }

             httppost.setParams(httpParams);
             httppost.setEntity(new UrlEncodedFormEntity(params, "UTF-8"));

             //Execute and get the response.
             HttpResponse response = httpclient.execute(httppost);

             int statusCode = response.getStatusLine().getStatusCode();

             switch ( statusCode ) {
             case HttpStatus.SC_OK:                  // 200
                 // Authentication was successful. Deserialize the response and return it.
                 JSONObject responseJson = new JSONObject(EntityUtils.toString(response.getEntity()));
                 System.out.println("Refinitiv Data Platform Authentication succeeded. RECEIVED:");
                 System.out.println(responseJson.toString(2));
                 return responseJson;
             case HttpStatus.SC_MOVED_PERMANENTLY:              // 301
             case HttpStatus.SC_MOVED_TEMPORARILY:              // 302
             case HttpStatus.SC_TEMPORARY_REDIRECT:             // 307
             case 308:                                          // 308 HttpStatus.SC_PERMANENT_REDIRECT
                 // Perform URL redirect
                 System.out.println("Refinitiv Data Platform authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                 Header header = response.getFirstHeader("Location");
                 if( header != null )
                 {
                     String newHost = header.getValue();
                     if ( newHost != null )
                     {
                         System.out.println("Perform URL redirect to " + newHost);
                         return getAuthenticationInfo(previousAuthResponseJson, newHost);
                     }
                 }
                 return null;
             case HttpStatus.SC_BAD_REQUEST:                    // 400
             case HttpStatus.SC_UNAUTHORIZED:                   // 401
                 // Retry with username and password
                 System.out.println("Refinitiv Data Platform authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                 if (previousAuthResponseJson != null)
                 {
                     System.out.println("Retry with username and password");
                     return getAuthenticationInfo(null);
                 }
                 return null;
             case HttpStatus.SC_FORBIDDEN:                      // 403
             case 451:                                          // 451 Unavailable For Legal Reasons
                 // Stop retrying with the request
                 System.out.println("Refinitiv Data Platform authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                 System.out.println("Stop retrying with the request");
                 return null;
             default:
                 // Retry the request to Refinitiv Data Platform 
                 System.out.println("Refinitiv Data Platform authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                 System.out.println("Retry the request to Refinitiv Data Platform");
                 return getAuthenticationInfo(previousAuthResponseJson);
             }
         } catch (Exception e) {
             System.out.println("Refinitiv Data Platform authentication failure:");
             e.printStackTrace();
             return null;
         }
     }

    /**
     * Retrive service information indicating locations to connect to.
     * Uses authHostname and authPort as url.
     * @return A JSONObject containing the service information.
     */
    public static JSONObject queryServiceDiscovery() {
        String url =  discoveryUrl;
      	return queryServiceDiscovery(url);
    }

    /**
     * Retrive service information indicating locations to connect to for a specific host.
     * @param host the host to connect to
     * @return A JSONObject containing the service information.
     */
    public static JSONObject queryServiceDiscovery( String url ) {
        try
        {
            SSLConnectionSocketFactory sslsf = new SSLConnectionSocketFactory(new SSLContextBuilder().build());

            HttpClient httpclient = HttpClients.custom().setSSLSocketFactory(sslsf).build();

            HttpGet httpget = new HttpGet(url + "?transport=websocket");
            HttpParams httpParams = new BasicHttpParams();

            // Disable redirect
            httpParams.setParameter(ClientPNames.HANDLE_REDIRECTS, false);

            httpget.setParams(httpParams);
            httpget.setHeader("Authorization", "Bearer " + authJson.getString("access_token"));

            System.out.println("Sending service discovery request to " + url + "...");

            //Execute and get the response.
            HttpResponse response = httpclient.execute(httpget);

	          int statusCode = response.getStatusLine().getStatusCode();

            switch ( statusCode )
            {
            case HttpStatus.SC_OK:                             // 200
                // Service discovery was successful. Deserialize the response and return it.
                JSONObject responseJson = new JSONObject(EntityUtils.toString(response.getEntity()));
                System.out.println("Refinitiv Data Platform service discovery succeeded. RECEIVED:");
                System.out.println(responseJson.toString(2));
                return responseJson;
            case HttpStatus.SC_MOVED_PERMANENTLY:              // 301
            case HttpStatus.SC_MOVED_TEMPORARILY:              // 302
            case HttpStatus.SC_SEE_OTHER:                      // 303
            case HttpStatus.SC_TEMPORARY_REDIRECT:             // 307
            case 308:                                          // 308 HttpStatus.SC_PERMANENT_REDIRECT
                // Perform URL redirect
                System.out.println("Refinitiv Data Platform service discovery HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                Header header = response.getFirstHeader("Location");
                if( header != null )
                {
                    String newHost = header.getValue();
                    if ( newHost != null )
                    {
                        System.out.println("Perform URL redirect to " + newHost);
                        return queryServiceDiscovery(newHost);
                    }
                }
                return null;
            case HttpStatus.SC_FORBIDDEN:                      // 403
            case 451:                                          // 451 Unavailable For Legal Reasons
                // Stop retrying with the request
                System.out.println(" Refinitiv Data Platform service discovery HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                System.out.println("Stop retrying with the request");
                return null;
            default:
                // Retry the service discovery request
                System.out.println("Refinitiv Data Platform service discovery HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                System.out.println("Retry the service discovery request");
                return queryServiceDiscovery();
            }
        } catch (Exception e) {
            System.out.println("Refinitiv Data Platform service discovery failure:");
            e.printStackTrace();
            return null;
        }
    }
}
