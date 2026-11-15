<script setup>
import { ref, nextTick } from 'vue'
import { marked } from 'marked'
import HistorySidebar from './components/HistorySidebar.vue'

const activeTab = ref('chat') // Default to our new chat thread mode
const prompt = ref('')
const loading = ref(false)
const result = ref(null)
const streamingResult = ref('')
const error = ref(null)
const sidebarRef = ref(null)

// Chat Thread State for multi-turn conversations
const chatMessages = ref([
  { role: 'assistant', content: 'Hello! I am your clinical operations assistant. How can I help you today?' }
])
const chatContainerRef = ref(null)
const renderMarkdown = (content) => {
  if (!content) return ''
  return marked.parse(content)
}
const handleSelectLog = (log) => {
  prompt.value = log.prompt
  if (log.endpointType === 'chat') {
    activeTab.value = 'chat'
    // Load log content into chat view
    chatMessages.value = [
      { role: 'user', content: log.prompt },
      { role: 'assistant', content: log.responseContent }
    ]
  } else {
    activeTab.value = log.endpointType || 'query'
    try {
      result.value = JSON.parse(log.responseContent)
    } catch {
      result.value = { response: log.responseContent }
    }
  }
}

const loadThreads = async () => {
  const response = await fetch('http://localhost:5047/api/ClinicalAssistant/threads');
  if (response.ok) {
    sidebarThreads.value = await response.json();
  }
};
const selectThread = async (threadId) => {
  currentThreadId.value = threadId;
  const response = await fetch(`http://localhost:5047/api/ClinicalAssistant/history/${threadId}`);
  if (response.ok) {
    const history = await response.json();
    // Map history logs into your chat messages array
    messages.value = history.map(h => ({
      role: h.IsUser ? 'user' : 'assistant',
      content: h.Query // or Response depending on your model schema
    }));
  }
};

const startNewThread = () => {
  currentThreadId.value = crypto.randomUUID(); // Generate new session ID
  messages.value = [{ role: 'assistant', content: 'Hello! I am your clinical operations assistant. How can I help you today?' }];
};
function clearChat() {
  chatMessages.value = [
    { role: 'assistant', content: 'Hello! I am your clinical operations assistant. How can I help you today?' }
  ]
}
async function handleChatSubmit() {
  if (!prompt.value.trim() || loading.value) return

  const userMsg = prompt.value
  prompt.value = ''
  error.value = null

  // Append user message
  chatMessages.value.push({ role: 'user', content: userMsg })
  
  // Append placeholder assistant message
  const assistantMsgIndex = chatMessages.value.length
  chatMessages.value.push({ role: 'assistant', content: '' })
  
  await scrollToBottom()
  loading.value = true

  try {
   // Keep only the most recent messages to protect the model's context limit
    const maxHistoryMessages = 8;
    const trimmedMessages = chatMessages.value.slice(-maxHistoryMessages);

    const response = await fetch('http://localhost:5047/api/ClinicalAssistant/chat-stream', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        threadId: currentThreadId.value,
        messages: trimmedMessages.map(m => ({ role: m.role, content: m.content }))
      })
    });

    if (!response.ok) throw new Error(`Server returned status ${response.status}`)
    
    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    
    while (true) {
      const { value, done } = await reader.read()
      if (done) break
      
      const chunk = decoder.decode(value, { stream: true })
      chatMessages.value[assistantMsgIndex].content += chunk
      await scrollToBottom()
    }
    
    if (sidebarRef.value) {
      sidebarRef.value.fetchHistory()
    }
  } catch (err) {
    error.value = err.message
    // Remove the empty placeholder bubble if the request failed
    chatMessages.value.splice(assistantMsgIndex, 1)
  } finally {
    loading.value = false
    await scrollToBottom()
  }
}

async function handleSubmit() {
  if (activeTab.value === 'chat') {
    return handleChatSubmit()
  }

  loading.value = true
  error.value = null
  result.value = null
  streamingResult.value = ''

  try {
    const endpoint = `http://localhost:5047/api/ClinicalAssistant/${activeTab.value}`
    
    if (activeTab.value === 'stream') {
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
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prompt: prompt.value })
      })

      if (!response.ok) throw new Error(`Server returned status ${response.status}`)
      
      result.value = await response.json()
    }

    if (sidebarRef.value) {
      sidebarRef.value.fetchHistory()
    }
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

async function scrollToBottom() {
  await nextTick()
  if (chatContainerRef.value) {
    chatContainerRef.value.scrollTop = chatContainerRef.value.scrollHeight
  }
}
</script>

