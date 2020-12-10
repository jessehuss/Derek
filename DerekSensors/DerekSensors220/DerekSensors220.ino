#include <stdio.h>
#include <ESP8266WebServer.h>
#include <ArduinoJson.h>

#define PORT 80
#define DELAY 500
#define MAX_RETRY 50
#define SWITCH_PIN 0 // D3
#define SECURED_PIN 4 // D2
#define UNSECURED_PIN 5 // D1
#define WIFI_SSD "liam-2.4G"
#define WIFI_PASSWORD "liammail17"

enum SensorStatus {
  UNREACHABLE = 0,
  SECURED = 1,
  UNSECURED = 2
};

struct Sensor {
    byte NodeID;
    String NodeLocation;
    SensorStatus NodeStatus;
} doorSensor;

ESP8266WebServer server(PORT);

void InitSensor()
{
    pinMode(SWITCH_PIN, INPUT_PULLUP);
    pinMode(SECURED_PIN, OUTPUT);
    pinMode(UNSECURED_PIN, OUTPUT);

    doorSensor.NodeID = 220;
    doorSensor.NodeLocation = "Side Door";
    doorSensor.NodeStatus = UNREACHABLE;
}

int InitWiFi() {
    int retries = 0;

    WiFi.mode(WIFI_STA);
    WiFi.begin(WIFI_SSD, WIFI_PASSWORD);

    // check the status of WiFi connection to be WL_CONNECTED
    while ((WiFi.status() != WL_CONNECTED) && (retries < MAX_RETRY)) {
        retries++;
        delay(DELAY);
    }
    return WiFi.status(); // return the WiFi connection status
}

void GetStatus() {
    StaticJsonBuffer<200> jsonBuffer;
    JsonObject& jsonObj = jsonBuffer.createObject();
    char JSONmessageBuffer[200];

    if (doorSensor.NodeID == -1)
        server.send(204);
    else {
        jsonObj["NodeID"] = doorSensor.NodeID;
        jsonObj["NodeLocation"] = doorSensor.NodeLocation;
        jsonObj["NodeStatus"] = (int)doorSensor.NodeStatus;
        jsonObj.prettyPrintTo(JSONmessageBuffer, sizeof(JSONmessageBuffer));
        server.send(200, "application/json", JSONmessageBuffer);
    }
}

void ConfigServerRouting() {
    server.on("/", HTTP_GET, []() {
        server.send(200, "text/html",
            "Welcome to the ESP8266 REST Web Server");
    });
    server.on("/status", HTTP_GET, GetStatus);
}

void setup(void) {
    Serial.begin(115200);
    InitSensor();
    InitWiFi();
    Serial.println(WiFi.localIP());

    ConfigServerRouting();

    server.begin();
}

void loop(void) {
    if (digitalRead(SWITCH_PIN) == HIGH)
    {
        doorSensor.NodeStatus = UNSECURED;
        digitalWrite(SECURED_PIN, LOW);
        digitalWrite(UNSECURED_PIN, HIGH);
    }
    else
    {
        doorSensor.NodeStatus = SECURED;
        digitalWrite(SECURED_PIN, HIGH);
        digitalWrite(UNSECURED_PIN, LOW);
    }
    server.handleClient();
}
