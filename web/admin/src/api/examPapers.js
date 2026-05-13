import request from './request'

export const examPapersApi = {
  getList: (params) => request.get('/exam-papers', { params }),
  getById: (id) => request.get(`/exam-papers/${id}`),
  create: (data) => request.post('/exam-papers', data),
  update: (id, data) => request.put(`/exam-papers/${id}`, data),
  publish: (id) => request.post(`/exam-papers/${id}/publish`),
  cancel: (id) => request.post(`/exam-papers/${id}/cancel`),
  getResults: (id, params) => request.get(`/exam-papers/${id}/results`, { params }),
  getStudentResult: (examPaperId, studentId) =>
    request.get(`/exam-papers/${examPaperId}/answers/${studentId}`),
  manualGrade: (examPaperId, answerId, data) =>
    request.patch(`/exam-papers/${examPaperId}/answers/items/${answerId}/grade`, data)
}