<template>
  <div class="app-layout">
    <!-- History Sidebar Component -->
    <HistorySidebar ref="sidebarRef" @select-log="handleSelectLog" />

    <!-- Main Workspace -->
    <main class="main-workspace">
      <header class="header">
        <h1>Healthcare AI Assistant Portal</h1>
        <span class="badge">.NET 8 + Ollama (phi4-mini)</span>
      </header>

      <div class="card">
        <div class="chat-header-actions">
          <button class="clear-btn" @click="clearChat">🗑️ Clear Thread</button>
        </div>
        <div class="tabs">
          <button :class="{ active: activeTab === 'chat' }" @click="activeTab = 'chat'">
            💬 Interactive Chat Agent
          </button>
          <button :class="{ active: activeTab === 'structured' }" @click="activeTab = 'structured'">
            Structured JSON
          </button>
          <button :class="{ active: activeTab === 'query' }" @click="activeTab = 'query'">
            Standard Query
          </button>
          <button :class="{ active: activeTab === 'stream' }" @click="activeTab = 'stream'">
            Real-Time Stream
          </button>
        </div>

        <!-- Chat Thread View -->
        <div v-if="activeTab === 'chat'" class="chat-container">
          <div ref="chatContainerRef" class="chat-messages">
            <div 
              v-for="(msg, index) in chatMessages" 
              :key="index" 
              :class="['chat-bubble', msg.role]"
            >
              <span class="role-label">{{ msg.role === 'user' ? 'You' : 'Assistant' }}</span>
              <!-- Use v-html for assistant messages to render Markdown, keep text for user messages -->
              <div v-if="msg.role === 'assistant'" class="markdown-body" v-html="renderMarkdown(msg.content)"></div>
              <p v-else>{{ msg.content }}</p>
            </div>
            <!-- <div v-if="loading" class="chat-bubble assistant typing">
              <span class="role-label">Assistant</span>
              <p>Thinking...</p>
            </div> -->
          </div>

          <div class="chat-input-area">
            <textarea 
              v-model="prompt" 
              rows="2" 
              placeholder="Ask a follow-up question..."
              @keydown.enter.prevent="handleChatSubmit"
            ></textarea>
            <button class="submit-btn" @click="handleChatSubmit" :disabled="loading">
              {{ loading ? 'Sending...' : 'Send' }}
            </button>
          </div>
        </div>

        <!-- Single-turn Views -->
        <div v-else>
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
        </div>

        <div v-if="error" class="error-box">
          ⚠️ Error: {{ error }}
        </div>

        <!-- Output for Structured / Standard JSON -->
        <div v-if="result && activeTab !== 'stream' && activeTab !== 'chat'" class="output-container">
          <h3>Response Payload:</h3>
          <pre>{{ JSON.stringify(result, null, 2) }}</pre>
        </div>

        <!-- Output for Real-Time Streaming -->
        <div v-if="activeTab === 'stream' && (streamingResult || loading)" class="output-container">
          <h3>Live Streaming Output:</h3>
          <div class="stream-box">{{ streamingResult }}<span v-if="loading" class="cursor">_</span></div>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  height: 100vh;
  margin: 0;
  font-family: system-ui, -apple-system, sans-serif;
  color: #1e293b;
  background: #f1f5f9;
  box-sizing: border-box;
}

.main-workspace {
  flex-grow: 1;
  padding: 30px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
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
  margin: 0;
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
  display: flex;
  flex-direction: column;
  flex-grow: 1;
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

.chat-container {
  display: flex;
  flex-direction: column;
  flex-grow: 1;
  height: 60vh;
}

.chat-messages {
  flex-grow: 1;
  overflow-y: auto;
  padding: 12px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  margin-bottom: 12px;
}

.chat-bubble {
  margin-bottom: 12px;
  padding: 12px;
  border-radius: 8px;
  max-width: 80%;
}

.chat-bubble.user {
  background: #dbeafe;
  margin-left: auto;
  text-align: right;
}

.chat-bubble.assistant {
  background: white;
  border: 1px solid #cbd5e1;
  margin-right: auto;
}

.role-label {
  display: block;
  font-size: 11px;
  font-weight: bold;
  color: #64748b;
  margin-bottom: 4px;
  text-transform: uppercase;
}

.chat-bubble p {
  margin: 0;
  font-size: 14px;
  line-height: 1.4;
}

.chat-input-area {
  display: flex;
  gap: 8px;
}

.chat-input-area textarea {
  flex-grow: 1;
  padding: 10px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  font-size: 14px;
  resize: none;
}

.chat-input-area .submit-btn {
  width: 100px;
  background: #2563eb;
  color: white;
  border: none;
  font-weight: 600;
  border-radius: 6px;
  cursor: pointer;
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
  resize: vertical;
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
  margin-top: 10px;
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

/* Markdown Styling inside Chat Bubbles */
.markdown-body {
  font-size: 14px;
  line-height: 1.5;
}

.markdown-body p {
  margin: 0 0 8px 0;
}

.markdown-body p:last-child {
  margin-bottom: 0;
}

.markdown-body ul, .markdown-body ol {
  margin: 4px 0 8px 20px;
  padding: 0;
}

.markdown-body li {
  margin-bottom: 4px;
}

.markdown-body code {
  background: #f1f5f9;
  padding: 2px 4px;
  border-radius: 4px;
  font-family: monospace;
  font-size: 13px;
  color: #0f172a;
}

.markdown-body pre {
  background: #0f172a;
  color: #38bdf8;
  padding: 12px;
  border-radius: 6px;
  overflow-x: auto;
  margin: 8px 0;
}

.markdown-body pre code {
  background: transparent;
  color: inherit;
  padding: 0;
}
</style>