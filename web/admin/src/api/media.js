import request from './request'

export const mediaApi = {
  uploadImage: (file) => {
    const fd = new FormData()
    fd.append('file', file)
    return request.post('/media/image', fd, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  }
}
