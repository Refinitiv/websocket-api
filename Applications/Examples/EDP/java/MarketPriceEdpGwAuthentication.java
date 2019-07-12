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

import java.io.IOException;
import java.net.Inet4Address;
import java.net.UnknownHostException;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import org.json.*;
import org.apache.commons.cli.*;
import org.apache.http.*;
import org.apache.http.client.HttpClient;
import org.apache.http.client.entity.UrlEncodedFormEntity;
import org.apache.http.client.methods.HttpPost;
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
    public static String authHostname = "api.refinitiv.com";
    public static String authPort = "443";
    public static String ric = "/TRI.N";
    public static String scope = "trapi";
    public static JSONObject authJson = null;

    public static void main(String[] args) {
        
    Options options = new Options();
        
        options.addOption(Option.builder().longOpt("hostname").required().hasArg().desc("hostname").build());
        options.addOption(Option.builder().longOpt("port").hasArg().desc("port").build());
        options.addOption(Option.builder().longOpt("app_id").hasArg().desc("app_id").build());
        options.addOption(Option.builder().longOpt("user").required().hasArg().desc("user").build());
        options.addOption(Option.builder().longOpt("clientid").required().hasArg().desc("clientid").build());
        options.addOption(Option.builder().longOpt("position").hasArg().desc("position").build());
        options.addOption(Option.builder().longOpt("password").required().hasArg().desc("password").build());
        options.addOption(Option.builder().longOpt("auth_hostname").hasArg().desc("auth_hostname").build());
        options.addOption(Option.builder().longOpt("auth_port").hasArg().desc("auth_port").build());
        options.addOption(Option.builder().longOpt("ric").hasArg().desc("ric").build());
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
        requestJsonString = "{\"ID\":2,\"Key\":{\"Name\":\"" + ric + "\"}}";
        JSONObject mpRequestJson = new JSONObject(requestJsonString);
        websocket.sendText(requestJsonString);
        System.out.println("SENT:\n" + mpRequestJson.toString(2));
    }
}
