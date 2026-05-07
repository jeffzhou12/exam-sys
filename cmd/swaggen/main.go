package main

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
)

func main() {
	swagPath, err := resolveSwagPath()
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	rootDir, err := projectRoot()
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	cmd := exec.Command(swagPath, "init", "-g", "cmd/server/main.go", "-o", "docs")
	cmd.Dir = rootDir
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	cmd.Stdin = os.Stdin

	if err := cmd.Run(); err != nil {
		fmt.Fprintf(os.Stderr, "run swag: %v\n", err)
		os.Exit(1)
	}
}

func resolveSwagPath() (string, error) {
	if path, err := exec.LookPath("swag"); err == nil {
		return path, nil
	}

	homeDir, err := os.UserHomeDir()
	if err != nil {
		return "", fmt.Errorf("find swag executable: %w", err)
	}

	fileName := "swag"
	if runtime.GOOS == "windows" {
		fileName = "swag.exe"
	}

	candidate := filepath.Join(homeDir, "go", "bin", fileName)
	if _, err := os.Stat(candidate); err == nil {
		return candidate, nil
	}

	return "", fmt.Errorf("swag executable not found; run 'go install github.com/swaggo/swag/cmd/swag@latest' and ensure it is available in PATH or %%USERPROFILE%%\\go\\bin")
}

func projectRoot() (string, error) {
	workingDir, err := os.Getwd()
	if err != nil {
		return "", fmt.Errorf("resolve project root: %w", err)
	}

	rootDir := filepath.Clean(filepath.Join(workingDir, "../.."))
	if _, err := os.Stat(filepath.Join(rootDir, "go.mod")); err != nil {
		return "", fmt.Errorf("resolve project root: go.mod not found in %s", rootDir)
	}

	return rootDir, nil
}