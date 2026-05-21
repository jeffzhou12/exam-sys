import request from './request'

export const tenantsApi = {
  getList: (params) => request.get('/tenants', { params }),
}
