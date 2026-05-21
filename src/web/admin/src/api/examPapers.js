import request from './request'

const tenantCfg = (tenantId) =>
  tenantId ? { headers: { 'X-Tenant-ID': tenantId } } : {}

export const examPapersApi = {
  getList: (params) => request.get('/exam-papers', { params }),
  getById: (id) => request.get(`/exam-papers/${id}`),
  create: (data, tenantId) => request.post('/exam-papers', data, tenantCfg(tenantId)),
  update: (id, data, tenantId) => request.put(`/exam-papers/${id}`, data, tenantCfg(tenantId)),
  publish: (id) => request.post(`/exam-papers/${id}/publish`),
  cancel: (id) => request.post(`/exam-papers/${id}/cancel`),
  getResults: (id, params) => request.get(`/exam-papers/${id}/results`, { params }),
  getStudentResult: (examPaperId, studentId) =>
    request.get(`/exam-papers/${examPaperId}/answers/${studentId}`),
  manualGrade: (examPaperId, answerId, data) =>
    request.patch(`/exam-papers/${examPaperId}/answers/items/${answerId}/grade`, data)
}
