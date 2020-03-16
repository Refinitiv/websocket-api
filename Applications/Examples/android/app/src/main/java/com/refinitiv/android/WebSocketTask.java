//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright (C) 2019 Refinitiv. All rights reserved.              --
//|-----------------------------------------------------------------------------


package com.refinitiv.android;

import android.os.AsyncTask;
import android.widget.TextView;

import com.neovisionaries.ws.client.WebSocket;
import com.neovisionaries.ws.client.WebSocketAdapter;
import com.neovisionaries.ws.client.WebSocketException;
import com.neovisionaries.ws.client.WebSocketExtension;
import com.neovisionaries.ws.client.WebSocketFactory;
import com.neovisionaries.ws.client.WebSocketFrame;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.net.InetAddress;
import java.net.NetworkInterface;
import java.net.URL;
import java.security.KeyManagementException;
import java.security.NoSuchAlgorithmException;
import java.security.cert.X509Certificate;
import java.util.Collections;
import java.util.List;
import java.util.Map;

import org.json.*;

import javax.net.ssl.HostnameVerifier;
import javax.net.ssl.HttpsURLConnection;
import javax.net.ssl.SSLContext;
import javax.net.ssl.SSLSession;
import javax.net.ssl.TrustManager;
import javax.net.ssl.X509TrustManager;

/**
 * Created on 12/9/2016.
 */
public class WebSocketTask extends AsyncTask<Void, String, Void> {


    public enum Request {MARKET_PRICE, MARKET_PRICE_BV, MARKET_PRICE_POST};

    private String server;
    private TextView textView;
    private String hostname;
    private String port;
    private String appId;
    private String username;
    private String password;
    private String authHostname;
    private String authPort;
    private String authToken;
    private boolean authentication;
    private String position;
    private Request requestType;
    private WebSocket ws = null;
    private int pingTimeoutInterval = 30;
    private long pingSendTime = 0;
    private long pingTimeoutTime = 0;

    private long nextPostTime = 0;
    private int postUserAddress;
    private int postId = 1;

    public WebSocketTask(TextView textView, String hostname, String port, String username, String password, String authHostname, String authPort, String appId, boolean authentication, Request requestType) {
        this.textView = textView;
        this.requestType = requestType;
        this.hostname = hostname;
        this.port = port;
        this.username = username;
        this.password = password;
        this.authHostname = authHostname;
        this.authPort = authPort;
        this.appId = appId;
        this.authentication = authentication;
        this.position = getDeviceIPAddress(true);
        this.server = String.format("ws://%s:%s/WebSocket", hostname, port);
    }

    // Main WebSocketTask thread
    protected Void doInBackground(Void... none) {
        try {
            if (authentication) {
                publishProgress("Sending authentication request...");

                TrustManager[] trustAllCerts = new TrustManager[] { new X509TrustManager() {
                    public java.security.cert.X509Certificate[] getAcceptedIssuers() {
                        return null;
                    }

                    public void checkClientTrusted(X509Certificate[] certs, String authType) {
                    }

                    public void checkServerTrusted(X509Certificate[] certs, String authType) {
                    }
                } };

                SSLContext sc = SSLContext.getInstance("SSL");
                sc.init(null, trustAllCerts, new java.security.SecureRandom());
                HttpsURLConnection.setDefaultSSLSocketFactory(sc.getSocketFactory());
                HostnameVerifier hv = new HostnameVerifier() {
                    public boolean verify(String hostname, SSLSession session) { return true; }
                };
                HttpsURLConnection.setDefaultHostnameVerifier(hv);
                URL url = new URL("https://" + authHostname + ":" + authPort + "/getToken");
                HttpsURLConnection conn = (HttpsURLConnection) url.openConnection();
                conn.setRequestMethod("POST");
                conn.setDoInput(true);
                conn.setDoOutput(true);

                OutputStream os = conn.getOutputStream();
                BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(os, "UTF-8"));
                writer.write("username=" + username + "&password=" + password);
                writer.flush();
                writer.close();
                os.close();
                String response = "";
                int responseCode=conn.getResponseCode();
                List<String> cookies = conn.getHeaderFields().get("set-cookie");

                if (responseCode == HttpsURLConnection.HTTP_OK) {
                    String line;
                    BufferedReader br=new BufferedReader(new InputStreamReader(conn.getInputStream()));
                    while ((line=br.readLine()) != null) {
                        response+=line;
                    }

                    JSONObject authJson = new JSONObject(response);

                    publishProgress("RECEIVED:");
                    publishProgress(authJson.toString(2));

                    boolean success = authJson.getBoolean("success");

                    if(success) {
                        for(String cookie : cookies)
                            if (cookie.startsWith("AuthToken"))
                                authToken = cookie.substring(cookie.indexOf("=") + 1, cookie.indexOf(";"));

                        publishProgress("Authentication Succeeded. Received AuthToken: " + authToken);
                        websocketConnect();
                        return null;
                    }
                }
                else {
                    publishProgress("Authentication failed");
                }
            }
            else {
                websocketConnect();
                return null;
            }
        }
        catch (JSONException ex)
        {
            publishProgress(ex.toString());
        }
        catch (IOException ex)
        {
            publishProgress(ex.toString());
        } catch (NoSuchAlgorithmException ex) {
            publishProgress(ex.toString());
        } catch (KeyManagementException ex) {
            publishProgress(ex.toString());
        }

