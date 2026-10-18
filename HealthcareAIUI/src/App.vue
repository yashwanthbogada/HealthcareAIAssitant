<script setup>
import { ref } from 'vue'

const prompt = ref('Patient presents with acute chest pain and shortness of breath.')
const activeTab = ref('structured')
const loading = ref(false)
const result = ref(null)
const streamingResult = ref('')
const error = ref(null)

async function handleSubmit() {
  loading.value = true
  error.value = null
  result.value = null
  streamingResult.value = ''

  try {
    const endpoint = `http://localhost:5047/api/ClinicalAssistant/${activeTab.value}`

    if (activeTab.value === 'stream') {
      // Handle Streaming API Request
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt: prompt.value })
      })

      if (!response.ok) throw new Error(`Server returned status ${response.status}`)

      const reader = response.body.getReader()
      const decoder = new TextDecoder()

      while (true) {
        const { value, done } = await reader.read()
        if (done) break
        streamingResult.value += decoder.decode(value, { stream: true })
      }

    } else {
      // Handle Standard & Structured JSON Requests
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt: prompt.value })
      })

      if (!response.ok) throw new Error(`Server returned status ${response.status}`)
      
      result.value = await response.json()
    }
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="app-container">
    <header class="header">
      <h1>Healthcare AI Assistant Portal</h1>
      <span class="badge">.NET 8 + Ollama (phi4-mini)</span>
    </header>

    <main class="card">
      <div class="tabs">
        <button 
          :class="{ active: activeTab === 'structured' }" 
          @click="activeTab = 'structured'"
        >
          Structured JSON Schema
        </button>
        <button 
          :class="{ active: activeTab === 'query' }" 
          @click="activeTab = 'query'"
        >
          Standard Query
        </button>
        <button 
          :class="{ active: activeTab === 'stream' }" 
          @click="activeTab = 'stream'"
        >
          Real-Time Stream
        </button>
      </div>

      <div class="input-group">
        <label for="prompt">Clinical Scenario / Prompt</label>
        <textarea 
          id="prompt" 
          v-model="prompt" 
          rows="4" 
          placeholder="Enter patient notes or symptoms..."
        ></textarea>
      </div>

      <button class="submit-btn" @click="handleSubmit" :disabled="loading">
        {{ loading ? 'Processing via Local AI...' : 'Generate Analysis' }}
      </button>

      <div v-if="error" class="error-box">
        ⚠️ Error: {{ error }}
      </div>

      <!-- Output for Structured / Standard JSON -->
      <div v-if="result && activeTab !== 'stream'" class="output-container">
        <h3>Response Payload:</h3>
        <pre>{{ JSON.stringify(result, null, 2) }}</pre>
      </div>

      <!-- Output for Real-Time Streaming -->
      <div v-if="activeTab === 'stream' && (streamingResult || loading)" class="output-container">
        <h3>Live Streaming Output:</h3>
        <div class="stream-box">{{ streamingResult }}<span v-if="loading" class="cursor">_</span></div>
      </div>
    </main>
  </div>
</template>

<style scoped>
.app-container {
  max-width: 900px;
  margin: 40px auto;
  padding: 0 20px;
  font-family: system-ui, -apple-system, sans-serif;
  color: #1e293b;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.header h1 {
  font-size: 22px;
  color: #0f172a;
}

.badge {
  background: #e0f2fe;
  color: #0369a1;
  padding: 4px 10px;
  border-radius: 9999px;
  font-size: 12px;
  font-weight: 600;
}

.card {
  background: white;
  padding: 24px;
  border-radius: 12px;
  box-shadow: 0 10px 15px -3px rgb(0 0 0 / 0.1);
  border: 1px solid #e2e8f0;
}

.tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 20px;
}

.tabs button {
  flex: 1;
  padding: 10px;
  border: 1px solid #cbd5e1;
  background: #f8fafc;
  font-weight: 600;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.tabs button.active {
  background: #2563eb;
  color: white;
  border-color: #2563eb;
}

.input-group {
  margin-bottom: 20px;
}

label {
  display: block;
  font-weight: 600;
  font-size: 14px;
  margin-bottom: 6px;
}

textarea {
  width: 100%;
  padding: 12px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  font-size: 14px;
  box-sizing: border-box;
}

.submit-btn {
  width: 100%;
  background: #0f172a;
  color: white;
  border: none;
  padding: 12px;
  font-weight: 600;
  border-radius: 6px;
  cursor: pointer;
}

.submit-btn:hover {
  background: #1e293b;
}

.submit-btn:disabled {
  background: #94a3b8;
  cursor: not-allowed;
}

.error-box {
  margin-top: 20px;
  background: #fef2f2;
  color: #991b1b;
  padding: 12px;
  border-radius: 6px;
  font-size: 14px;
}

.output-container {
  margin-top: 20px;
}

.output-container h3 {
  font-size: 14px;
  margin-bottom: 8px;
  color: #475569;
}

pre {
  background: #0f172a;
  color: #38bdf8;
  padding: 16px;
  border-radius: 6px;
  overflow-x: auto;
  font-size: 13px;
}

.stream-box {
  background: #0f172a;
  color: #38bdf8;
  padding: 16px;
  border-radius: 6px;
  font-family: monospace;
  white-space: pre-wrap;
  word-break: break-word;
  font-size: 14px;
  line-height: 1.5;
}

.cursor {
  animation: blink 1s infinite;
  font-weight: bold;
}

@keyframes blink {
  0%, 100% { opacity: 1; }
  50% { opacity: 0; }
}
</style>