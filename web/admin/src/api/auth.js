import request from './request'

export const authApi = {
  login: (data) => request.post('/auth/login', data),
  register: (data) => request.post('/auth/register', data),
  forgotPassword: (data) => request.post('/auth/forgot-password', data),
  resetPassword: (data) => request.post('/auth/reset-password', data)
}