        return null;
    }

    // Output to UI TextView
    protected void onProgressUpdate(String ... progress) {
        super.onProgressUpdate(progress);
        textView.append(progress[0] + "\n");
    }

    // Create the websocket connection
    private void websocketConnect() {

        publishProgress("Connecting to WebSocket " + server + " ...");
        try {
            ws = connect();

            // Build IP Address integer (used if sending post messages)
            String[] octets = position.split("\\.");
            this.postUserAddress = Integer.parseInt(octets[3]);
            this.postUserAddress += Integer.parseInt(octets[2]) << 8;
            this.postUserAddress += Integer.parseInt(octets[1]) << 16;
            this.postUserAddress += Integer.parseInt(octets[0]) << 24;

            pingSendTime = System.currentTimeMillis() + (pingTimeoutInterval * 1000 / 3);
            pingTimeoutTime = 0;

            while (true)
            {
                try {
                    Thread.sleep(1000);
                } catch (InterruptedException e) {
                    e.printStackTrace();
                    return;
                }

                if (ws != null) {
                    if (nextPostTime != 0 && System.currentTimeMillis() >= nextPostTime)
                    {
                        // If configured to send posts, send one every three seconds if the item
                        // stream is open.
                        sendPost(ws);
                        nextPostTime = System.currentTimeMillis() + 3000;
                    }

                    // If we didn't receive any traffic for a while, send a Ping.
                    // This is an optional behavior that can be used to monitor connection health.
                    if(pingSendTime > 0 && System.currentTimeMillis() > pingSendTime) {
                        String pingJsonString = "{\"Type\":\"Ping\"}";
                        JSONObject pingJson = new JSONObject(pingJsonString);
                        ws.sendText(pingJsonString);
                        publishProgress("SENT:\n" + pingJson.toString(2));

                        pingSendTime = 0;
                        pingTimeoutTime = System.currentTimeMillis() + (pingTimeoutInterval * 1000);
                    }

                    // If we sent a Ping but did not get any response, disconnect.
                    if(pingTimeoutTime > 0 && System.currentTimeMillis() > pingTimeoutTime) {
                        publishProgress("No ping from server, timing out");
                        try {
                            sendCloseMessage(ws);
                            ws.sendClose();
                            return;
                        } catch (JSONException e) {
                            e.printStackTrace();
                        }
                    }
                }
            }
        } catch (IOException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    // Connect to the server
    private WebSocket connect() throws IOException, WebSocketException
    {
        WebSocket ws = new WebSocketFactory()
                .createSocket(server)
                .addProtocol("tr_json2")
                .addListener(new WebSocketAdapter() {

                    // Called when frame is received, checks for server pings
                    public void onFrame(WebSocket websocket, WebSocketFrame frame) {

                        if (frame.isTextFrame() && frame.getPayloadLength() == 0) {
                            pingTimeoutTime = System.currentTimeMillis() + (pingTimeoutInterval * 1000);
                            publishProgress("PING RECEIVED!");
                        }
                    }

                    // A text message arrived from the server.
                    public void onTextMessage(WebSocket websocket, String message) throws JSONException {

                        publishProgress("RECEIVED:");

                        JSONArray jsonArray = new JSONArray(message);

                        publishProgress(jsonArray.toString(2));

                        for (int i = 0; i < jsonArray.length(); ++i)
                            processMessage(websocket, jsonArray.getJSONObject(i));

                        pingTimeoutTime = 0;
                        pingSendTime = System.currentTimeMillis() + (pingTimeoutInterval * 1000 / 3);
                    }

                    // Called when handshake is complete and websocket is open, send login
                    public void onConnected(WebSocket websocket, Map<String, List<String>> headers) throws JSONException {
                        publishProgress("WebSocket successfully connected!");
                        if(!authentication) {
                            sendLoginRequest(websocket);
                        }
                    }

                    // Called when websocket error has occurred
                    public void onError(WebSocket websocket, WebSocketException cause) throws JSONException {
                        publishProgress("Error:\n" + cause.getMessage());
                    }
                })
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE);

        if (authentication) {
            ws.addHeader("Cookie", String.format("AuthToken=%s;AuthPosition=%s;applicationId=%s;", authToken, position, appId));
        }

        return ws.connect();
    }

    // Generate a login request and send
    private void sendLoginRequest(WebSocket websocket) throws JSONException {
        String loginJsonString = "{\"ID\":1,\"Domain\":\"Login\",\"Key\":{\"Name\":\"root\",\"Elements\":{\"ApplicationId\":\"\",\"Position\":\"\"}}}";
        JSONObject loginJson = new JSONObject(loginJsonString);
        loginJson.getJSONObject("Key").getJSONObject("Elements").put("Position", position);
        loginJson.getJSONObject("Key").getJSONObject("Elements").put("ApplicationId", appId);
        websocket.sendText(loginJsonString);
        publishProgress("SENT:\n" + loginJson.toString(2));
    }

    // Parse at high level and output JSON of message
    private void processMessage(WebSocket websocket, JSONObject messageJson) throws JSONException {

        String messageType = messageJson.getString("Type");

        switch(messageType)
        {
            case "Refresh":
                if(messageJson.has("Domain")) {
                    String messageDomain = messageJson.getString("Domain");
                    if(messageDomain.equals("Login")) {
                        processLoginResponse(websocket, messageJson);
                    }
                }

                if (messageJson.getInt("ID") == 2)
                {
                    JSONObject messageState = messageJson.optJSONObject("State");
                    if (requestType == Request.MARKET_PRICE_POST && nextPostTime == 0 &&
                            (messageState == null || messageState.getString("Stream").equals("Open") && messageState.getString("Data").equals("Ok")))
                    {
                        // Item is open. We can start posting to it.
                        nextPostTime = System.currentTimeMillis() + 3000;
                    }
                }
                break;

            case "Ping":
                String pongJsonString = "{\"Type\":\"Pong\"}";
                JSONObject pongJson = new JSONObject(pongJsonString);
                websocket.sendText(pongJsonString);
                publishProgress("SENT:\n" + pongJson.toString(2));
                break;

            default:
                break;
        }
    }

    // Parse login message to set/start client ping and send item request
    private void processLoginResponse(WebSocket websocket, JSONObject messageJson) throws JSONException {
        pingTimeoutInterval = messageJson.getJSONObject("Elements").getInt("PingTimeout");
        sendRequest(websocket);
    }

    // Send the user specified request
    private void sendRequest(WebSocket websocket) throws JSONException {
        String requestJsonString;
        if(requestType == Request.MARKET_PRICE_BV)
                requestJsonString = "{\"ID\":2,\"Key\":{\"Name\":[\"TRI.N\",\"IBM.N\",\"T.N\"]},\"View\":[\"BID\",\"ASK\",\"BIDSIZE\"]}";
        else
                requestJsonString = "{\"ID\":2,\"Key\":{\"Name\":\"TRI.N\"}}";
        JSONObject mpRequestJson = new JSONObject(requestJsonString);
        websocket.sendText(requestJsonString);
        publishProgress("SENT:\n" + mpRequestJson.toString(2));
    }

    // Send a post message containing market-price content for TRI.N
    private void sendPost(WebSocket websocket) throws JSONException {

        String postJsonString =
            "{\"ID\": 2," +
            "\"Type\":\"Post\"," +
            "\"Domain\":\"MarketPrice\"," +
            "\"Ack\":true, \"PostID\":" + postId + "," +
                    "\"PostUserInfo\": {" +

                //Use our current IP address in the PostUserInfo.
                "\"Address\": \"" + position + "\"," +
                "\"UserID\": 1" +
                "}," +
                "\"Message\":{" +
                    "\"ID\":0,\"Type\":\"Update\",\"Fields\":{\"BID\":45.55,\"BIDSIZE\":18,\"ASK\":45.57,\"ASKSIZE\":19}" +
                "}" +
            "}";

        JSONObject mpPostJson = new JSONObject(postJsonString);
        websocket.sendText(postJsonString);
        publishProgress("SENT:\n" + mpPostJson.toString(2));
        ++postId;
    }

    // Create and send a close message
    private void sendCloseMessage(WebSocket websocket) throws JSONException {
        String closeJsonString = "{\"ID\":1,\"Type\":\"Close\"}";
        JSONObject closeJson = new JSONObject(closeJsonString);
        if(authentication)
            closeJson.put("ID", "-1");
        websocket.sendText(closeJsonString);
        publishProgress("SENT:\n" + closeJson.toString(2));

        websocket.sendClose();
        websocket = null;
    }

    // Get the IP address of this device
    private String getDeviceIPAddress(boolean useIPv4) {
        try {
            List<NetworkInterface> networkInterfaces = Collections.list(NetworkInterface.getNetworkInterfaces());
            for (NetworkInterface networkInterface : networkInterfaces) {
                List<InetAddress> inetAddresses = Collections.list(networkInterface.getInetAddresses());
                for (InetAddress inetAddress : inetAddresses) {
                    if (!inetAddress.isLoopbackAddress()) {
                        String sAddr = inetAddress.getHostAddress().toUpperCase();
                        boolean isIPv4 = sAddr.indexOf(':')<0;
                        if (useIPv4) {
                            if (isIPv4)
                                return sAddr;
                        } else {
                            if (!isIPv4) {
                                // drop ip6 port suffix
                                int delim = sAddr.indexOf('%');
                                return delim < 0 ? sAddr : sAddr.substring(0, delim);
                            }
                        }
                    }
                }
            }
        } catch (Exception ex) {
            ex.printStackTrace();
        }
        return "";
    }

    // Close the websocket connection
    public void closeConnection() throws JSONException {
        if (ws != null) {
            sendCloseMessage(ws);
        }
    }
}
