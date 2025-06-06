# Ollama Report Web API

一個基於 Ollama 的報告生成 Web API 服務

## 專案概述

本專案提供一個 RESTful Web API，用於與 Ollama 本地大型語言模型進行互動，專門針對報告生成和分析功能進行優化。

## 功能特色

- 🚀 與 Ollama 本地模型無縫整合
- 📊 自動化報告生成
- 🔧 RESTful API 設計
- 📝 多種報告格式支援
- ⚡ 高效能處理
- 🛡️ 安全性保障

## 系統需求

- Node.js >= 16.0.0
- Ollama 已安裝並運行
- 至少 8GB RAM
- 支援的作業系統：Linux、macOS、Windows

## 安裝指南

### 1. 克隆專案

```bash
git clone https://github.com/s05061343/ollama-report-webapi.git
cd ollama-report-webapi
```

### 2. 安裝依賴

```bash
npm install
```

### 3. 環境配置

建立 `.env` 檔案：

```env
# 伺服器配置
PORT=3000
HOST=localhost

# Ollama 配置
OLLAMA_HOST=http://localhost:11434
OLLAMA_MODEL=llama2

# 資料庫配置（如果適用）
DATABASE_URL=your_database_url

# API 金鑰配置
API_KEY=your_secret_api_key
```

### 4. 啟動服務

```bash
# 開發模式
npm run dev

# 生產模式
npm start
```

## API 文檔

### 基本資訊

- **基礎 URL**: `http://localhost:3000/api`
- **認證方式**: API Key (Header: `X-API-Key`)
- **內容類型**: `application/json`

### 端點說明

#### 1. 健康檢查

```http
GET /health
```

**回應範例：**
```json
{
  "status": "ok",
  "timestamp": "2025-06-06T10:00:00Z",
  "ollama_status": "connected"
}
```

#### 2. 生成報告

```http
POST /reports/generate
```

**請求參數：**
```json
{
  "title": "報告標題",
  "content": "報告內容或資料",
  "format": "markdown|html|pdf",
  "model": "llama2",
  "parameters": {
    "temperature": 0.7,
    "max_tokens": 2000
  }
}
```

**回應範例：**
```json
{
  "success": true,
  "report_id": "report_123456",
  "generated_report": "# 報告標題\n\n生成的報告內容...",
  "metadata": {
    "model_used": "llama2",
    "tokens_used": 1250,
    "generation_time": "2.5s"
  }
}
```

#### 3. 獲取報告狀態

```http
GET /reports/{report_id}
```

#### 4. 列出可用模型

```http
GET /models
```

**回應範例：**
```json
{
  "models": [
    {
      "name": "llama2",
      "size": "7B",
      "status": "available"
    },
    {
      "name": "codellama",
      "size": "13B",
      "status": "available"
    }
  ]
}
```

## 使用範例

### JavaScript/Node.js

```javascript
const axios = require('axios');

async function generateReport() {
  try {
    const response = await axios.post('http://localhost:3000/api/reports/generate', {
      title: '月度銷售報告',
      content: '2024年12月銷售數據分析',
      format: 'markdown',
      model: 'llama2'
    }, {
      headers: {
        'X-API-Key': 'your_api_key',
        'Content-Type': 'application/json'
      }
    });
    
    console.log('報告生成成功：', response.data);
  } catch (error) {
    console.error('錯誤：', error.response.data);
  }
}

generateReport();
```

### Python

```python
import requests
import json

def generate_report():
    url = "http://localhost:3000/api/reports/generate"
    headers = {
        "X-API-Key": "your_api_key",
        "Content-Type": "application/json"
    }
    
    data = {
        "title": "月度銷售報告",
        "content": "2024年12月銷售數據分析",
        "format": "markdown",
        "model": "llama2"
    }
    
    response = requests.post(url, headers=headers, data=json.dumps(data))
    
    if response.status_code == 200:
        print("報告生成成功：", response.json())
    else:
        print("錯誤：", response.json())

generate_report()
```

### cURL

```bash
curl -X POST http://localhost:3000/api/reports/generate \
  -H "X-API-Key: your_api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "月度銷售報告",
    "content": "2024年12月銷售數據分析",
    "format": "markdown",
    "model": "llama2"
  }'
```

## 設定說明

### Ollama 模型管理

確保您已安裝所需的 Ollama 模型：

```bash
# 安裝模型
ollama pull llama2
ollama pull codellama

# 查看已安裝的模型
ollama list
```

### 效能調優

- **記憶體配置**: 根據使用的模型調整記憶體設定
- **並發處理**: 設定適當的並發請求數量
- **快取策略**: 啟用回應快取以提升效能

## 疑難排解

### 常見問題

1. **無法連接到 Ollama**
   - 確認 Ollama 服務正在運行
   - 檢查 `OLLAMA_HOST` 設定是否正確

2. **模型載入失敗**
   - 確認模型已正確安裝
   - 檢查可用記憶體是否足夠

3. **API 回應緩慢**
   - 考慮使用較小的模型
   - 調整 `max_tokens` 參數
   - 檢查硬體資源使用情況

### 日誌查看

```bash
# 查看應用程式日誌
npm run logs

# 查看 Ollama 日誌
ollama logs
```

## 開發指南

### 專案結構

```
ollama-report-webapi/
├── src/
│   ├── controllers/
│   ├── middleware/
│   ├── models/
│   ├── routes/
│   └── utils/
├── tests/
├── docs/
├── config/
└── scripts/
```

### 測試

```bash
# 運行所有測試
npm test

# 運行特定測試
npm test -- --grep "報告生成"

# 測試覆蓋率
npm run test:coverage
```

### 建置和部署

```bash
# 建置專案
npm run build

# Docker 部署
docker build -t ollama-report-webapi .
docker run -p 3000:3000 ollama-report-webapi
```

## 貢獻指南

歡迎貢獻！請遵循以下步驟：

1. Fork 本專案
2. 建立功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交變更 (`git commit -m 'Add some amazing feature'`)
4. 推送到分支 (`git push origin feature/amazing-feature`)
5. 開啟 Pull Request

## 授權條款

本專案採用 MIT 授權條款。詳見 [LICENSE](LICENSE) 檔案。

## 聯絡資訊

- **作者**: s05061343
- **Email**: your-email@example.com
- **GitHub**: [s05061343](https://github.com/s05061343)

## 更新日誌

### v1.0.0 (2025-06-06)
- 初始版本發布
- 基本報告生成功能
- RESTful API 實作
- Ollama 整合

---

**注意**: 本文檔基於專案名稱推測生成。請根據實際專案內容進行調整。
