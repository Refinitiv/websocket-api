//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|            Copyright (C) 2021 Refinitiv. All rights reserved.        --
//|-----------------------------------------------------------------------------


// Simple example of outputting Market Price JSON data using Websockets with authentication

package main

import (
	"crypto/tls"
	"io/ioutil"
	"flag"
	"fmt"
	"strings"
	"log"
	"os"
	"time"
	"sync"
	"os/signal"
	"net/http"
	"net/url"
	"net"
	"encoding/json"
	"github.com/gorilla/websocket"
)

func getCurrentTime() (string) {
	return time.Now().Format("2006-01-02 15:04:05.000000")
}

func main() {

	usage := "Usage: market_price_rdpgw_client_cred_auth.go --clientid clientid --clientsecret client secret " +
		"[--app_id app_id] [--position position] [--auth_url auth_url] " +
		"[--hostname hostname] [--port port] [--standbyhostname hostname] [--standbyport port] " +
		"[--discovery_url discovery_url] [--scope scope] [--service service] " +
		"[--region region] [--ric ric] [--hotstandby] [--help]"

	// Get command line parameters
	hostname := flag.String("hostname", "", "hostname")
	port := flag.String("port", "443", "websocket port")
	appId := flag.String("app_id", "256", "application id")
	clientId := flag.String("clientid", "", "client id")
	clientSecret := flag.String("clientsecret", "", "client secret")
	authUrl := flag.String("auth_url", "https://api.refinitiv.com/auth/oauth2/v2/token", "authentication url")
	discoveryUrl := flag.String("discovery_url", "https://api.refinitiv.com/streaming/pricing/v1/", "discovery url")
	hostname2 := flag.String("standbyhostname", "", "standby hostname")
	port2 := flag.String("standbyport", "443", "standby port")
	scope := flag.String("scope", "trapi.streaming.pricing.read", "scope")
	service := flag.String("service", "ELEKTRON_DD", "service")
	region := flag.String("region", "us-east-1", "region")
	ric := flag.String("ric", "/TRI.N", "ric")
	hotstandby := flag.Bool("hotstandby", false, "hotstandby")
	help := flag.Bool("help", false, "help")

	positionDefault := ""
	host, _ := os.Hostname()
	addrs, _ := net.LookupIP(host)
	for _, addr := range addrs {
		if ipv4 := addr.To4(); ipv4 != nil {
			positionDefault = fmt.Sprintf("%s",ipv4)
		}
	}

	position := flag.String("position", positionDefault, "position")

	flag.Usage = func() {
		log.Println(usage)
	}

	flag.Parse()
	log.SetFlags(0)

	if *help {
		log.Println(usage)
		return
	}

	if len(*clientId) == 0 || len(*clientSecret) == 0 {
		log.Println("clientid and clientsecret are required options")
		return
	}

	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt)

	token, expired := getAuthToken(*authUrl, *clientId, *clientSecret, *scope)

	tokenTS := time.Now()

	// If hostname is specified, use it for the connection
	addr := fmt.Sprintf("%s:%s", *hostname, *port)
	addr2 := fmt.Sprintf("%s:%s", *hostname2, *port2)

	if len(*hostname) == 0 {
		addr, addr2 = queryServiceDiscovery(*discoveryUrl, token, *region, *hotstandby)
	} else if *hotstandby && len(*hostname2) == 0 {
		addr2 = ""
	}

	log.Println(addr, addr2)

	done := make(chan struct{}, 2)
	connect := make(chan struct{}, 2)
	reconnect := make(chan struct{}, 2)
	newToken := make(chan string, 2)

	var wg sync.WaitGroup

	handler := func(addr string) {
		defer wg.Done()

		closed := make(chan struct{})
		token := ""

		// Main loop
		for {
			select {
			case token = <- newToken:
				break
			case <-done:
				return
			}

			// Start websocket handshake
			u := url.URL{Scheme: "wss", Host: addr, Path: "/WebSocket"}
			h := http.Header{"Sec-WebSocket-Protocol": {"tr_json2"}}
			log.Printf(getCurrentTime() + " Connecting to WebSocket %s ...\n", u.String())

			c, _, err := websocket.DefaultDialer.Dial(u.String(), h)
			if err != nil {
				log.Println(getCurrentTime(), "WebSocket Connection Failed: ", err)
				connect <- struct{}{}
				continue
			} else {
				log.Println(getCurrentTime(), "WebSocket successfully connected!")
			}

			defer c.Close()

			sendLoginRequest(c, *appId, *position, token)

			go func() {
				// Read loop
				for {
					_, message, err := c.ReadMessage()
					if err != nil {
						log.Println(getCurrentTime(), "read:", err)
						closed <- struct{}{}
						return
					}

					var jsonArray []map[string]interface{}
					log.Println(getCurrentTime(), "RECEIVED: ")
					printJsonBytes(message)
					json.Unmarshal(message, &jsonArray)

					for _,jsonMessage := range jsonArray {
						// Parse JSON message at a high level
						switch jsonMessage["Type"] {
							case "Refresh":
								if(jsonMessage["Domain"] == "Login"){
									sendMarketPriceRequest(c, *ric, *service)
								}
							case "Ping":
								sendMessage(c, []byte(`{"Type":"Pong"}`))
							default:
						}
					}
				}
			}()

			select {
			case <-done:
				log.Println(getCurrentTime(), "WebSocket Closed")
				c.WriteMessage(websocket.CloseMessage, websocket.FormatCloseMessage(websocket.CloseNormalClosure, ""))
				c.Close()
				return
			case <-closed:
				reconnect <- struct{}{}
				continue
			}
		}
	}

	defer wg.Wait()

	wg.Add(1)
	go handler(addr)
	newToken <- token
	if *hotstandby && len(addr2) != 0 {
		wg.Add(1)
		go handler(addr2)
		newToken <- token
	}

	for {
		select {
		case <-interrupt:
			done <- struct{}{}
			done <- struct{}{}
			return
		case <-connect:
			// Waiting a few seconds before attempting to reconnect
			time.Sleep(4 * time.Second)
			var deltaTime float64
			if expired < 600 {
				deltaTime = expired * 0.90
			} else {
				deltaTime = 300
			}
			if time.Now().Sub(tokenTS).Seconds() >= deltaTime {
				token, expired = getAuthToken(*authUrl, *clientId, *clientSecret, *scope)
				tokenTS = time.Now()
			}
			newToken <- token
		case <-reconnect:
			// Waiting a few seconds before attempting to reconnect
			time.Sleep(4 * time.Second)
			token, expired = getAuthToken(*authUrl, *clientId, *clientSecret, *scope)
			tokenTS = time.Now()
			newToken <- token
		}
	}
}

