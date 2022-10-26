//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|            Copyright (C) 2022 Refinitiv. All rights reserved.        --
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
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

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
 * using the endpoint and authentitcation to retrieve market content. Specifically
 * for authentication, this application uses the client credentials grant type
 * connection to version 2 (v2) of RDP.
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
 * - Upon disconnect, re-request authentication token to reconnect to Refinitiv Data
 *   Platform endpoint(s) if it is no longer valid.
 */

public class MarketPriceRdpGwClientCredAuth {

    public static String port = "443";
    public static String port2 = "443";
    public static String hostName = "";
    public static String hostName2 = "";
    public static String clientid = "";
    public static String clientsecret = "";
    public static String position = "";
    public static String appId = "256";
    public static String authUrl = "https://api.refinitiv.com/auth/oauth2/v2/token";
    public static String discoveryUrl = "https://api.refinitiv.com/streaming/pricing/v1/";
    public static String ric = "/TRI.N";
    public static String service = "ELEKTRON_DD";
    public static String scope = "trapi.streaming.pricing.read";
    public static JSONObject authJson = null;
    public static JSONObject serviceJson = null;
    public static List<String> hostList = new LinkedList<String>();
	public static List<String> backupHostList = new LinkedList<String>();
    public static WebSocketFactory websocketFactory = new WebSocketFactory();
    public static WebSocketSession webSocketSession1 = null;
    public static WebSocketSession webSocketSession2 = null;
    public static boolean hotstandby = false;
    public static String region = "us-east-1";
    
    /**
     * Helper class for date time stamp formatting.
     */
    public static class DateTimeStamp
    {
        /** Name to use when printing messages sent/received over this WebSocket. */
        private static final DateTimeFormatter formatter = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss.n");

        public static String getCurrentTime() {
            return formatter.format(LocalDateTime.now());
        }
    }

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

        /** Whether the session was disconnected and needs a new authentication token. */
        boolean _needNewToken = false;

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
                System.out.println(DateTimeStamp.getCurrentTime() + " Connecting to WebSocket " + _url + " for " + _name + "...");

                try {
                    _websocket = websocketFactory.createSocket(_url)
                        .addProtocol("tr_json2")
                        .addListener(new WebSocketAdapter() {

                                /**
                                 * Called when message received, parse message into JSON for processing
                                 */
                                public void onTextMessage(WebSocket websocket, String message) throws JSONException {
                                    System.out.println(DateTimeStamp.getCurrentTime() + " RECEIVED on " + _name +":");
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
                                    System.out.println(DateTimeStamp.getCurrentTime() + " WebSocket successfully connected for " + _name + "!");
                                    sendLoginRequest(true);
                                }

                                /**
                                 * Called when an error occurs while attempting to connect the WebSocket.
                                 */
                                public void onConnectError(WebSocket websocket, WebSocketException e)
                                {
                                    System.out.println(DateTimeStamp.getCurrentTime() + " Connect error for " + _name + ":" + e);
                                    reconnect(websocket, false);
                                }

                                /**
                                 * Called when the WebSocket is disconnected.
                                 */
                                public void onDisconnected(WebSocket websocket,
                                        WebSocketFrame serverCloseFrame,
                                        WebSocketFrame clientCloseFrame,
                                        boolean closedByServer)

                                {
                                    System.out.println(DateTimeStamp.getCurrentTime() + " WebSocket disconnected for " + _name + ".");
                                    reconnect(websocket, true);
                                }

                                /**
                                 * Reconnect after a delay.
                                 */
                                public void reconnect(WebSocket websocket, boolean needNewToken)
                                {
                                    WebSocketSession webSocketSession = webSocketSessionMap.get(websocket);
                                    webSocketSession.isLoggedIn(false);
                                    webSocketSession.needNewToken(needNewToken);

                                    do {
                                        try {
                                            Thread.sleep(3000);
                                        } catch (InterruptedException e) {
                                            e.printStackTrace();
                                            System.exit(1);
                                        }

                                        if (needNewToken && webSocketSession.needNewToken())
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            webSocketSession.connect();
                                            break;
                                        }
                                    } while(true);
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

            _websocket.sendText(loginJson.toString());
            System.out.println(DateTimeStamp.getCurrentTime() + " SENT on " + _name + ": \n" + loginJson.toString(2));
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
            System.out.println(DateTimeStamp.getCurrentTime() + " SENT on " + _name + ": \n" + mpRequestJson.toString(2));
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
                    System.out.println(DateTimeStamp.getCurrentTime() + " SENT on " + _name + ": \n" + pongJson.toString(2));
                    break;
                default:
                    break;
            }
        }

        /**
         * Update access token.
         */
        public synchronized void updateToken(String updatedAuthToken)
        {
            _authToken = updatedAuthToken;
            _needNewToken = false;

            System.out.println(DateTimeStamp.getCurrentTime() + " Refreshing the access token for " + _name);
        }

        /**
         * Mark whether we are connected and logged in so that the updateToken method knows whether or not to
         * reissue the login.
         */
        public synchronized void isLoggedIn(boolean isLoggedIn)
        {
            _isLoggedIn = isLoggedIn;
        }

        /**
         * Mark whether we has been disconnected and need a new authentication token.
         */
        public synchronized void needNewToken(boolean needNewToken)
        {
            _needNewToken = needNewToken;
        }

        /**
         * Whether the session was disconnected and needs a new authentication token.
         */
        public synchronized boolean needNewToken()
        {
            return _needNewToken;
        }
    }

