import request from './request'

export const AI_SCENES = [
  { value: 0, label: '默认（通用）' },
  { value: 1, label: '生成题目' },
  { value: 2, label: '批改答案' },
  { value: 3, label: '解释题目' },
  { value: 4, label: '分析图书' },
  { value: 5, label: '向量嵌入' }
]

export const aiConfigsApi = {
  getList: (params) => request.get('/ai-configs', { params }),
  getById: (id) => request.get(`/ai-configs/${id}`),
  create: (data) => request.post('/ai-configs', data),
  update: (id, data) => request.put(`/ai-configs/${id}`, data),
  remove: (id) => request.delete(`/ai-configs/${id}`),
  resetQuota: (id) => request.post(`/ai-configs/${id}/reset-quota`)
}
