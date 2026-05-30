import request from './request'

export const SMS_SCENES = [
  { value: 'login', label: '登录验证码' },
  { value: 'register', label: '注册验证码' },
  { value: 'reset-password', label: '重置密码' }
]

export const smsTemplatesApi = {
  getList: (params) => request.get('/sms-templates', { params }),
  getById: (id) => request.get(`/sms-templates/${id}`),
  create: (data) => request.post('/sms-templates', data),
  update: (id, data) => request.put(`/sms-templates/${id}`, data),
  remove: (id) => request.delete(`/sms-templates/${id}`)
}
