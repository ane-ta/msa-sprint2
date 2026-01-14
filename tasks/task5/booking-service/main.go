package main

import (
	"fmt"
	"log"
	"math/rand" 
	"net/http"
	"os"
	"time"
)

var appVersion = os.Getenv("APP_VERSION")
var errorPossible = os.Getenv("ERROR_POSSIBLE")
var isHealthy = true

func isVersionNewer() bool {
	return appVersion == "v2"
}

func isErrorPossible() bool {
	return errorPossible == "true"
}

func flakyPingHandler(w http.ResponseWriter, r *http.Request) {
	// Инициализируем генератор случайных чисел
	rand.Seed(time.Now().UnixNano())
	
	// В 50% случаев возвращаем ошибку 500
	if rand.Intn(100) < 50 && isErrorPossible() {
		log.Println("Returning 500 Internal Server Error (Flaky endpoint)")
		http.Error(w, "Internal Server Error (Simulated Flaky Response)", http.StatusInternalServerError)
		return
	}

	// В остальных 50% случаев возвращаем успешный ответ
	response := fmt.Sprintf("pong (OK) | Version: %s\n", appVersion)
	fmt.Fprint(w, response)
}

func errorPingHandler(w http.ResponseWriter, r *http.Request) {
	
	if isErrorPossible() {
		log.Println("Returning 500 Internal Server Error (Error endpoint)")
		http.Error(w, "Internal Server Error (Simulated Error Response)", http.StatusInternalServerError)
	}

	// В остальных 50% случаев возвращаем успешный ответ
	response := fmt.Sprintf("pong (OK) | Version: %s\n", appVersion)
	fmt.Fprint(w, response)
}

func xFeatureHandler(w http.ResponseWriter, r *http.Request) {
	response := fmt.Sprintf("Version: %s", appVersion)

	if isVersionNewer() == true {

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
}
func main() {

	if appVersion == "" {
		appVersion = "unknown"
	}

	http.HandleFunc("/ping", func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintf(w, "pong")
	})

	http.HandleFunc("/feature", xFeatureHandler)

	http.HandleFunc("/ping-flaky", flakyPingHandler)
	http.HandleFunc("/ping-error", errorPingHandler)

	http.HandleFunc("/health-status", func(w http.ResponseWriter, r *http.Request) {
        if isHealthy {
            w.WriteHeader(http.StatusOK)
            fmt.Fprintf(w, "Healthy\n")
        } else {
            w.WriteHeader(http.StatusServiceUnavailable) // Возвращаем 503 ошибку
            fmt.Fprintf(w, "Unhealthy\n")
        }
    })
    
    http.HandleFunc("/toggle-health", func(w http.ResponseWriter, r *http.Request) {
        isHealthy = !isHealthy
        status := "Healthy"
        if !isHealthy { status = "Unhealthy" }
        fmt.Fprintf(w, "Health status toggled to %s\n", status)
    })

	fmt.Printf("Service starting version %s on :8080\n", appVersion)
	log.Fatal(http.ListenAndServe(":8080", nil))
}
