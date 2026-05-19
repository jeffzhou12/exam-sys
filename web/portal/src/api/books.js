import request from './request'

export const booksApi = {
  getBooks: (params) => request.get('/books', { params }),
  getBook: (id) => request.get(`/books/${id}`),

  // 通过 axios 以 blob 方式加载 PDF，token/tenantId 由 request 拦截器自动注入
  async getPdfObjectUrl(id) {
    const res = await request.get(`/books/${id}/pdf`, { responseType: 'blob' })
    return URL.createObjectURL(res)
  },

  getAnnotations: (bookId) =>
    request.get(`/books/${bookId}/annotations`),
  createAnnotation: (bookId, data) =>
    request.post(`/books/${bookId}/annotations`, data),
  updateAnnotation: (bookId, annId, data) =>
    request.put(`/books/${bookId}/annotations/${annId}`, data),
  deleteAnnotation: (bookId, annId) =>
    request.delete(`/books/${bookId}/annotations/${annId}`),
  aiAnalyze: (bookId, data) =>
    request.post(`/books/${bookId}/ai-analyze`, data),
}
