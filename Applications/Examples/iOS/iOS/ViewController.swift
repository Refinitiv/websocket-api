//|-----------------------------------------------------------------------------
//|            This source code is provided under the Apache 2.0 license      --
//|  and is provided AS IS with no warranty or guarantee of fit for purpose.  --
//|                See the project's LICENSE.md for details.                  --
//|           Copyright (C) Refinitiv 2019. All rights reserved.              --
//|-----------------------------------------------------------------------------

import UIKit
import Starscream
import SwiftyJSON

class ViewController: UIViewController, WebSocketDelegate, UITableViewDelegate, UITableViewDataSource, URLSessionDelegate, UITextFieldDelegate {

    // Global variables
    var socket = WebSocket(url: NSURL(string: "")! as URL)
    var hostname = ""
    var port = ""
    var username = ""
    var password = ""
    var position = "127.0.0.1"
    var authHostname = ""
    var authPort = ""
    var authentication = false
    var authToken = ""
    var appId = ""
    var postId = 1
    var pingTimeoutInterval = 30.0
    var postInterval = 3.0
    var pingSendTime = 0.0
    var pingTimeoutTime = 0.0
    var nextPostTime = 0.0
    var running = false
    var selectedTable = 0
    var useAuthenticationSwitch = UISwitch()

    let tableContents: [String] = ["Market Price", "Market Price Batch View", "Market Price Post", "Authentication"]
    let cellReuseIdentifier = "cell"

    // UI Elements
    @IBOutlet weak var hostnameField: UITextField!
    @IBOutlet weak var portField: UITextField!
    @IBOutlet weak var usernameField: UITextField!
    @IBOutlet weak var passwordField: UITextField!
    @IBOutlet weak var goButton: UIBarButtonItem!
    @IBOutlet weak var closeButton: UIBarButtonItem!
    @IBOutlet weak var clearButton: UIBarButtonItem!
    @IBOutlet weak var textView: UITextView!
    @IBOutlet weak var tableView: UITableView!
    @IBOutlet weak var authHostnameField: UITextField!
    @IBOutlet weak var authPortField: UITextField!
    @IBOutlet weak var usernameLabel: UILabel!
    @IBOutlet weak var passwordLabel: UILabel!
    @IBOutlet weak var authHostnameLabel: UILabel!
    @IBOutlet weak var authPortLabel: UILabel!

