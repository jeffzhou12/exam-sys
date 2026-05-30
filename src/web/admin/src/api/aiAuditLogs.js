import request from './request'

export const aiAuditLogsApi = {
  getList: (params) => request.get('/ai-audit-logs', { params })
}
