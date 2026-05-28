import request from './request'

// 收藏类型常量
export const FavoriteType = {
  QUESTION: 1,
  EXAM: 2,
  BOOK: 3,
}

export const favoritesApi = {
  /** 切换收藏（返回 { isFavorited: bool }） */
  toggle: (targetType, targetId) =>
    request.post('/favorites/toggle', { targetType, targetId }, { withTenant: true }),

  /** 检查是否已收藏（返回 { isFavorited: bool }） */
  check: (targetType, targetId) =>
    request.get('/favorites/check', { params: { targetType, targetId }, withTenant: true }),

  /** 获取收藏列表 */
  getList: (targetType, page = 1, pageSize = 20) =>
    request.get('/favorites', { params: { targetType, page, pageSize }, withTenant: true }),
}
