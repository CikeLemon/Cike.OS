import { describe, expect, it } from 'vitest'
import type { TagDto } from '@/api'
import { existingTagIds } from './model-form'

const tags: TagDto[] = [{ id: '1', key: 'vision', name: '视觉' }, { id: '2', key: 'chat', name: '对话' }]

describe('existingTagIds', () => {
  it('只保留当前标签列表里能匹配到的 Id，顺序不变', () => {
    expect(existingTagIds(['2', '999', '1'], tags)).toEqual(['2', '1'])
  })

  it('全部匹配不到 / 空输入 / null', () => {
    expect(existingTagIds(['999'], tags)).toEqual([])
    expect(existingTagIds([], tags)).toEqual([])
    expect(existingTagIds(null, tags)).toEqual([])
    expect(existingTagIds(undefined, tags)).toEqual([])
    expect(existingTagIds(['1'], [])).toEqual([])
  })
})