    override func viewDidLoad() {
        super.viewDidLoad()
        socket.delegate = self

        tableView.register(UITableViewCell.self, forCellReuseIdentifier: cellReuseIdentifier)
        tableView.delegate = self
        tableView.dataSource = self

        hostnameField.delegate = self
        portField.delegate = self
        usernameField.delegate = self
        passwordField.delegate = self

        useAuthenticationSwitch.addTarget(self, action: #selector(authenticationSwitchEvent), for: .valueChanged)
        useAuthenticationSwitch.isAccessibilityElement = true
        useAuthenticationSwitch.accessibilityIdentifier = "authSwitch"
        goButton.action = #selector(goButtonAction(sender:))
        goButton.isAccessibilityElement = true
        goButton.accessibilityIdentifier = "goButton"
        closeButton.action = #selector(closeButtonAction(sender:))
        closeButton.isAccessibilityElement = true
        closeButton.accessibilityIdentifier = "closeButton"
        clearButton.action = #selector(clearButtonAction(sender:))
        clearButton.isAccessibilityElement = true
        clearButton.accessibilityIdentifier = "clearButton"

        let indexPath = IndexPath(row: 0, section: 0)
        tableView.selectRow(at: indexPath, animated: false, scrollPosition: .none)
        tableView(tableView, didSelectRowAt: indexPath)

        if let addr = self.getWiFiAddress() {
            position = addr
        }
    }


    func authenticationSwitchEvent() {
        authentication = useAuthenticationSwitch.isOn

        let alpha = CGFloat(authentication ? 1 : 0.5)

        authHostnameField.isUserInteractionEnabled = authentication
        authHostnameLabel.alpha = alpha
        authHostnameField.alpha = alpha
        authPortField.isUserInteractionEnabled = authentication
        authPortLabel.alpha = alpha
        authPortField.alpha = alpha
        usernameField.isUserInteractionEnabled = authentication
        usernameLabel.alpha = alpha
        usernameField.alpha = alpha
        passwordField.isUserInteractionEnabled = authentication
        passwordLabel.alpha = alpha
        passwordField.alpha = alpha
    }

    func goButtonAction(sender: UIBarButtonItem) {
        hostname = hostnameField.text!
        port = portField.text!
        username = usernameField.text!
        password = passwordField.text!
        appId = "256"

        if authentication {
            authHostname = authHostnameField.text!
            authPort = authPortField.text!
            appId = "555"

            // Perform authentication
            appendToTextView(text: "Sending authentication request...")

            let authUrl = URL(string: "https://\(authHostname):\(authPort)/getToken")!
            var request = URLRequest(url: authUrl)
            request.httpMethod = "POST"
            let postString = "username=\(username)&password=\(password)"
            request.httpBody = postString.data(using: .utf8)

            let session = URLSession(configuration: URLSessionConfiguration.default, delegate: self, delegateQueue: OperationQueue.current)

            // Send post
            let task = session.dataTask(with: request) { data, response, error in
                guard let data = data, error == nil else {
                    print("Error=\(String(describing: error))")
                    return
                }

                // Post successful
                if let httpUrlResponse = response as? HTTPURLResponse, httpUrlResponse.statusCode == 200 {

                    let responseString = String(data: data, encoding: .utf8)

                    // Get JSON
                    if let responseData = responseString?.data(using: .utf8, allowLossyConversion: false) {

                        let responseJson = JSON(data: responseData)

                        if let prettyJsonString = responseJson.rawString() {
                            self.appendToTextView(text: "RECEIVED:")
                            self.appendToTextView(text: prettyJsonString)
                        }

                        // Authentication Succeeded
                        if responseJson["success"].bool == true {

                            // Get Token
                            let cookies = HTTPCookie.cookies(withResponseHeaderFields: httpUrlResponse.allHeaderFields as! [String : String], for: authUrl)
                            for cookie in cookies {
                                if cookie.name == "AuthToken" {
                                    self.authToken = cookie.value
                                }
                            }

                            // Start WebSocket connection with cookie authentication
                            self.appendToTextView(text: "Authentication Succeeded. Received AuthToken: \(self.authToken)")
                            self.startWebSocketConnection()
                        } else {
                            self.appendToTextView(text: "Authentication failed")
                        }
                    }
                }

            }
            task.resume()
        } else {
            startWebSocketConnection()
        }



    }

    func startWebSocketConnection() {
        // Start WebSocket connection
        appendToTextView(text: "Connecting to WebSocket ws://\(hostname):\(port)/WebSocket ...")
        socket = WebSocket(url: NSURL(string: "ws://\(hostname):\(port)/WebSocket")! as URL, protocols: ["tr_json2"])
        socket.delegate = self
        if authentication {
            socket.headers["Cookie"] = "AuthToken=\(authToken);AuthPosition=\(position);applicationId=\(appId);"
        }
        socket.connect()
        running = true

        // Set pings
        pingSendTime = Date().timeIntervalSince1970 + pingTimeoutInterval / 3
        pingTimeoutTime = 0.0

        // Create ping/posting thread
        DispatchQueue.global(qos: .background).async {

            while self.running {
                sleep(1)

                if self.socket.isConnected {
                    // Post timer
                    if self.nextPostTime != 0.0 && Date().timeIntervalSince1970 >= self.nextPostTime {
                        DispatchQueue.main.async {
                            self.sendPost(socket: self.socket)
                            self.nextPostTime = Date().timeIntervalSince1970 + self.postInterval
                        }
                    }

                    // If we didn't receive any traffic for a while, send a Ping.
                    // This is an optional behavior that can be used to monitor connection health.
                    if self.pingSendTime > 0.0 && Date().timeIntervalSince1970 > self.pingSendTime {
                        DispatchQueue.main.async {
                            let pingJson = JSON(["Type":"Ping"])
                            if let pingJsonString = pingJson.rawString() {
                                self.appendToTextView(text: "SENT:")
                                self.appendToTextView(text: pingJsonString)
                                self.socket.write(string: pingJsonString)
                            }
                        }
                        self.pingSendTime = 0.0;
                        self.pingTimeoutTime = Date().timeIntervalSince1970 + self.pingTimeoutInterval;
                    }

                    // If we sent a Ping but haven't received a Pong from the server, disconnect.
                    if self.pingTimeoutTime > 0 && Date().timeIntervalSince1970 > self.pingTimeoutTime {
                        DispatchQueue.main.async {
                            self.appendToTextView(text: "No ping from server, timing out")
                            self.sendCloseMessage(socket: self.socket)
                            self.socket.disconnect()
                        }
                        self.running = false
                    }
                }
            }
        }
    }

    func closeButtonAction(sender: UIBarButtonItem) {
        sendCloseMessage(socket: socket)
        socket.disconnect()
        running = false
    }

    func clearButtonAction(sender: UIBarButtonItem) {
        clearTextView()
    }

    func appendToTextView(text: String) {
        let previousText = textView.text
        let newText = previousText! + "\n" + text
        textView.text = newText
    }

    func clearTextView() {
        textView.text = ""
    }

    func websocketDidConnect(_ socket: WebSocket) {
        appendToTextView(text: "WebSocket successfully connected!")

        if !authentication {
            sendLoginRequest(socket: socket)
        }
    }

    func websocketDidDisconnect(_ socket: WebSocket, error: NSError?) {
    }

    func websocketDidReceiveMessage(_ socket: WebSocket, text: String) {
        appendToTextView(text: "RECEIVED:")

        if let stringData = text.data(using: .utf8, allowLossyConversion: false) {
            let jsonArray = (JSON(data: stringData))

            if let prettyJsonString = jsonArray.rawString() {
                appendToTextView(text: prettyJsonString)
            }

            for (_,singleMsg):(String, JSON) in jsonArray {
                processMessage(socket: socket, json: singleMsg)
            }

        }

        pingSendTime = Date().timeIntervalSince1970 + pingTimeoutInterval / 3
        pingTimeoutTime = 0
    }

    func processMessage(socket: WebSocket, json: JSON) {

        let messageType = json["Type"].stringValue
        if messageType == "Refresh" {
            if let messageDomain = json["Domain"].string {
                if messageDomain == "Login" {
                    pingTimeoutInterval = Double(json["Elements"]["PingTimeout"].int!)
                    if let row = self.tableView.indexPathForSelectedRow?.row {
                        sendRequest(socket: socket, row: row)
                    }
                }
            }

            if json["ID"].int == 2 {
                if tableContents[selectedTable] == "Market Price Post" && nextPostTime == 0.0 && (!json["State"].exists() || json["State"]["Stream"].string == "Open" && json["State"]["Data"] == "Ok") {
                    nextPostTime = Date().timeIntervalSince1970 + postInterval
                }
            }
        } else if messageType == "Ping" {
            let pongJson = JSON(["Type":"Pong"])
            if let pongJsonString = pongJson.rawString() {
                appendToTextView(text: "SENT:")
                appendToTextView(text: pongJsonString)
                socket.write(string: pongJsonString)
            }
        }
    }

    func websocketDidReceiveData(_ socket: WebSocket, data: Data) {
    }

    // Generate a login request and send
    func sendLoginRequest(socket: WebSocket) {
        var loginJson = JSON(
            [
                "ID":1,
                "Domain":"Login",
                "Key":[
                    "Name":"",
                    "Elements":[
                        "ApplicationId":"",
                        "Position":""
                    ]
                ]
            ])
        loginJson["Key"]["Name"].string = username
        loginJson["Key"]["Elements"]["ApplicationId"].string = appId
        loginJson["Key"]["Elements"]["Position"].string = position
        if let loginJsonString = loginJson.rawString() {
            appendToTextView(text: "SENT:")
            appendToTextView(text: loginJsonString)
            socket.write(string: loginJsonString)
        }
    }

    // Send the user specified request
    func sendRequest(socket: WebSocket, row: Int) {
        var marketPriceJson :JSON
        switch row {
        // Market Price
        case 0:
            marketPriceJson = JSON(
                [
                    "ID":2,
                    "Key":[
                        "Name":"TRI.N"
                    ]
                ])
        // Market Price Batch View
        case 1:
            marketPriceJson = JSON(
                [
                    "ID":2,
                    "Key":[
                        "Name":[
                            "TRI.N",
                            "IBM.N",
                            "T.N"
                        ]
                    ],
                    "View":[
                        "BID",
                        "ASK",
                        "BIDSIZE"
                    ]
                ])
        // Market Price Post
        case 2:
            marketPriceJson = JSON(
                [
                    "ID":2,
                    "Key":[
                        "Name":"TRI.N"
                    ]
                ])
        default:
            marketPriceJson = JSON(
                [
                    "ID":2,
                    "Key":[
                        "Name":"TRI.N"
                    ]
                ])
        }
        if let marketPriceJsonString = marketPriceJson.rawString() {
            appendToTextView(text: "SENT:")
            appendToTextView(text: marketPriceJsonString)
            socket.write(string: marketPriceJsonString)
        }
    }

    // Create and send a close message
    func sendCloseMessage(socket: WebSocket) {
        var closeJson = JSON(
            [
                "ID":1,
                "Type":"Close"
            ])
        if authentication {
            closeJson["ID"].int = -1
        }
        if let closeJsonString = closeJson.rawString() {
            appendToTextView(text: "SENT:")
            appendToTextView(text: closeJsonString)
            socket.write(string: closeJsonString)
        }
    }

    // Send a post message
    func sendPost(socket: WebSocket) {
        var postJson = JSON(
            [
                "ID":2,
                "Type":"Post",
                "Domain":"MarketPrice",
                "Ack":true,
                "PostID":1,
                "PostUserInfo":[
                    "Address":"",
                    "UserID":1
                ],
                "Message":[
                    "ID":0,
                    "Type":"Update",
                    "Fields":[
                        "BID":45.55,
                        "BIDSIZE":18,
                        "ASK":45.57,
                        "ASKSIZE":19
                    ]
                ]
            ])
        postJson["PostID"].int = postId
        postJson["PostUserInfo"]["Address"].string = position
        
        if let postJsonString = postJson.rawString() {
            appendToTextView(text: "SENT:")
            appendToTextView(text: postJsonString)
            socket.write(string: postJsonString)
        }
        postId += 1
    }

    // Table rows
    func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return self.tableContents.count
    }

