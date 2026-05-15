import request from './request'

export const auditLogsApi = {
  getList: (params) => request.get('/audit-logs', { params }),
}
