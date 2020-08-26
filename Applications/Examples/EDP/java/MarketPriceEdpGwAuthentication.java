//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|            Copyright (C) 2019-2020 Refinitiv. All rights reserved.        --
//|-----------------------------------------------------------------------------


import com.neovisionaries.ws.client.WebSocket;
import com.neovisionaries.ws.client.WebSocketAdapter;
import com.neovisionaries.ws.client.WebSocketException;
import com.neovisionaries.ws.client.WebSocketExtension;
import com.neovisionaries.ws.client.WebSocketFactory;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.net.Inet4Address;
import java.net.UnknownHostException;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Properties;

import org.json.*;
import org.apache.commons.cli.*;
import org.apache.http.*;
import org.apache.http.client.HttpClient;
import org.apache.http.client.entity.UrlEncodedFormEntity;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.client.params.ClientPNames;
import org.apache.http.params.HttpParams;
import org.apache.http.params.BasicHttpParams;
import org.apache.http.conn.ssl.SSLConnectionSocketFactory;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.message.BasicNameValuePair;
import org.apache.http.ssl.SSLContextBuilder;
import org.apache.http.util.EntityUtils;

/**
 * This example demonstrates authenticating via the Elektron Real-Time Service and Elektron Data Platform Gateway, and logging in with the retrieved token to retrieve market content.
 * It does so by:
 * - Authenticating via HTTP Post request to the Gateway
 * - Opening a WebSocket to the Elektron Real-Time Service
 * - Logging into the Real-Time Service using the token retrieved from the Gateway
 * - Requesting market content
 * - Perodically re-authenticating to the Gateway, and providing the updated token to the Real-Time Service.
 */
public class MarketPriceEdpGwAuthentication {

    public static String server;
    public static String hostname = "127.0.0.1";
    public static String port = "443";
    public static String user = "root";
    public static String clientid = "";
    public static String position = "";
    public static String appId = "256";
    public static WebSocket ws = null;
    public static String authToken = "";
    public static String password = "";
    public static String newPassword = "";
    public static String authUrl = "https://api.refinitiv.com:443/auth/oauth2/v1/token";
    public static String ric = "/TRI.N";
    public static String service = "ELEKTRON_DD";
    public static String scope = "trapi";
    public static JSONObject authJson = null;

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
    
