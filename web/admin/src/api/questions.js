import request from './request'

export const questionsApi = {
  getList: (params) => request.get('/questions', { params }),
  getById: (id) => request.get(`/questions/${id}`),
  create: (data) => request.post('/questions', data),
  update: (id, data) => request.put(`/questions/${id}`, data),
  delete: (id) => request.delete(`/questions/${id}`),
  aiGenerate: (data) => request.post('/questions/ai-generate', data),
  aiPreview: (data) => request.post('/questions/ai-preview', data),
  batchCreate: (data) => request.post('/questions/batch', data)
}
