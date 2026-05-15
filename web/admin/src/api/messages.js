import request from './request'

export const messagesApi = {
  getInbox: (params) => request.get('/messages/inbox', { params }),
  getSent: (params) => request.get('/messages/sent', { params }),
  send: (data) => request.post('/messages', data),
  markRead: (id) => request.patch(`/messages/${id}/read`),
  getTeachers: (params) => request.get('/messages/teachers', { params }),
}
