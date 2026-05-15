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
}