// Create and send simple Login request
func sendLoginRequest(c *websocket.Conn, appId string, position string, token string) {
	sendMessage(c, []byte(`{"ID":1,"Domain":"Login","Key":{"NameType":"AuthnToken","Elements":{"ApplicationId":"`+appId+`","Position":"`+position+`","AuthenticationToken":"`+token+`"}}}`))
}

// Create and send simple Market Price request
func sendMarketPriceRequest(c *websocket.Conn, ric string, service string) {
	sendMessage(c, []byte(`{"ID":2,"Key":{"Name":"`+ric+`","Service":"`+service+`"}}`))
}

// Helper to send bytes over WebSocket connection
func sendMessage(c *websocket.Conn, message []byte) {
	log.Println(getCurrentTime(), "SENT:")
	printJsonBytes(message)
	err := c.WriteMessage(websocket.TextMessage, message)
	if err != nil {
		log.Println(getCurrentTime(), "Send Failed:", err)
	}
}

// Output bytes as formatted JSON
func printJsonBytes(bytes []byte) {
	var dat interface{}
	json.Unmarshal(bytes, &dat)
	bytesJson, _ := json.MarshalIndent(dat, "", "  ")
	log.Println(string(bytesJson))
}

func getAuthToken(authUrl string, clientId string, clientSecret string, scope string) (string, float64) {
	log.Println(getCurrentTime(), "Sending authentication request...")
	// Send login info for authentication token
	transCfg := &http.Transport{
		TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
	}
	client := &http.Client{Transport: transCfg,
			CheckRedirect: func(req *http.Request, via []*http.Request) error {
			        return http.ErrUseLastResponse
			},
	}

	authResp, err := client.PostForm(authUrl, url.Values{"grant_type": {"client_credentials"}, "client_id": {clientId}, "client_secret": {clientSecret}, "scope": {scope}})

	if err != nil {
		log.Fatalln(getCurrentTime(), "Token request failed: ", err)
	}

 	authData, _ := ioutil.ReadAll(authResp.Body)

	authResp.Body.Close()

	client.CloseIdleConnections()

	switch authResp.StatusCode {
	case 200:
		break
	case 301, 302, 307, 308:
		// Perform URL redirect
		log.Println(getCurrentTime(), "Refinitiv Data Platform authentication HTTP code: ", authResp.StatusCode)
		newHost := authResp.Header.Get("Location")
		log.Println(getCurrentTime(), "Perform URL redirect to ", newHost)
		return getAuthToken(newHost, clientId, clientSecret, scope)
	case 400, 401, 403, 404, 410, 451:
		// Stop trying the request
		// NOTE: With 400 and 401, there is not retry to keep this sample code simple
		log.Fatalln(getCurrentTime(), "Authentication failed: ", authResp.StatusCode)
	default:
		log.Println(getCurrentTime(), "Authentication failed: ", authResp.StatusCode)
		time.Sleep(5 * time.Second)
		// CAUTION: This is sample code with infinite retries.
		log.Println(getCurrentTime(), "Retrying auth request")
		return getAuthToken(authUrl, clientId, clientSecret, scope)
	}

	var authJson interface{}
	json.Unmarshal(authData, &authJson)

	authMap := authJson.(map[string]interface{})

	log.Println(getCurrentTime(), "RECEIVED:")
	bytesJson, _ := json.MarshalIndent(authMap, "", "  ")
	log.Println(string(bytesJson))

	token := authMap["access_token"].(string)
	expires := authMap["expires_in"].(float64)

	log.Printf("Authentication Succeeded. Received AuthToken: %s\n", token)

	return token, expires
}