    public static void main(String[] args) {

        Options options = new Options();

        options.addOption(Option.builder().longOpt("port").hasArg().desc("port").build());
        options.addOption(Option.builder().longOpt("standbyport").hasArg().desc("hotstandby port").build());
        options.addOption(Option.builder().longOpt("hostname").hasArg().desc("hostname").valueSeparator().build());
        options.addOption(Option.builder().longOpt("standbyhostname").hasArg().desc("hotstandby hostname").valueSeparator().build());
        options.addOption(Option.builder().longOpt("app_id").hasArg().desc("app_id").build());
        options.addOption(Option.builder().longOpt("clientid").required().hasArg().desc("clientid").build());
        options.addOption(Option.builder().longOpt("clientsecret").required().hasArg().desc("clientsecret").build());
        options.addOption(Option.builder().longOpt("position").hasArg().desc("position").build());
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
            formatter.printHelp("MarketPriceRdpGwClientCredAuth", options);
            System.exit(1);
            return;
        }

        if(cmd.hasOption("help"))
        {
             formatter.printHelp("MarketPriceRdpGwClientCredAuth", options);
             System.exit(0);
        }
        if(cmd.hasOption("port"))
            port = cmd.getOptionValue("port");
        if(cmd.hasOption("standbyport"))
            port2 = cmd.getOptionValue("standbyport");
        if(cmd.hasOption("hostname"))
            hostName = cmd.getOptionValue("hostname");
        if(cmd.hasOption("standbyhostname"))
            hostName2 = cmd.getOptionValue("standbyhostname");
        if(cmd.hasOption("app_id"))
            appId = cmd.getOptionValue("app_id");
        if(cmd.hasOption("clientid"))
            clientid = cmd.getOptionValue("clientid");
        if(cmd.hasOption("clientsecret"))
            clientsecret = cmd.getOptionValue("clientsecret");
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
        }

        try {

            // Connect to Refinitiv Data Platform and authenticate (using our clientid and clientsecret)
            authJson = getAuthenticationInfo();
            if (authJson == null)
                System.exit(1);

            if (hostName.isEmpty())
            {
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

                    if ( endpoint.getJSONArray("location").getString(0).startsWith(region) == false )
                        continue;

                    if (!hotstandby)
                    {
                        if (endpoint.getJSONArray("location").length() >= 2)
                        {
                            hostList.add(endpoint.getString("endpoint") + ":" + endpoint.getInt("port"));
                            continue;
                        }
						else if (endpoint.getJSONArray("location").length() == 1)
                        {
                            backupHostList.add(endpoint.getString("endpoint") + ":" + endpoint.getInt("port"));
                            continue;
                        }
                    }
                    else
                    {
                        if (endpoint.getJSONArray("location").length() == 1)
                            hostList.add(endpoint.getString("endpoint") + ":" + endpoint.getInt("port"));
                    }
                }
            }
            else
            {
                hostList.add(hostName + ":" + port);
                if (hotstandby && !hostName2.isEmpty())
                {
                    hostList.add(hostName2 + ":" + port2);
                }
            }

            //long expireTime = calcExpireTime(Integer.parseInt(authJson.getString("expires_in")));
            long expireTime = calcExpireTime(authJson.getInt("expires_in"));

            if (hotstandby)
            {
                if(hostList.size() < 2)
                {
                    System.out.println("Error: Expected 2 hosts but received " + hostList.size() + " or the region: " + region + " is not present in list of endpoints" );
                    System.exit(1);
                }
            }
			else
			{
				if (hostList.size() == 0)
				{
					if (backupHostList.size() > 0)
					{
						hostList = backupHostList;
					}
				}
			}

			if (hostList.size() == 0)
			{
				System.out.println("Error: The region: " + region + " is not present in list of endpoints");
				System.exit(1);
			}

            // Connect WebSocket(s).
            webSocketSession1 = new WebSocketSession("session1", hostList.get(0), authJson.getString("access_token"));
            if (hotstandby)
                webSocketSession2 = new WebSocketSession("session2", hostList.get(1), authJson.getString("access_token"));

            while(true) {
                // NOTE about connection recovery: When connecting or reconnecting 
                //   to the server, a valid token must be used. 
                //   Any requested token may be used in [re]connecting to the 
                //   server upto the expires_in time. Therefore, check if token 
                //   is valid before using it after reconnection and get a new token ONLY as needed
                Thread.sleep(3000);
                if (expireTime < System.currentTimeMillis()
                    || webSocketSession1.needNewToken()
                    || hotstandby && webSocketSession2.needNewToken())
                {
                    // Connect to Refinitiv Data Platform and re-authenticate using client_id and client_secret
                    authJson = getAuthenticationInfo();
                    if (authJson == null)
                        System.exit(1);
 
                    // Update token expiration time
                    //expireTime = calcExpireTime(Integer.parseInt(authJson.getString("expires_in")));
                    expireTime = calcExpireTime(authJson.getInt("expires_in"));

                    // Updated access token for WebSocket connections.
                    webSocketSession1.updateToken(authJson.getString("access_token"));
                    if (hotstandby)
                        webSocketSession2.updateToken(authJson.getString("access_token"));
                }
            }
        } catch (JSONException e) {
            e.printStackTrace();
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
    }

    /**
     * Determine when the access token expires. We will re-authenticate before then.
     * @param expireIn The "expires_in" value of the authentication response
     * @return expire time.
     */
    private static long calcExpireTime(int expireIn) {
        if (expireIn < 600)
        {
            // The value 900 means 90% of expireTime in milliseconds
            return System.currentTimeMillis() + expireIn * 900;
        }
        else
        {
            return System.currentTimeMillis() + 300 * 1000;
        }
    }

    /**
     * Authenticate to Refinitiv Data Platform via an HTTP post request.
     * Initially authenticates using the specified client_secret. If information from a previous authentication response is provided, it instead authenticates using
     * the refresh token from that response. Uses authUrl as url.
     * @return A JSONObject containing the authentication information from the response.
     */
    public static JSONObject getAuthenticationInfo() {
        String url = authUrl;
        return getAuthenticationInfo(url);
    }

    /**
     * Authenticate to Refinitiv Data Platform via an HTTP post request.
     * Authenticates using the specified client_secret.
     * @param url The HTTP post url
     * @return A JSONObject containing the authentication information from the response.
     */
     public static JSONObject getAuthenticationInfo(String url) {
         try
         {
             SSLConnectionSocketFactory sslsf = new SSLConnectionSocketFactory(new SSLContextBuilder().build());

             HttpClient httpclient = HttpClients.custom().setSSLSocketFactory(sslsf).build();
             HttpPost httppost = new HttpPost(url);
             HttpParams httpParams = new BasicHttpParams();

             // Disable redirect
             httpParams.setParameter(ClientPNames.HANDLE_REDIRECTS, false);

             // Set request parameters.
             List<NameValuePair> params = new ArrayList<NameValuePair>(4);
             params.add(new BasicNameValuePair("grant_type", "client_credentials"));
             params.add(new BasicNameValuePair("client_id", clientid));
             params.add(new BasicNameValuePair("client_secret", clientsecret));
             params.add(new BasicNameValuePair("scope", scope));
             System.out.println(DateTimeStamp.getCurrentTime() + " Sending authentication request with client_secret to " + url + "...");

             httppost.setParams(httpParams);
             httppost.setEntity(new UrlEncodedFormEntity(params, "UTF-8"));

             //Execute and get the response.
             HttpResponse response = httpclient.execute(httppost);

             int statusCode = response.getStatusLine().getStatusCode();

             switch ( statusCode ) {
             case HttpStatus.SC_OK:                  // 200
                 // Authentication was successful. Deserialize the response and return it.
                 JSONObject responseJson = new JSONObject(EntityUtils.toString(response.getEntity()));
                 System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform Authentication succeeded. RECEIVED:");
                 System.out.println(responseJson.toString(2));
                 return responseJson;
             case HttpStatus.SC_MOVED_PERMANENTLY:              // 301
             case HttpStatus.SC_MOVED_TEMPORARILY:              // 302
             case HttpStatus.SC_TEMPORARY_REDIRECT:             // 307
             case 308:                                          // 308 HttpStatus.SC_PERMANENT_REDIRECT
                 // Perform URL redirect
                 System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                 Header header = response.getFirstHeader("Location");
                 if( header != null )
                 {
                     String newHost = header.getValue();
                     if ( newHost != null )
                     {
                         System.out.println(DateTimeStamp.getCurrentTime() + " Perform URL redirect to " + newHost);
                         return getAuthenticationInfo(newHost);
                     }
                 }
                 return null;
             case HttpStatus.SC_BAD_REQUEST:                    // 400
             case HttpStatus.SC_UNAUTHORIZED:                   // 401
             case HttpStatus.SC_FORBIDDEN:                      // 403
             case HttpStatus.SC_NOT_FOUND:                      // 404
             case HttpStatus.SC_GONE:                           // 410
             case 451:                                          // 451 Unavailable For Legal Reasons
                 // Stop trying the request
                 System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                 System.out.println("Stop retrying with the request");
                 return null;
             default:
                 // Retry the request with an appropriate delay
                 System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                 Thread.sleep(5000);
                 // CAUTION: This is sample code with infinite retries.
                 System.out.println("Retrying auth request");
                 return getAuthenticationInfo();
             }
         } catch (Exception e) {
             System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform authentication failure:");
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

            System.out.println(DateTimeStamp.getCurrentTime() + " Sending service discovery request to " + url + "...");

            //Execute and get the response.
            HttpResponse response = httpclient.execute(httpget);

	          int statusCode = response.getStatusLine().getStatusCode();

            switch ( statusCode )
            {
            case HttpStatus.SC_OK:                             // 200
                // Service discovery was successful. Deserialize the response and return it.
                JSONObject responseJson = new JSONObject(EntityUtils.toString(response.getEntity()));
                System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform service discovery succeeded. RECEIVED:");
                System.out.println(responseJson.toString(2));
                return responseJson;
            case HttpStatus.SC_MOVED_PERMANENTLY:              // 301
            case HttpStatus.SC_MOVED_TEMPORARILY:              // 302
            case HttpStatus.SC_TEMPORARY_REDIRECT:             // 307
            case 308:                                          // 308 HttpStatus.SC_PERMANENT_REDIRECT
                // Perform URL redirect
                System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform service discovery HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                Header header = response.getFirstHeader("Location");
                if( header != null )
                {
                    String newHost = header.getValue();
                    if ( newHost != null )
                    {
                        System.out.println(DateTimeStamp.getCurrentTime() + " Perform URL redirect to " + newHost);
                        return queryServiceDiscovery(newHost);
                    }
                }
                return null;
            case HttpStatus.SC_FORBIDDEN:                      // 403
            case HttpStatus.SC_NOT_FOUND:                      // 404
            case HttpStatus.SC_GONE:                           // 410
            case 451:                                          // 451 Unavailable For Legal Reasons
                // Stop retrying with the request
                System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform service discovery HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                System.out.println("Stop retrying with the request");
                return null;
            default:
                // Retry the service discovery request with an appropriate delay
                System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform service discovery HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                Thread.sleep(5000);
                // CAUTION: This is sample code with infinite retries.
                System.out.println("Retry the service discovery request");
                return queryServiceDiscovery();
            }
        } catch (Exception e) {
            System.out.println(DateTimeStamp.getCurrentTime() + " Refinitiv Data Platform service discovery failure:");
            e.printStackTrace();
            return null;
        }
    }
}
