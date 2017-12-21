//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright Thomson Reuters 2017. All rights reserved.            --
//|-----------------------------------------------------------------------------


// Simple example of outputting Market Price JSON data using Websockets with authentication

package main

import (
	"crypto/tls"
	"io/ioutil"
	"flag"
	"fmt"
	"log"
	"os"
	"time"
	"os/signal"
	"net/http"
	"net/url"
	"net"
	"encoding/json"
	"github.com/gorilla/websocket"
)

func main() {

	// Get command line parameters
	hostname := flag.String("hostname", "127.0.0.1", "hostname")
	port := flag.String("port", "15000", "websocket port")
	user := flag.String("user", "root", "user")
	appId := flag.String("app_id", "555", "application id")
	password := flag.String("password", "", "password")
	authHostname := flag.String("auth_hostname", "127.0.0.1", "authentication hostname")
	authPort := flag.String("auth_port", "8443", "authentication port")

	positionDefault := ""
	host, _ := os.Hostname()
	addrs, _ := net.LookupIP(host)
	for _, addr := range addrs {
		if ipv4 := addr.To4(); ipv4 != nil {
			positionDefault = fmt.Sprintf("%s",ipv4)
		}
	}

	position := flag.String("position", positionDefault, "position")

	flag.Parse()
	log.SetFlags(0)

	addr := fmt.Sprintf("%s:%s", *hostname, *port)

	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt)

	log.Println("Sending authentication request...")
	// Send login info for authentication token
	transCfg := &http.Transport{
		TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
	}
	client := &http.Client{Transport: transCfg}

	authUrl := fmt.Sprintf("https://%s:%s/getToken", *authHostname, *authPort)

	authResp, err := client.PostForm(authUrl, url.Values{"username": {*user}, "password": {*password}})

	if err != nil {
		log.Println("Token request failed: ", err)
		return
	}

 	authData, _ := ioutil.ReadAll(authResp.Body)

	authResp.Body.Close()

	var authJson interface{}
	json.Unmarshal(authData, &authJson)

	authMap := authJson.(map[string]interface{})

	log.Println("RECEIVED:")
	bytesJson, _ := json.MarshalIndent(authMap, "", "  ")
	log.Println(string(bytesJson))

	if authMap["success"] == true {

		var token string
		for _, cookie := range authResp.Cookies() {
		  if cookie.Name == "AuthToken" {
			token = cookie.Value
		  }
		}

		log.Printf("Authentication Succeeded. Received AuthToken: %s\n", token)

		// Start websocket handshake
		u := url.URL{Scheme: "ws", Host: addr, Path: "/WebSocket"}
		h := http.Header{"Sec-WebSocket-Protocol": {"tr_json2"}, "Cookie": {fmt.Sprintf("AuthToken=%s;AuthPosition=%s;applicationId=%s;", token, *position, *appId)}}
		log.Printf("Connecting to WebSocket %s ...\n", u.String())

		c, _, err := websocket.DefaultDialer.Dial(u.String(), h)
		if err != nil {
			log.Fatal("WebSocket Connection Failed: ", err)
			return
		} else {
			log.Println("WebSocket successfully connected!")
		}
		defer c.Close()

		done := make(chan struct{})

		go func() {
			defer c.Close()
			defer close(done)

			// Read loop
			for {
				_, message, err := c.ReadMessage()
				if err != nil {
					log.Println("read:", err)
					return
				}

				var jsonArray []map[string]interface{}
				log.Println("RECEIVED: ")
				printJsonBytes(message)
				json.Unmarshal(message, &jsonArray)

				for _,singleMsg := range jsonArray {
					processMessage(c, singleMsg)
				}
			}
		}()

		go func() {
			for {
				time.Sleep(1*time.Second)
			}
		}()

		for {
			select {
			case <-interrupt:
				c.WriteMessage(websocket.CloseMessage, websocket.FormatCloseMessage(websocket.CloseNormalClosure, ""))
				c.Close()
				log.Println("WebSocket Closed")
				return
			}
		}
	} else {
		log.Println("Authentication failed")
	}
}

// Parse JSON message at a high level
func processMessage(c *websocket.Conn, message map[string]interface{} ) {
	switch message["Type"] {
		case "Refresh":
			if(message["Domain"] == "Login"){
				sendMarketPriceRequest(c)
			}
		case "Ping":
			sendMessage(c, []byte(`{"Type":"Pong"}`))
		default:
	}
}

// Create and send simple Market Price request
func sendMarketPriceRequest(c *websocket.Conn) {
	sendMessage(c, []byte(`{"ID":2,"Key":{"Name":"TRI.N"}}`))
}

// Helper to send bytes over WebSocket connection
func sendMessage(c *websocket.Conn, message []byte) {
	log.Println("SENT:")
	printJsonBytes(message)
	err := c.WriteMessage(websocket.TextMessage, message)
	if err != nil {
		log.Println("Send Failed:", err)
	}
}

// Output bytes as formatted JSON
func printJsonBytes(bytes []byte) {
	var dat interface{}
	json.Unmarshal(bytes, &dat)
	bytesJson, _ := json.MarshalIndent(dat, "", "  ")
	log.Println(string(bytesJson))
}
