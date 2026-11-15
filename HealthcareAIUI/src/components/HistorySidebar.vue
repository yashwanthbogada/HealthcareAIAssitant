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
          <span :class="['badge', log.endpointType]">{{ log.endpointType }}</span>
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

// Expose refresh so parent can trigger it after a new query
defineExpose({ fetchHistory })
</script>

<style scoped>
.history-sidebar {
  width: 280px;
  background: #f8f9fa;
  border-right: 1px solid #dee2e6;
  display: flex;
  flex-direction: column;
  height: 100vh;
  padding: 1rem;
  box-sizing: border-box;
}

.sidebar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.sidebar-header h3 {
  font-size: 1.1rem;
  color: #333;
  margin: 0;
}

.refresh-btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 1rem;
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
  border: 1px solid #e9ecef;
  transition: all 0.2s ease;
}

.history-item:hover {
  border-color: #0d6efd;
  box-shadow: 0 2px 4px rgba(0,0,0,0.05);
}

.log-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.badge {
  font-size: 0.7rem;
  padding: 2px 6px;
  border-radius: 4px;
  text-transform: uppercase;
  font-weight: bold;
}

.badge.query { background: #cfe2ff; color: #084298; }
.badge.structured { background: #d1e7dd; color: #0f5132; }
.badge.stream { background: #fff3cd; color: #664d03; }

.timestamp {
  font-size: 0.75rem;
  color: #6c757d;
}

.log-prompt {
  font-size: 0.85rem;
  color: #495057;
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.loading-text, .empty-text {
  font-size: 0.85rem;
  color: #6c757d;
  text-align: center;
  margin-top: 2rem;
}
</style>