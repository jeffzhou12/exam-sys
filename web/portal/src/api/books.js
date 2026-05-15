import request from './request'

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api'

export const booksApi = {
  getBooks: (params) => request.get('/books', { params, withTenant: true }),
  getBook: (id) => request.get(`/books/${id}`, { withTenant: true }),

  // Load PDF as blob (with auth) then return an object URL
  async getPdfObjectUrl(id) {
    const token = localStorage.getItem('portal-token')
    const tenantId = localStorage.getItem('portal-tenantId')
    const headers = {}
    if (token) headers['Authorization'] = `Bearer ${token}`
    if (tenantId) headers['X-Tenant-ID'] = tenantId

    const res = await fetch(`${baseURL}/books/${id}/pdf`, { headers })
    if (!res.ok) throw new Error('PDF 加载失败')
    const blob = await res.blob()
    return URL.createObjectURL(blob)
  },

  getAnnotations: (bookId) =>
    request.get(`/books/${bookId}/annotations`, { withTenant: true }),
  createAnnotation: (bookId, data) =>
    request.post(`/books/${bookId}/annotations`, data, { withTenant: true }),
  updateAnnotation: (bookId, annId, data) =>
    request.put(`/books/${bookId}/annotations/${annId}`, data, { withTenant: true }),
  deleteAnnotation: (bookId, annId) =>
    request.delete(`/books/${bookId}/annotations/${annId}`, { withTenant: true }),
  aiAnalyze: (bookId, data) =>
    request.post(`/books/${bookId}/ai-analyze`, data, { withTenant: true }),
}
