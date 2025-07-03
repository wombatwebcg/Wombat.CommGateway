import request from '@/utils/request'

export interface Rule {
  id: number
  name: string
  description: string
  condition?: string
  conditions?: RuleCondition[]
  action: string
  priority: number
  enable: boolean
  status: string
  type: string
  deviceId?: number
  pointId?: number
  createTime?: string
  updateTime?: string
}

export interface RuleQuery {
  page: number
  pageSize: number
  name?: string
  status?: string
  triggerType?: string
  actionType?: string
}

export interface RuleResponse {
  items: Rule[]
  total: number
}

// 获取规则列表
export function getRules(params: RuleQuery) {
  return request<RuleResponse>({
    url: '/api/Rule',
    method: 'get',
    params
  })
}

// 创建规则
export function createRule(data: Omit<Rule, 'id' | 'createTime' | 'updateTime'>) {
  return request({
    url: '/api/Rule',
    method: 'post',
    data
  })
}

// 更新规则
export function updateRule(data: Rule) {
  return request({
    url: `/api/Rule/${data.id}`,
    method: 'put',
    data
  })
}

// 删除规则
export function deleteRule(id: number) {
  return request({
    url: `/api/Rule/${id}`,
    method: 'delete'
  })
}

// 更新规则状态
export function updateRuleStatus(id: number, enable: boolean) {
  return request({
    url: `/api/Rule/${id}/status`,
    method: 'put',
    data: {
      status: enable ? 1 : 0
    }
  })
}

// 测试规则
export function testRule(id: number, testData: {
  triggerValue: string,
  pointId: number,
  deviceId: number,
  timestamp?: string
}) {
  return request({
    url: `/api/Rule/${id}/test`,
    method: 'post',
    data: {
      ...testData,
      timestamp: testData.timestamp || new Date().toISOString()
    }
  })
}

// 获取规则详情
export function getRuleById(id: number) {
  return request<Rule>({
    url: `/api/Rule/${id}`,
    method: 'get'
  })
}

// 获取启用的规则
export function getEnabledRules() {
  return request<Rule[]>({
    url: '/api/Rule/enabled',
    method: 'get'
  })
}

// 添加规则动作和条件接口
export interface RuleAction {
  id?: number
  ruleId?: number
  name?: string
  type: string
  config: string
}

export interface RuleCondition {
  id?: number
  ruleId?: number
  operator?: string
  value?: string
} 