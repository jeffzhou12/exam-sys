import request from './request'

export const meApi = {
  /** 获取个人资料 */
  getProfile: () => request.get('/profile', { withTenant: true }),

  /** 更新个人资料 */
  updateProfile: (data) => request.patch('/profile', data, { withTenant: true }),

  /** 上传头像（FormData，字段名 file） */
  uploadAvatar: (formData) => request.post('/profile/avatar', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    withTenant: true,
  }),

  /** 发送换绑手机验证码（target: 新手机号） */
  sendChangePhoneCode: (target) => request.post('/auth/send-code', { target, purpose: 'change_phone' }),

  /** 换绑手机 */
  changePhone: (data) => request.post('/profile/change-phone', data, { withTenant: true }),

  /** 修改密码 */
  changePassword: (data) => request.post('/profile/change-password', data, { withTenant: true }),
}

// 预设学历选项
export const EDUCATION_LEVELS = [
  '小学', '初中', '高中', '大学', '研究生', '博士',
]

// 预设学科列表
export const SUBJECT_OPTIONS = [
  '数学', '语文', '英语', '物理', '化学', '生物',
  '历史', '地理', '政治', '计算机', '人工智能',
  '音乐', '美术', '体育', '其他',
]
