# ── Build stage ───────────────────────────────────────────────────────────────
FROM golang:1.26-alpine AS builder

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -trimpath -ldflags="-s -w" -o go_web ./cmd/server

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM alpine:3.21

# ca-certificates: system TLS roots; tzdata: timezone support
RUN apk --no-cache add ca-certificates tzdata

WORKDIR /app

COPY --from=builder /app/go_web .
# Embed the Amazon RDS global CA bundle for sslmode=verify-full
COPY global-bundle.pem .

EXPOSE 8080

ENTRYPOINT ["./go_web"]
