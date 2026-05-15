import request from './request'

export const practiceApi = {
  /** 随机抽取练习题目（不含答案） */
  getQuestions: (params) =>
    request.get('/practice/questions', { params, withTenant: true }),

  /** 提交练习答案，获取批改结果 */
  submit: (answers) =>
    request.post('/practice/submit', { answers }, { withTenant: true }),

  /** 获取相似题目 */
  getSimilar: (questionId, knowledgePoint, difficulty, count = 5) =>
    request.get(`/practice/questions/${questionId}/similar`, {
      params: { knowledgePoint, difficulty, count },
      withTenant: true,
    }),

  /** AI 详解题目 */
  explain: (questionId) =>
    request.post(`/practice/questions/${questionId}/explain`, {}, { withTenant: true }),
}
