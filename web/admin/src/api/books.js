import request from './request'

// 当超级管理员在全租户模式下操作特定图书时，通过此函数生成 X-Tenant-ID 请求头覆盖
const tenantCfg = (tenantId) =>
  tenantId ? { headers: { 'X-Tenant-ID': tenantId } } : {}

export const booksApi = {
  getList: (params) => request.get('/books', { params }),
  getById: (id) => request.get(`/books/${id}`),
  create: (data, tenantId) => request.post('/books', data, tenantCfg(tenantId)),
  update: (id, data, tenantId) => request.put(`/books/${id}`, data, tenantCfg(tenantId)),
  uploadPdf: (id, formData, onProgress, tenantId) =>
    request.post(`/books/${id}/pdf`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
        ...(tenantId ? { 'X-Tenant-ID': tenantId } : {})
      },
      timeout: 300000,
      onUploadProgress: onProgress
    }),
  delete: (id, tenantId) => request.delete(`/books/${id}`, tenantCfg(tenantId)),
  getPdfBlob: (id, tenantId) =>
    request.get(`/books/${id}/pdf`, { responseType: 'blob', ...tenantCfg(tenantId) })
}
