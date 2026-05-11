import request from './request'

export const tenantsApi = {
  getList: (params) => request.get('/tenants', { params }),
  getById: (id) => request.get(`/tenants/${id}`),
  create: (data) => request.post('/tenants', data),
  update: (id, data) => request.put(`/tenants/${id}`, data),
  toggleStatus: (id) => request.patch(`/tenants/${id}/status`)
}