func queryServiceDiscovery(discoveryUrl string, token string, region string, hotstandby bool) (string, string) {
	log.Println(getCurrentTime(), "Sending discovery request...")
	// Send login info for authentication token
	transCfg := &http.Transport{
		TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
	}
	client := &http.Client{Transport: transCfg,
			CheckRedirect: func(req *http.Request, via []*http.Request) error {
			        return http.ErrUseLastResponse
			},
	}

	req, err := http.NewRequest("GET", discoveryUrl, nil)

	req.Header.Set("Authorization", "Bearer " + token)

	q := req.URL.Query()
	q.Add("transport", "websocket")
	req.URL.RawQuery = q.Encode()

	discoveryResp, err := client.Do(req)

	if err != nil {
		log.Fatalln(getCurrentTime(), "Discovery request failed: ", err)
	}

 	discoveryData, _ := ioutil.ReadAll(discoveryResp.Body)

	discoveryResp.Body.Close()

	client.CloseIdleConnections()

	switch discoveryResp.StatusCode {
	case 200:
		break
	case 301, 302, 307, 308:
		// Perform URL redirect
		log.Println(getCurrentTime(), "Refinitiv Data Platform service discovery HTTP code: ", discoveryResp.StatusCode)
		newHost := discoveryResp.Header.Get("Location")
		log.Println(getCurrentTime(), "Perform URL redirect to ", newHost)
		return queryServiceDiscovery(newHost, token, region, hotstandby)
	case 403, 404, 410, 451:
		// Stop trying the request
		log.Fatalln(getCurrentTime(), "Discovery failed: ", discoveryResp.StatusCode)
	default:
		log.Println(getCurrentTime(), "Discovery failed: ", discoveryResp.StatusCode)
		time.Sleep(5 * time.Second)
		// CAUTION: This is sample code with infinite retries.
		log.Println(getCurrentTime(), "Retrying the service discovery request")
		return queryServiceDiscovery(discoveryUrl, token, region, hotstandby)
	}

	var discoveryJson interface{}
	json.Unmarshal(discoveryData, &discoveryJson)

	discoveryMap := discoveryJson.(map[string]interface{})

	log.Println(getCurrentTime(), "RECEIVED:")
	bytesJson, _ := json.MarshalIndent(discoveryMap, "", "  ")
	log.Println(string(bytesJson))

	services := discoveryMap["services"].([]interface{})

	var addresses []string
	var backupAddresses [] string

	for index := range services {
		service := services[index].(map[string]interface{})
		location := service["location"].([]interface{})
		if !strings.HasPrefix(location[0].(string), region) {
			continue
		}
		if !hotstandby {
			if len(location) >= 2 {
				addresses = append(addresses, fmt.Sprintf("%s:%g", service["endpoint"].(string), service["port"].(float64)))
				continue
			} else if len(location) == 1 {
				backupAddresses = append(backupAddresses, fmt.Sprintf("%s:%g", service["endpoint"].(string), service["port"].(float64)))
				continue
			}
		} else {
			if len(location) == 1 {
				addresses = append(addresses, fmt.Sprintf("%s:%g", service["endpoint"].(string), service["port"].(float64)))
			}
		}
	}
	
	if hotstandby {
		if len(addresses) < 2 {
			log.Fatalln("Expected 2 hosts but received:", len(addresses), "or the region:", region, "is not present in list of endpoints");
		}
	} else {
		if len(addresses) == 0 {
			if len(backupAddresses) > 0 {
				addresses = backupAddresses
			}
		}
	}

	if len(addresses) == 0 {
		log.Fatalln("The region:", region, "is not present in list of endpoints");
	}

	addr := addresses[0]
	addr2 := ""
	if hotstandby && len(addresses) >= 2 {
		addr2 = addresses[1]
	}

	return addr, addr2
}