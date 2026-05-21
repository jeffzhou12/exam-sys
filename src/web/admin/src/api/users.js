import request from './request'

export const usersApi = {
  getList: (params) => request.get('/users', { params }),
  getById: (id) => request.get(`/users/${id}`),
  create: (data) => request.post('/users', data),
  update: (id, data) => request.put(`/users/${id}`, data),
  toggleStatus: (id) => request.patch(`/users/${id}/status`),
  resetPassword: (id, data) => request.patch(`/users/${id}/password`, data)
}
