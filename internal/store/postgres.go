package store

import (
	"context"
	"time"

	"gorm.io/driver/postgres"
	"gorm.io/gorm"

	"go_web/internal/config"
)

const pingTimeout = 5 * time.Second

func OpenPostgres(cfg config.DatabaseConfig) (*gorm.DB, error) {
	db, err := gorm.Open(postgres.Open(cfg.DSN()), &gorm.Config{})
	if err != nil {
		return nil, err
	}

	sqlDB, err := db.DB()
	if err != nil {
		return nil, err
	}

	sqlDB.SetMaxIdleConns(cfg.MaxIdleConns)
	sqlDB.SetMaxOpenConns(cfg.MaxOpenConns)
	sqlDB.SetConnMaxLifetime(cfg.ConnMaxLifetime)

	ctx, cancel := context.WithTimeout(context.Background(), pingTimeout)
	defer cancel()

	if err := sqlDB.PingContext(ctx); err != nil {
		return nil, err
	}

	return db, nil
}
