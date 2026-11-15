<template>
  <aside class="history-sidebar">
    <div class="sidebar-header">
      <h3>Clinical History</h3>
      <button @click="fetchHistory" class="refresh-btn" title="Refresh History">🔄</button>
    </div>

    <div v-if="loading" class="loading-text">Loading logs...</div>
    
    <div v-else-if="logs.length === 0" class="empty-text">
      No past queries found.
    </div>

    <ul v-else class="history-list">
      <li 
        v-for="log in logs" 
        :key="log.id" 
        @click="$emit('select-log', log)"
        class="history-item"
      >
        <div class="log-meta">
          <span :class="['badge-tag', log.endpointType]">{{ log.endpointType }}</span>
          <span class="timestamp">{{ formatDate(log.timestamp) }}</span>
        </div>
        <p class="log-prompt">{{ log.prompt }}</p>
      </li>
    </ul>
  </aside>
</template>

<script setup>
import { ref, onMounted } from 'vue'

const logs = ref([])
const loading = ref(false)

const emit = defineEmits(['select-log'])

const fetchHistory = async () => {
  loading.value = true
  try {
    const response = await fetch('http://localhost:5047/api/ClinicalAssistant/history')
    if (response.ok) {
      logs.value = await response.json()
    }
  } catch (error) {
    console.error('Failed to fetch clinical history:', error)
  } finally {
    loading.value = false
  }
}

const formatDate = (dateString) => {
  const date = new Date(dateString)
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + ' ' + date.toLocaleDateString()
}

onMounted(() => {
  fetchHistory()
})

defineExpose({ fetchHistory })
</script>

<style scoped>
.history-sidebar {
  width: 300px;
  background: #f8fafc;
  border-right: 1px solid #cbd5e1;
  display: flex;
  flex-direction: column;
  height: 100vh;
  padding: 16px;
  box-sizing: border-box;
}

.sidebar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.sidebar-header h3 {
  font-size: 16px;
  color: #0f172a;
  margin: 0;
}

.refresh-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 16px;
}

.history-list {
  list-style: none;
  padding: 0;
  margin: 0;
  overflow-y: auto;
  flex-grow: 1;
}

.history-item {
  background: white;
  padding: 10px;
  border-radius: 6px;
  margin-bottom: 8px;
  cursor: pointer;
  border: 1px solid #e2e8f0;
  transition: all 0.2s ease;
}

.history-item:hover {
  border-color: #2563eb;
  box-shadow: 0 2px 4px rgba(0,0,0,0.05);
}

.log-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.badge-tag {
  font-size: 10px;
  padding: 2px 6px;
  border-radius: 4px;
  text-transform: uppercase;
  font-weight: bold;
}

.badge-tag.query { background: #e0f2fe; color: #0369a1; }
.badge-tag.structured { background: #d1e7dd; color: #0f5132; }
.badge-tag.stream { background: #fff3cd; color: #664d03; }
.badge-tag.chat { background: #ede9fe; color: #6d28d9; }

.timestamp {
  font-size: 11px;
  color: #64748b;
}

.log-prompt {
  font-size: 13px;
  color: #334155;
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.loading-text, .empty-text {
  font-size: 13px;
  color: #64748b;
  text-align: center;
  margin-top: 2rem;
}
</style>