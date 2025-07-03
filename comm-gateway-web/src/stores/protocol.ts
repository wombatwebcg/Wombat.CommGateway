import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ProtocolConfig, CreateProtocolConfigRequest, UpdateProtocolConfigRequest } from '@/api/protocol'
import {
  getProtocolConfigList,
  getProtocolConfigById,
  createProtocolConfig,
  updateProtocolConfig,
  deleteProtocolConfig,
  updateProtocolConfigStatus
} from '@/api/protocol'

export const useProtocolStore = defineStore('protocol', () => {
  const protocolList = ref<ProtocolConfig[]>([])
  const currentProtocol = ref<ProtocolConfig | null>(null)
  const loading = ref(false)

  const fetchProtocolList = async () => {
    try {
      loading.value = true
      const data = await getProtocolConfigList()
      protocolList.value = data
    } finally {
      loading.value = false
    }
  }

  const fetchProtocolById = async (id: number) => {
    try {
      loading.value = true
      const data = await getProtocolConfigById(id)
      currentProtocol.value = data
    } finally {
      loading.value = false
    }
  }

  const createProtocol = async (request: CreateProtocolConfigRequest) => {
    try {
      loading.value = true
      const data = await createProtocolConfig(request)
      protocolList.value.push(data)
      return data
    } finally {
      loading.value = false
    }
  }

  const updateProtocol = async (id: number, request: UpdateProtocolConfigRequest) => {
    try {
      loading.value = true
      const data = await updateProtocolConfig(id, request)
      const index = protocolList.value.findIndex(p => p.id === id)
      if (index !== -1) {
        protocolList.value[index] = data
      }
      if (currentProtocol.value?.id === id) {
        currentProtocol.value = data
      }
      return data
    } finally {
      loading.value = false
    }
  }

  const removeProtocol = async (id: number) => {
    try {
      loading.value = true
      await deleteProtocolConfig(id)
      protocolList.value = protocolList.value.filter(p => p.id !== id)
      if (currentProtocol.value?.id === id) {
        currentProtocol.value = null
      }
    } finally {
      loading.value = false
    }
  }

  const updateStatus = async (id: number, enabled: boolean) => {
    try {
      loading.value = true
      const data = await updateProtocolConfigStatus(id, enabled)
      const index = protocolList.value.findIndex(p => p.id === id)
      if (index !== -1) {
        protocolList.value[index] = data
      }
      if (currentProtocol.value?.id === id) {
        currentProtocol.value = data
      }
      return data
    } finally {
      loading.value = false
    }
  }

  return {
    protocolList,
    currentProtocol,
    loading,
    fetchProtocolList,
    fetchProtocolById,
    createProtocol,
    updateProtocol,
    removeProtocol,
    updateStatus
  }
}) 