    // Initialize table
    func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell:UITableViewCell = self.tableView.dequeueReusableCell(withIdentifier: cellReuseIdentifier) as UITableViewCell!

        if indexPath.section == 0 && indexPath.row == 3 {
            cell.accessoryView = useAuthenticationSwitch
        } else {
            cell.isAccessibilityElement = true
            cell.accessibilityIdentifier = "tableCell" + self.tableContents[indexPath.row].replacingOccurrences(of: " ", with: "")
            print(cell.accessibilityIdentifier ?? "none")
        }
        cell.selectionStyle = UITableViewCellSelectionStyle.none
        cell.textLabel?.text = self.tableContents[indexPath.row]

        return cell
    }

    // Table row select event
    func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        if !(indexPath.section == 0 && indexPath.row == 3) {
            if let cell = tableView.cellForRow(at: indexPath) {
                cell.accessoryType = UITableViewCellAccessoryType.checkmark
                selectedTable = indexPath.row
            }
        }
    }

    // Table row deselect event
    func tableView(_ tableView: UITableView, didDeselectRowAt indexPath: IndexPath) {
        if !(indexPath.section == 0 && indexPath.row == 3) {
            if let cell = tableView.cellForRow(at:  indexPath) {
                cell.accessoryType = UITableViewCellAccessoryType.none
            }
        }
    }

    // Get the IP address of thie device
    func getWiFiAddress() -> String? {
        var address : String?

        // Get list of all interfaces on the local machine:
        var ifaddr : UnsafeMutablePointer<ifaddrs>?
        guard getifaddrs(&ifaddr) == 0 else { return nil }
        guard let firstAddr = ifaddr else { return nil }

        // For each interface ...
        for ifptr in sequence(first: firstAddr, next: { $0.pointee.ifa_next }) {
            let interface = ifptr.pointee

            // Check for IPv4 or IPv6 interface:
            let addrFamily = interface.ifa_addr.pointee.sa_family
            if addrFamily == UInt8(AF_INET) || addrFamily == UInt8(AF_INET6) {

                // Check interface name:
                let name = String(cString: interface.ifa_name)
                if  name == "en0" || name == "en1" {

                    // Convert interface address to a human readable string:
                    var addr = interface.ifa_addr.pointee
                    var hostname = [CChar](repeating: 0, count: Int(NI_MAXHOST))
                    getnameinfo(&addr, socklen_t(interface.ifa_addr.pointee.sa_len),
                                &hostname, socklen_t(hostname.count),
                                nil, socklen_t(0), NI_NUMERICHOST)
                    address = String(cString: hostname)
                }
            }
        }
        freeifaddrs(ifaddr)

        return address
    }

    // Disable SSL verification
    func urlSession(_ session: URLSession, didReceive challenge: URLAuthenticationChallenge, completionHandler: @escaping (URLSession.AuthChallengeDisposition, URLCredential?) -> Void) {
        completionHandler(.useCredential, URLCredential(trust: challenge.protectionSpace.serverTrust!))
    }

    func textFieldShouldReturn(_ textField: UITextField) -> Bool {
        textField.resignFirstResponder()
        return true
    }
}
