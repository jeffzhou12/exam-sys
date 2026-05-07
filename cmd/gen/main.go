package main

import (
	"log"

	"gorm.io/gen"

	"go_web/internal/config"
	"go_web/internal/store"
)

func main() {
	cfg := config.Load()
	db, err := store.OpenPostgres(cfg.Database)
	if err != nil {
		log.Fatalf("open postgres: %v", err)
	}

	generator := gen.NewGenerator(gen.Config{
		OutPath:      "internal/dal/query",
		ModelPkgPath: "model",
		Mode:         gen.WithDefaultQuery | gen.WithQueryInterface | gen.WithoutContext,
	})

	generator.UseDB(db)
	models := generator.GenerateAllTable()
	generator.ApplyBasic(models...)
	generator.Execute()
}
