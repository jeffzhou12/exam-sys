package handler

import (
	"encoding/json"
	"net/http"
	"time"
)

type healthResponse struct {
	Status   string `json:"status"`
	Database string `json:"database,omitempty"`
	Time     string `json:"time"`
}

// NewHealth godoc
// @Summary Health check
// @Description Returns application and database health status.
// @Tags system
// @Produce json
// @Success 200 {object} healthResponse
// @Failure 503 {object} healthResponse
// @Router /healthz [get]
func NewHealth(checkDatabase func() error) http.HandlerFunc {
	return func(w http.ResponseWriter, _ *http.Request) {
		response := healthResponse{
			Status: "ok",
			Time:   time.Now().UTC().Format(time.RFC3339),
		}

		statusCode := http.StatusOK
		if checkDatabase != nil {
			if err := checkDatabase(); err != nil {
				response.Status = "degraded"
				response.Database = err.Error()
				statusCode = http.StatusServiceUnavailable
			} else {
				response.Database = "ok"
			}
		}

		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(statusCode)
		_ = json.NewEncoder(w).Encode(response)
	}
}
