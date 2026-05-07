package server

import (
	"net/http"
	"time"

	httpSwagger "github.com/swaggo/http-swagger/v2"
	"gorm.io/gorm"

	"go_web/internal/config"
	"go_web/internal/handler"
)

func New(cfg config.Config, db *gorm.DB) *http.Server {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /", handler.Home)
	mux.HandleFunc("GET /healthz", handler.NewHealth(databasePing(db)))
	mux.HandleFunc("GET /api/users", handler.NewListUsers(db))
	mux.HandleFunc("POST /api/users", handler.NewCreateUser(db))
	mux.HandleFunc("POST /api/users/batch_create", handler.NewBatchCreateUsers(db))
	mux.Handle("GET /swagger/", httpSwagger.Handler(httpSwagger.URL("/swagger/doc.json")))

	return &http.Server{
		Addr:              cfg.Address(),
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
		ReadTimeout:       10 * time.Second,
		WriteTimeout:      10 * time.Second,
		IdleTimeout:       60 * time.Second,
	}
}

func databasePing(db *gorm.DB) func() error {
	if db == nil {
		return nil
	}

	sqlDB, err := db.DB()
	if err != nil {
		return func() error { return err }
	}

	return func() error {
		return sqlDB.Ping()
	}
}
