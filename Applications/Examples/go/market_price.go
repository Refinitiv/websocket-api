//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.
//|                See the project's LICENSE.md for details.
//|           Copyright (C) 2017-2020,2024,2026 LSEG. All rights reserved.
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
	"strconv"
)

func main() {

	// Get command line parameters
	hostname := flag.String("hostname", "127.0.0.1", "hostname")
	port := flag.String("port", "15000", "websocket port")
	user := flag.String("user", "root", "user")
	appId := flag.String("app_id", "256", "application id")
	snapshot := flag.Bool("snapshot", false, "snapshot")
	service := flag.String("service", "", "service")
	updateTypeFilter := flag.Int("updateTypeFilter", -1, "updateTypeFilter")	// See details below about Update Type Filter
	negativeUpdateTypeFilter:= flag.Int("negativeUpdateTypeFilter", -1, "negativeUpdateTypeFilter")		// See details below about Negative Update Type Filter

        /*
            - Update Type Filters -
            Update Type Filter specifies the update types DESIRED

            Negative Update Type Filter specifies the update types NOT DESIRED

            A single value, converted into binary, can be used to determine what Update Types
            you want filtered in your responses.

            UPDATE TYPE FILTERS
            Unspecified = 0x001
            Quote = 0x002
            Trade = 0x004
            News Alert = 0x008
            Volume Alert = 0x010
            Order Indication = 0x020
            Closing Run = 0x040
            Correction = 0x080
            Market Digest = 0x100
            Quotes Trade = 0x200
            Multiple = 0x400
            Verify = 0x800
         */

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
				processMessage(c, singleMsg, snapshot, service)
			}
		}
	}()

	// Generate a login request from command line data (or defaults) and send
	updateTypeFilterText := ""
	negativeUpdateTypeFilterText := ""
	if *updateTypeFilter >= 0 {
		updateTypeFilterText += `,"UpdateTypeFilter":`
		updateTypeFilterText = fmt.Sprint(updateTypeFilterText, strconv.Itoa(*updateTypeFilter))
	}
	if *negativeUpdateTypeFilter >= 0 {
		negativeUpdateTypeFilterText += `,"NegativeUpdateTypeFilter":`
		negativeUpdateTypeFilterText = fmt.Sprint(negativeUpdateTypeFilterText, strconv.Itoa(*negativeUpdateTypeFilter))
	}
	
	loginMessageBytes := []byte(`{"ID":1,"Domain":"Login","Key":{"Name":"<USER>","Elements":{"ApplicationId":"<APP_ID>","Position":"<POSITION>"` + updateTypeFilterText + negativeUpdateTypeFilterText + `}}}`)
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
func processMessage(c *websocket.Conn, message map[string]interface{}, snapshot *bool, service *string) {
	switch message["Type"] {
		case "Refresh":
			if(message["Domain"] == "Login"){
				sendMarketPriceRequest(c, snapshot, service)
			}
		case "Ping":
			sendMessage(c, []byte(`{"Type":"Pong"}`))
		default:
	}
}

// Create and send simple Market Price request
func sendMarketPriceRequest(c *websocket.Conn, snapshot *bool, service *string) {
	serviceInfo := ""
        if len(*service) > 1 {
		serviceInfo += `, "Service":"` + *service + `"`
	}
	sendMessage(c, []byte(`{"ID":2,"Key":{"Name":"TRI.N"` + serviceInfo + `},` + `"Streaming":` + strconv.FormatBool(!*snapshot) + `}`))
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
