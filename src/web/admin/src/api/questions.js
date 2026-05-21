import request from './request'

const tenantCfg = (tenantId) =>
  tenantId ? { headers: { 'X-Tenant-ID': tenantId } } : {}

export const questionsApi = {
  getList: (params) => request.get('/questions', { params }),
  getById: (id) => request.get(`/questions/${id}`),
  create: (data, tenantId) => request.post('/questions', data, tenantCfg(tenantId)),
  update: (id, data, tenantId) => request.put(`/questions/${id}`, data, tenantCfg(tenantId)),
  delete: (id) => request.delete(`/questions/${id}`),
  aiGenerate: (data) => request.post('/questions/ai-generate', data),
  aiPreview: (data) => request.post('/questions/ai-preview', data),
  batchCreate: (data) => request.post('/questions/batch', data)
}
