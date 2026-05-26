import request from './request'

export const authApi = {
  getCaptchaConfig: () => request.get('/auth/captcha-config'),
  /** 获取滑动验证码题目（背景图 + 拼图块） */
  getCaptcha: () => request.get('/auth/captcha'),
  /** 提交拼图位置，验证通过后返回单次 token */
  verifyCaptcha: (data) => request.post('/auth/captcha/verify', data),
  login: (data) => request.post('/auth/login', data),
  register: (data) => request.post('/auth/register', data),
  getPublicTenants: () => request.get('/auth/tenants'),
  /** 发送手机/邮箱验证码 */
  sendCode: (data) => request.post('/auth/send-code', data),
  /** 验证码登录（首次自动注册） */
  loginWithCode: (data) => request.post('/auth/login-code', data),
}
