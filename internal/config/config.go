package config

import (
	"bufio"
	"context"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"time"

	awscfg "github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/service/secretsmanager"
)

const defaultPort = "8080"

type Config struct {
	HTTP     HTTPConfig
	Database DatabaseConfig
}

type HTTPConfig struct {
	Port string
}

type DatabaseConfig struct {
	Host            string
	Port            string
	User            string
	Password        string
	Name            string
	SSLMode         string
	SSLRootCert     string
	TimeZone        string
	MaxIdleConns    int
	MaxOpenConns    int
	ConnMaxLifetime time.Duration
}

func Load() Config {
	loadDotEnv()

	port := os.Getenv("PORT")
	if port == "" {
		port = defaultPort
	}

	db := DatabaseConfig{
		Host:            getEnv("DB_HOST", "localhost"),
		Port:            getEnv("DB_PORT", "5432"),
		User:            getEnv("DB_USER", "postgres"),
		Password:        os.Getenv("DB_PASSWORD"),
		Name:            getEnv("DB_NAME", "go_web"),
		SSLMode:         getEnv("DB_SSLMODE", "disable"),
		SSLRootCert:     os.Getenv("DB_SSL_ROOT_CERT"),
		TimeZone:        getEnv("DB_TIMEZONE", "UTC"),
		MaxIdleConns:    getEnvAsInt("DB_MAX_IDLE_CONNS", 10),
		MaxOpenConns:    getEnvAsInt("DB_MAX_OPEN_CONNS", 30),
		ConnMaxLifetime: time.Duration(getEnvAsInt("DB_CONN_MAX_LIFETIME_MINUTES", 30)) * time.Minute,
	}

	if arn := os.Getenv("DB_PASSWORD_SECRET_ARN"); arn != "" {
		db.Password = mustFetchSecretPassword(arn)
	}

	return Config{
		HTTP: HTTPConfig{
			Port: port,
		},
		Database: db,
	}
}

func (c Config) Address() string {
	return ":" + c.HTTP.Port
}

func (c DatabaseConfig) DSN() string {
	dsn := fmt.Sprintf(
		"host=%s port=%s user=%s password=%s dbname=%s sslmode=%s TimeZone=%s",
		c.Host,
		c.Port,
		c.User,
		c.Password,
		c.Name,
		c.SSLMode,
		c.TimeZone,
	)
	if c.SSLRootCert != "" {
		dsn += " sslrootcert=" + c.SSLRootCert
	}
	return dsn
}

// mustFetchSecretPassword retrieves the "password" field from an AWS Secrets
// Manager secret. It terminates the process on any error, since the
// application cannot start without a valid database password.
func mustFetchSecretPassword(secretARN string) string {
	ctx := context.Background()

	cfg, err := awscfg.LoadDefaultConfig(ctx)
	if err != nil {
		log.Fatalf("config: failed to load AWS config: %v", err)
	}

	client := secretsmanager.NewFromConfig(cfg)
	result, err := client.GetSecretValue(ctx, &secretsmanager.GetSecretValueInput{
		SecretId: &secretARN,
	})
	if err != nil {
		log.Fatalf("config: failed to get secret %s: %v", secretARN, err)
	}

	var secret struct {
		Password string `json:"password"`
	}
	if err := json.Unmarshal([]byte(*result.SecretString), &secret); err != nil {
		log.Fatalf("config: failed to parse secret JSON: %v", err)
	}

	return secret.Password
}

func getEnv(key string, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}

	return value
}

func getEnvAsInt(key string, fallback int) int {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}

	parsed, err := strconv.Atoi(value)
	if err != nil {
		return fallback
	}

	return parsed
}

func loadDotEnv() {
	for _, candidate := range dotenvCandidates() {
		if err := loadEnvFile(candidate); err == nil {
			return
		}
	}
}

func dotenvCandidates() []string {
	workingDir, err := os.Getwd()
	if err != nil {
		return []string{".env"}
	}

	current := workingDir
	candidates := make([]string, 0, 4)
	seen := map[string]struct{}{}

	for {
		candidate := filepath.Join(current, ".env")
		if _, exists := seen[candidate]; !exists {
			candidates = append(candidates, candidate)
			seen[candidate] = struct{}{}
		}

		parent := filepath.Dir(current)
		if parent == current {
			break
		}

		current = parent
	}

	return candidates
}

func loadEnvFile(path string) error {
	file, err := os.Open(path)
	if err != nil {
		return err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}

		key, value, ok := strings.Cut(line, "=")
		if !ok {
			continue
		}

		key = strings.TrimSpace(key)
		if key == "" || os.Getenv(key) != "" {
			continue
		}

		value = strings.TrimSpace(value)
		value = strings.Trim(value, `"'`)
		_ = os.Setenv(key, value)
	}

	return scanner.Err()
}
