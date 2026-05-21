import request from './request'

export const teacherApi = {
  // 获取所属租户下的所有考试（需租户上下文）
  getExams: (params) => request.get('/exam-papers', { params, withTenant: true }),

  // 获取某场考试的所有考生答卷列表
  getResults: (examPaperId, params) =>
    request.get(`/exam-papers/${examPaperId}/results`, { params, withTenant: true }),

  // 获取单个考生的答题详情
  getStudentResult: (examPaperId, studentId) =>
    request.get(`/exam-papers/${examPaperId}/answers/${studentId}`, { withTenant: true }),

  // 手动批改某道题
  manualGrade: (examPaperId, answerId, data) =>
    request.patch(`/exam-papers/${examPaperId}/answers/items/${answerId}/grade`, data, {
      withTenant: true,
    }),
}
