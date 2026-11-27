import type { TagDto } from '@/api'

/**
 * 模型编辑框回填用：标签删除不清理模型上的引用，只保留当前标签列表里能匹配到的 Id。
 * 匹配不到的是已删标签，不带进表单，保存时自然不会提交、也不会被后端「标签不存在」拦下。
 */
function existingTagIds(tagIds: ReadonlyArray<string> | null | undefined, tags: ReadonlyArray<TagDto>): string[] {
  const existing = new Set(tags.map(tag => tag.id))
  return (tagIds ?? []).filter(id => existing.has(id))
}

export { existingTagIds }
