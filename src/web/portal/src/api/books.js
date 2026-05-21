import request from './request'

export const booksApi = {
  getBooks: (params) => request.get('/books', { params }),
  getBook:  (id) => request.get(`/books/${id}`),

  /**
   * 返回 PDF.js getDocument() 所需的配置对象（URL + 鉴权头）。
   * PDF.js 会自动通过 HTTP Range 请求分片拉取内容，无需提前下载整个文件。
   */
  getPdfConfig(id) {
    const apiBase   = import.meta.env.VITE_API_BASE_URL || '/api'
    const token     = localStorage.getItem('exam-token')
    const tenantId  = localStorage.getItem('exam-activeTenantId')
    const httpHeaders = {}
    if (token)    httpHeaders.Authorization  = `Bearer ${token}`
    if (tenantId) httpHeaders['X-Tenant-ID'] = tenantId
    return { url: `${apiBase}/books/${id}/pdf`, httpHeaders }
  },

  getAnnotations:    (bookId)           => request.get(`/books/${bookId}/annotations`),
  createAnnotation:  (bookId, data)     => request.post(`/books/${bookId}/annotations`, data),
  updateAnnotation:  (bookId, annId, data) => request.put(`/books/${bookId}/annotations/${annId}`, data),
  deleteAnnotation:  (bookId, annId)    => request.delete(`/books/${bookId}/annotations/${annId}`),
  aiAnalyze:         (bookId, data)     => request.post(`/books/${bookId}/ai-analyze`, data),
}
