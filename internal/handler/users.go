package handler

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strconv"
	"sync"

	"gorm.io/gorm"

	"go_web/internal/dal/model"
	"go_web/internal/dal/query"
)

const (
	defaultPage     = 1
	defaultPageSize = 10
	maxPageSize     = 100
)

type usersListResponse struct {
	Data     []*model.User    `json:"data"`
	Page     int              `json:"page"`
	PageSize int              `json:"page_size"`
	Total    int64            `json:"total"`
	Filters  usersListFilters `json:"filters"`
	HasMore  bool             `json:"has_more"`
}

type usersListFilters struct {
	Name   string `json:"name,omitempty"`
	MinAge int32  `json:"min_age,omitempty"`
	MaxAge int32  `json:"max_age,omitempty"`
}

type errorResponse struct {
	Error string `json:"error"`
}

type userCreateRequest struct {
	Name string `json:"name"`
	Age  int32  `json:"age"`
}

// NewListUsers godoc
// @Summary List users
// @Description Returns a paginated user list, optionally filtered by name.
// @Tags users
// @Produce json
// @Param page query int false "Page number" minimum(1) default(1)
// @Param page_size query int false "Page size" minimum(1) maximum(100) default(10)
// @Param name query string false "Filter by partial user name"
// @Param min_age query int false "Filter by minimum age" minimum(0)
// @Param max_age query int false "Filter by maximum age" minimum(0)
// @Success 200 {object} usersListResponse
// @Failure 400 {object} errorResponse
// @Failure 500 {object} errorResponse
// @Router /api/users [get]
func NewListUsers(db *gorm.DB) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		page, err := parsePositiveInt(r.URL.Query().Get("page"), defaultPage)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorResponse{Error: "invalid page"})
			return
		}

		pageSize, err := parsePositiveInt(r.URL.Query().Get("page_size"), defaultPageSize)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorResponse{Error: "invalid page_size"})
			return
		}
		if pageSize > maxPageSize {
			pageSize = maxPageSize
		}

		name := r.URL.Query().Get("name")
		minAge := r.URL.Query().Get("min_age")
		maxAge := r.URL.Query().Get("max_age")
		offset := (page - 1) * pageSize

		q := query.Use(db)
		userQuery := q.User.WithContext(r.Context())
		if name != "" {
			userQuery = userQuery.Where(q.User.Name.Like("%" + name + "%"))
		}

		if minAge != "" {
			userQuery = userQuery.Where(q.User.Age.Gte(parseAge(minAge)))
		}
		if maxAge != "" {
			userQuery = userQuery.Where(q.User.Age.Lte(parseAge(maxAge)))
		}

		users, total, err := userQuery.
			Order(q.User.ID.Desc()).
			FindByPage(offset, pageSize)
		if err != nil {
			writeJSON(w, http.StatusInternalServerError, errorResponse{Error: err.Error()})
			return
		}

		writeJSON(w, http.StatusOK, usersListResponse{
			Data:     users,
			Page:     page,
			PageSize: pageSize,
			Total:    total,
			Filters: usersListFilters{
				Name:   name,
				MinAge: parseAge(minAge),
				MaxAge: parseAge(maxAge),
			},
			HasMore: int64(offset+pageSize) < total,
		})
	}
}

// NewCreateUser godoc
// @Summary Create user
// @Description Creates a new user with the provided name and age.
// @Tags users
// @Accept json
// @Produce json
// @Param user body userCreateRequest true "User to create"
// @Success 200 {object} model.User
// @Failure 400 {object} errorResponse
// @Failure 500 {object} errorResponse
// @Router /api/users [post]
func NewCreateUser(db *gorm.DB) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		q := query.Use(db)

		var req userCreateRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			writeJSON(w, http.StatusBadRequest, errorResponse{Error: "invalid request body"})
			return
		}

		user := &model.User{
			Name: req.Name,
			Age:  req.Age,
		}
		if err := q.User.Create(user); err != nil {
			writeJSON(w, http.StatusInternalServerError, errorResponse{Error: err.Error()})
			return
		}
		writeJSON(w, http.StatusOK, user)
	}
}

// NewBatchCreateUsers godoc
// @Summary Batch create users
// @Description Starts a background process to create 50 mock users.
// @Tags users
// @Produce json
// @Success 200 {object} map[string]string
// @Router /api/users/batch_create [post]
func NewBatchCreateUsers(db *gorm.DB) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		go batchMockCreateUsers(db)
		writeJSON(w, http.StatusOK, map[string]string{"message": "Batch user creation started"})
	}
}

func parsePositiveInt(value string, fallback int) (int, error) {
	if value == "" {
		return fallback, nil
	}

	parsed, err := strconv.Atoi(value)
	if err != nil || parsed <= 0 {
		return 0, err
	}

	return parsed, nil
}

func writeJSON(w http.ResponseWriter, statusCode int, payload interface{}) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(statusCode)
	_ = json.NewEncoder(w).Encode(payload)
}

func parseAge(value string) int32 {
	if value == "" {
		return 0
	}

	parsed, err := strconv.Atoi(value)
	if err != nil || parsed < 0 {
		return 0
	}

	return int32(parsed)
}

func batchMockCreateUsers(db *gorm.DB) {
	q := query.Use(db)
	var workgroup sync.WaitGroup
	for i := 1; i <= 50; i++ {
		workgroup.Add(1)
		go func(i int) {
			user := &model.User{
				Name: "User " + strconv.Itoa(i),
				Age:  int32(20 + i%30),
			}
			if err := q.User.Create(user); err != nil {
				panic(err)
			}
		}(i)

	}

	workgroup.Wait()
	fmt.Println("Mock users created")
}