    public static void main(String[] args) {

    Options options = new Options();

        options.addOption(Option.builder().longOpt("hostname").required().hasArg().desc("hostname").build());
        options.addOption(Option.builder().longOpt("port").hasArg().desc("port").build());
        options.addOption(Option.builder().longOpt("app_id").hasArg().desc("app_id").build());
        options.addOption(Option.builder().longOpt("user").required().hasArg().desc("user").build());
        options.addOption(Option.builder().longOpt("clientid").required().hasArg().desc("clientid").build());
        options.addOption(Option.builder().longOpt("position").hasArg().desc("position").build());
        options.addOption(Option.builder().longOpt("password").required().hasArg().desc("password").build());
        options.addOption(Option.builder().longOpt("newPassword").hasArg().desc("newPassword").build());
        options.addOption(Option.builder().longOpt("auth_url").hasArg().desc("auth_url").build());
        options.addOption(Option.builder().longOpt("ric").hasArg().desc("ric").build());
        options.addOption(Option.builder().longOpt("service").hasArg().desc("service").build());
        options.addOption(Option.builder().longOpt("scope").hasArg().desc("scope").build());
        options.addOption(Option.builder().longOpt("help").desc("help").build());


        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        CommandLine cmd;

        try {
            cmd = parser.parse(options, args);
        } catch (org.apache.commons.cli.ParseException e) {
            System.out.println(e.getMessage());
            formatter.printHelp("MarketPriceEdpGwAuthentication", options);
            System.exit(1);
            return;
        }

        if(cmd.hasOption("help"))
        {
             formatter.printHelp("MarketPriceEdpGwAuthentication", options);
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
        if(cmd.hasOption("clientid"))
            clientid = cmd.getOptionValue("clientid");
        if(cmd.hasOption("password"))
            password = cmd.getOptionValue("password");
        if(cmd.hasOption("auth_url"))
            authUrl = cmd.getOptionValue("auth_url");
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
	    				+ "uppercase letters, lovercase letters, digits, and special characters.\n");
        		System.exit(0);
        	}
         	if (!changePassword(authUrl)) {
         		System.exit(0);
         	}
         	password = newPassword;
         	newPassword = "";
        }

        try {

            // Connect to the gateway and authenticate (using our username and password)
            authJson = getAuthenticationInfo(null);
            if (authJson == null)
                System.exit(1);

            // Determine when the access token expires. We will re-authenticate before then.
            int expireTime = Integer.parseInt(authJson.getString("expires_in"));

            server = String.format("wss://%s:%s/WebSocket", hostname, port);
            System.out.println("Connecting to WebSocket " + server + " ...");
            ws = connect();

            while(true) {
                // Continue using current token until 90% of initial time before it expires.
                Thread.sleep(expireTime * 900);  // The value 900 means 90% of expireTime in milliseconds

                // Connect to the gateway and re-authenticate, using the refresh token provided in the previous response
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

                // Send the updated access token over the WebSocket.
                sendLoginRequest(ws, authJson.getString("access_token"), false);

            }
        } catch (IOException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
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
     * Connect to the Realtime Service over a WebSocket.
     */
    public static WebSocket connect() throws IOException, WebSocketException
    {
        return new WebSocketFactory()
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
                        sendLoginRequest(websocket, authJson.getString("access_token"), true);
                    }
                })
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE)
                .connect();
    }

    /**
     * Authenticate to the gateway via an HTTP post request.
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
     * Authenticate to the gateway via an HTTP post request.
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
                System.out.println("EDP-GW Authentication succeeded. RECEIVED:");
                System.out.println(responseJson.toString(2));
                return responseJson;
            case HttpStatus.SC_MOVED_PERMANENTLY:              // 301
            case HttpStatus.SC_MOVED_TEMPORARILY:              // 302
            case HttpStatus.SC_TEMPORARY_REDIRECT:             // 307
            case 308:                                          // 308 HttpStatus.SC_PERMANENT_REDIRECT
                // Perform URL redirect
                System.out.println("EDP-GW authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
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
                System.out.println("EDP-GW authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                if (previousAuthResponseJson != null)
                {
                    System.out.println("Retry with username and password");
                    return getAuthenticationInfo(null);
                }
                return null;
            case HttpStatus.SC_FORBIDDEN:                      // 403
            case 451:                                          // 451 Unavailable For Legal Reasons
                // Stop retrying with the request
                System.out.println("EDP-GW authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                System.out.println("Stop retrying with the request");
                return null;
            default:
                // Retry the request to the API gateway
                System.out.println("EDP-GW authentication HTTP code: " + response.getStatusLine().getStatusCode() + " " + response.getStatusLine().getReasonPhrase());
                System.out.println("Retry the request to the API gateway");
                return getAuthenticationInfo(previousAuthResponseJson);
            }
        } catch (Exception e) {
            System.out.println("EDP-GW authentication failure:");
            e.printStackTrace();
            return null;
        }
    }

    /**
     * Generate a login request from command line data (or defaults) and send
     * Used for both the initial login and subsequent logins that send updated access tokens.
     * @param websocket Websocket to send the request on
     * @param authToken Token to use
     * @param isFirstLogin Whether this is our first login
     * @throws JSONException
     */
    public static void sendLoginRequest(WebSocket websocket, String authToken, boolean isFirstLogin) throws JSONException {
        String loginJsonString = "{\"ID\":1,\"Domain\":\"Login\",\"Key\":{\"Elements\":{\"ApplicationId\":\"\",\"Position\":\"\",\"AuthenticationToken\":\"\"},\"NameType\":\"AuthnToken\"}}";
        JSONObject loginJson = new JSONObject(loginJsonString);
        loginJson.getJSONObject("Key").getJSONObject("Elements").put("AuthenticationToken", authToken);
        loginJson.getJSONObject("Key").getJSONObject("Elements").put("ApplicationId", appId);
        loginJson.getJSONObject("Key").getJSONObject("Elements").put("Position", position);

        if (!isFirstLogin) // If this isn't our first login, we don't need another refresh for it.
            loginJson.put("Refresh", false);

        websocket.sendText(loginJson.toString());
        System.out.println("SENT:\n" + loginJson.toString(2));
    }

    /**
     * Process a message received over the WebSocket
     * @param websocket Websocket the message was received on
     * @param messageJson Deserialized JSON message
     * @throws JSONException
     */
    public static void processMessage(WebSocket websocket, JSONObject messageJson) throws JSONException {
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
                            sendRequest(websocket);
                        }

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
     * @param websocket Websocket to send the message on
     * @throws JSONException
     */
    public static void sendRequest(WebSocket websocket) throws JSONException {
        String requestJsonString;
        requestJsonString = "{\"ID\":2,\"Key\":{\"Name\":\"" + ric + "\",\"Service\":\"" + service + "\"}}";
        JSONObject mpRequestJson = new JSONObject(requestJsonString);
        websocket.sendText(requestJsonString);
        System.out.println("SENT:\n" + mpRequestJson.toString(2));
    }
}
