//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright Thomson Reuters 2017. All rights reserved.            --
//|-----------------------------------------------------------------------------


// Simple example of outputting Market Price JSON data using Websockets

package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"time"
	"os/signal"
	"net/http"
	"net/url"
	"net"
	"bytes"
	"encoding/json"
	"github.com/gorilla/websocket"
)

func main() {

	// Get command line parameters
	hostname := flag.String("hostname", "127.0.0.1", "hostname")
	port := flag.String("port", "15000", "websocket port")
	user := flag.String("user", "root", "user")
	appId := flag.String("app_id", "256", "application id")

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

	// Start websocket handshake
	u := url.URL{Scheme: "ws", Host: addr, Path: "/WebSocket"}
	h := http.Header{"Sec-WebSocket-Protocol": {"tr_json2"}}
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

	// Generate a login request from command line data (or defaults) and send
	loginMessageBytes := []byte(`{"ID":1,"Domain":"Login","Key":{"Name":"<USER>","Elements":{"ApplicationId":"<APP_ID>","Position":"<POSITION>"}}}`)
	loginMessageBytes = bytes.Replace(loginMessageBytes, []byte("<USER>"), []byte(*user), 1)
	loginMessageBytes = bytes.Replace(loginMessageBytes, []byte("<APP_ID>"), []byte(*appId), 1)
	loginMessageBytes = bytes.Replace(loginMessageBytes, []byte("<POSITION>"), []byte(*position), 1)
	sendMessage(c, loginMessageBytes)

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
