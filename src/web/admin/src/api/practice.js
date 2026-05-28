import request from './request'

export const practiceApi = {
  /** 管理端：分页查询练习记录 */
  getAdminSessions: (params) => request.get('/admin/practice/sessions', { params }),

  /** 管理端：分页查询错题本 */
  getAdminWrongBook: (params) => request.get('/admin/wrong-book', { params }),
}
