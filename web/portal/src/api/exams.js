import request from './request'

export const examsApi = {
  // 不需租户：公开浏览考试列表和详情
  getList: (params) => request.get('/exam-papers', { params }),
  getById: (id) => request.get(`/exam-papers/${id}`),

  // 需租户：参加考试、查询自身成绩（学生属于租户，接口需租户上下文）
  submit: (examPaperId, data) =>
    request.post(`/exam-papers/${examPaperId}/answers`, data, { withTenant: true }),
  getMyResult: (examPaperId, studentId) =>
    request.get(`/exam-papers/${examPaperId}/answers/${studentId}`, { withTenant: true }),
  getMyExams: () => request.get('/student/my-results', { withTenant: true }),
}
