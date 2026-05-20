import request from './request'

export const questionsApi = {
  getById: (id) => request.get(`/questions/${id}`, { withTenant: true }),
}
