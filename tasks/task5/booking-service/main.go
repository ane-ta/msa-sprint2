package main

import (
	"fmt"
	"log"
	"net/http"
	"os"
)

var appVersion = os.Getenv("APP_VERSION")

func IsVersionXFeatureEnabled() bool {
	return appVersion == "v2"
}

func main() {

	if appVersion == "" {
		appVersion = "unknown"
	}

	http.HandleFunc("/ping", func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintf(w, "pong")
	})

	http.HandleFunc("/feature", func(w http.ResponseWriter, r *http.Request) {
		
		response := fmt.Sprintf("Version: %s", appVersion)

		if  IsVersionXFeatureEnabled() == true {

			featureEnabledHeader := r.Header.Get("X-Feature-Enabled")

			if featureEnabledHeader == "true" {
				response += " | Feature Status: Enabled for request"
			} else {
				response += " | Feature Status: Disabled for request"
			}
		} else {
				response += " | Feature Status: Disabled"
		
		}
		fmt.Fprintf(w, response + "\n")
	})

	fmt.Printf("Service starting version %s on :8080\n", appVersion)
	log.Fatal(http.ListenAndServe(":8080", nil))
}
