import type { CreateTagDto, TagDetailDto, UpdateTagDto } from '@/api'

/** 键的格式与后端 CreateTagDtoValidator 一致：小写字母开头，其后只允许小写字母、数字、- 和 _ */
const TAG_KEY_PATTERN = /^[a-z][a-z0-9_-]*$/

interface TagFormState {
  key: string
  name: string
}

function emptyTagForm(): TagFormState {
  return { key: '', name: '' }
}

function tagFormFromDto(tag: TagDetailDto | null | undefined): TagFormState {
  return { key: tag?.key ?? '', name: tag?.name ?? '' }
}

/** 键是否重复由后端裁决；这里只做请求级的基础校验 */
function validateTagForm(state: TagFormState): Record<string, string> {
  const errors: Record<string, string> = {}
  const key = state.key.trim()
  const name = state.name.trim()
  if (!key) errors.key = '请填写键。'
  else if (!TAG_KEY_PATTERN.test(key)) errors.key = '键只能由小写字母、数字、- 和 _ 组成，且以字母开头。'
  else if (key.length > 64) errors.key = '键不能超过 64 个字符。'
  if (!name) errors.name = '请填写名称。'
  else if (name.length > 128) errors.name = '名称不能超过 128 个字符。'
  return errors
}

function tagFormToCreateBody(state: TagFormState): CreateTagDto {
  return { key: state.key.trim(), name: state.name.trim() }
}

/** 键创建后不可变，更新请求体只有名称 */
function tagFormToUpdateBody(state: TagFormState): UpdateTagDto {
  return { name: state.name.trim() }
}

export {
  TAG_KEY_PATTERN,
  emptyTagForm,
  tagFormToCreateBody,
  tagFormToUpdateBody,
  tagFormFromDto,
  validateTagForm,
  type TagFormState,
}
