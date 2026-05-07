package main

//go:generate go run ../swaggen

import (
	"errors"
	"log"
	"net/http"

	_ "go_web/docs"
	"go_web/internal/config"
	"go_web/internal/server"
	"go_web/internal/store"
)

// @title go_web API
// @version 1.0
// @description Go Web demo API with PostgreSQL and GORM Gen.
// @BasePath /
// @schemes http
func main() {
	cfg := config.Load()
	db, err := store.OpenPostgres(cfg.Database)
	if err != nil {
		log.Fatalf("open postgres: %v", err)
	}

	httpServer := server.New(cfg, db)

	log.Printf("starting server on %s", httpServer.Addr)
	if err := httpServer.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
		log.Fatalf("server stopped: %v", err)
	}
}
