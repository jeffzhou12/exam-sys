import request from './request'

export const messagesApi = {
  getInbox: (params) =>
    request.get('/messages/inbox', { params, withTenant: true }),

  getSent: (params) =>
    request.get('/messages/sent', { params, withTenant: true }),

  send: (data) =>
    request.post('/messages', data, { withTenant: true }),

  markRead: (id) =>
    request.patch(`/messages/${id}/read`, {}, { withTenant: true }),

  getTeachers: () =>
    request.get('/messages/teachers', { withTenant: true }),

  // 获取对话线程（根消息 + 全部回复，按时间升序）
  getThread: (id) =>
    request.get(`/messages/${id}/thread`, { withTenant: true }),

  // 获取消息关联的题目（学生有权限）
  getMessageQuestions: (id) =>
    request.get(`/messages/${id}/questions`, { withTenant: true }),
}
