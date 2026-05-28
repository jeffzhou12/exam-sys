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

  /** 获取题目参考答案和解析 */
  getAnswer: (questionId) =>
    request.get(`/practice/questions/${questionId}/answer`, { withTenant: true }),

  /** 保存练习会话记录到服务端 */
  saveSession: (data) =>
    request.post('/practice/sessions', data, { withTenant: true }),

  /** 获取当前用户的服务端练习历史记录 */
  getHistory: () =>
    request.get('/practice/sessions', { withTenant: true }),

  /** 同步添加/更新错题本条目（server-side upsert） */
  saveWrongBookItem: (questionId, answerGiven = '') =>
    request.post('/practice/wrong-book', { questionId, answerGiven }, { withTenant: true }),

  /** AI 智能分析本次练习成绩 */
  analyzeSession: (data) =>
    request.post('/practice/analyze', data, { withTenant: true }),
}
