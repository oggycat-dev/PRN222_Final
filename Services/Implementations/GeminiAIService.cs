using DAL.Entities;
using DAL.Repositories;
using Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Services.Implementations
{
    public class GeminiAIService : IAIService
    {
        private readonly ILogger<GeminiAIService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAIUsageRepository _aiUsageRepository;
        private readonly IPromptSessionRepository _promptSessionRepository;
        private readonly GoogleAI _googleAI;
        private readonly string _apiKey;

        public GeminiAIService(
            ILogger<GeminiAIService> logger,
            IConfiguration configuration,
            IAIUsageRepository aiUsageRepository,
            IPromptSessionRepository promptSessionRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _aiUsageRepository = aiUsageRepository;
            _promptSessionRepository = promptSessionRepository;
            
            // Get API key from configuration with better validation
            _apiKey = configuration["GEMINI_API_KEY"] ?? throw new InvalidOperationException("GEMINI_API_KEY is not configured");
            
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "null")
            {
                throw new InvalidOperationException("GEMINI_API_KEY is not properly configured");
            }
            
            _googleAI = new GoogleAI(_apiKey);
            _logger.LogInformation("GeminiAIService initialized successfully");
        }

        public async Task<AIResponse> SendMessageAsync(string message, int? sessionId = null, int? userId = null)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Validate input
                if (string.IsNullOrWhiteSpace(message))
                {
                    return new AIResponse
                    {
                        Success = false,
                        Message = "Tin nhắn không được để trống.",
                        ErrorDetails = "Empty message provided"
                    };
                }

                // Get the model with updated model name
                var model = _googleAI.GenerativeModel("gemini-1.5-flash");
                
                // Generate response with retry logic
                var response = await GenerateContentWithRetry(model, message, maxRetries: 3);
                stopwatch.Stop();

                var aiResponse = new AIResponse
                {
                    Success = true,
                    Message = "Phản hồi AI thành công.",
                    Response = response.Text ?? "Không có phản hồi từ AI.",
                    TokensUsed = EstimateTokens(message + (response.Text ?? "")),
                    Metadata = new AIResponseMetadata
                    {
                        Model = "gemini-1.5-flash",
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        TotalTokens = EstimateTokens(message + (response.Text ?? "")),
                        PromptTokens = EstimateTokens(message),
                        CompletionTokens = EstimateTokens(response.Text ?? "")
                    }
                };

                // Log usage if userId is provided
                if (userId.HasValue)
                {
                    await LogUsageAsync(userId.Value, "gemini-1.5-flash", message, response.Text ?? "", 
                                      aiResponse.TokensUsed, stopwatch.ElapsedMilliseconds, sessionId, true);
                }

                _logger.LogInformation($"AI response generated successfully for user {userId}. Tokens: {aiResponse.TokensUsed}, Time: {stopwatch.ElapsedMilliseconds}ms");
                
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating AI response for user {userId}");
                
                // Log failed usage if userId is provided
                if (userId.HasValue)
                {
                    await LogUsageAsync(userId.Value, "gemini-1.5-flash", message, "", 0, 0, sessionId, false, ex.Message);
                }

                // Provide user-friendly error messages
                string userMessage;
                if (ex.Message.Contains("Service Unavailable") || ex.Message.Contains("503"))
                {
                    userMessage = "🚧 Gemini AI hiện tại đang bảo trì. Vui lòng thử lại sau vài phút.";
                }
                else if (ex.Message.Contains("Too Many Requests") || ex.Message.Contains("429"))
                {
                    userMessage = "⏰ Đã vượt quá giới hạn số lần gọi API. Vui lòng thử lại sau vài phút.";
                }
                else if (ex.Message.Contains("unauthorized") || ex.Message.Contains("401"))
                {
                    userMessage = "🔑 Lỗi xác thực API. Vui lòng liên hệ quản trị viên.";
                }
                else if (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
                {
                    userMessage = "📊 Đã hết quota API. Vui lòng thử lại vào ngày mai.";
                }
                else if (ex.Message.StartsWith("Gemini AI"))
                {
                    userMessage = ex.Message; // Use our custom retry messages
                }
                else
                {
                    userMessage = "❌ Đã xảy ra lỗi không xác định khi kết nối với AI. Vui lòng thử lại.";
                }

                return new AIResponse
                {
                    Success = false,
                    Message = userMessage,
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<AIResponse> SendMessageWithContextAsync(string message, List<ChatMessage> chatHistory, int? userId = null)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Build conversation context
                var contextBuilder = new System.Text.StringBuilder();
                
                // Add chat history
                foreach (var historyMessage in chatHistory.TakeLast(10)) // Limit to last 10 messages
                {
                    var roleLabel = historyMessage.Role == "user" ? "Người dùng" : "AI";
                    contextBuilder.AppendLine($"{roleLabel}: {historyMessage.Content}");
                }
                
                // Add current message
                contextBuilder.AppendLine($"Người dùng: {message}");
                contextBuilder.AppendLine("AI:");

                var fullPrompt = contextBuilder.ToString();
                
                // Get the model with updated model name
                var model = _googleAI.GenerativeModel("gemini-1.5-flash");
                
                // Generate response
                var response = await model.GenerateContent(fullPrompt);
                stopwatch.Stop();

                var aiResponse = new AIResponse
                {
                    Success = true,
                    Message = "Phản hồi AI với ngữ cảnh thành công.",
                    Response = response.Text ?? "Không có phản hồi từ AI.",
                    TokensUsed = EstimateTokens(fullPrompt + (response.Text ?? "")),
                    Metadata = new AIResponseMetadata
                    {
                        Model = "gemini-1.5-flash",
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        TotalTokens = EstimateTokens(fullPrompt + (response.Text ?? "")),
                        PromptTokens = EstimateTokens(fullPrompt),
                        CompletionTokens = EstimateTokens(response.Text ?? "")
                    }
                };

                // Log usage if userId is provided
                if (userId.HasValue)
                {
                    await LogUsageAsync(userId.Value, "gemini-1.5-flash", message, response.Text ?? "", 
                                      aiResponse.TokensUsed, stopwatch.ElapsedMilliseconds, null, true);
                }

                _logger.LogInformation($"AI contextual response generated successfully for user {userId}. Tokens: {aiResponse.TokensUsed}");
                
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating AI contextual response for user {userId}");
                
                return new AIResponse
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo phản hồi AI với ngữ cảnh.",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<AIResponse> GenerateResponseAsync(string prompt, AIOptions? options = null)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Get the model with updated model name
                var model = _googleAI.GenerativeModel("gemini-1.5-flash");
                
                // Configure generation config if options provided
                if (options != null)
                {
                    // Note: Mscc.GenerativeAI may have different parameter names
                    // This is a basic implementation, adjust based on actual library API
                }
                
                // Generate response
                var response = await model.GenerateContent(prompt);
                stopwatch.Stop();

                return new AIResponse
                {
                    Success = true,
                    Message = "Tạo phản hồi thành công.",
                    Response = response.Text ?? "Không có phản hồi từ AI.",
                    TokensUsed = EstimateTokens(prompt + (response.Text ?? "")),
                    Metadata = new AIResponseMetadata
                    {
                        Model = "gemini-1.5-flash",
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        TotalTokens = EstimateTokens(prompt + (response.Text ?? "")),
                        PromptTokens = EstimateTokens(prompt),
                        CompletionTokens = EstimateTokens(response.Text ?? "")
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response with options");
                
                return new AIResponse
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo phản hồi AI.",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<AIResponse> GenerateTextAsync(string prompt, int maxTokens = 1000)
        {
            var options = new AIOptions
            {
                MaxTokens = maxTokens,
                Model = "gemini-1.5-flash"
            };
            
            return await GenerateResponseAsync(prompt, options);
        }

        public async Task<AIResponse> SummarizeTextAsync(string text, int maxLength = 500)
        {
            var prompt = $"Hãy tóm tắt đoạn văn bản sau trong khoảng {maxLength} từ bằng tiếng Việt:\n\n{text}";
            
            var options = new AIOptions
            {
                MaxTokens = maxLength + 100, // Add some buffer
                Model = "gemini-1.5-flash"
            };
            
            return await GenerateResponseAsync(prompt, options);
        }

        public async Task<AIResponse> TranslateTextAsync(string text, string targetLanguage = "vi")
        {
            var languageName = targetLanguage.ToLower() switch
            {
                "vi" => "tiếng Việt",
                "en" => "tiếng Anh",
                "fr" => "tiếng Pháp",
                "de" => "tiếng Đức",
                "es" => "tiếng Tây Ban Nha",
                "ja" => "tiếng Nhật",
                "ko" => "tiếng Hàn",
                "zh" => "tiếng Trung",
                _ => targetLanguage
            };
            
            var prompt = $"Hãy dịch đoạn văn bản sau sang {languageName}:\n\n{text}";
            
            return await GenerateResponseAsync(prompt);
        }

        public async Task<bool> ValidateAPIConnectionAsync()
        {
            try
            {
                var model = _googleAI.GenerativeModel("gemini-1.5-flash");
                var response = await model.GenerateContent("Hello, test connection.");
                
                return !string.IsNullOrEmpty(response.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate Gemini API connection");
                return false;
            }
        }

        public async Task<AIUsageStats> GetUsageStatsAsync(int userId)
        {
            try
            {
                var totalRequests = await _aiUsageRepository.GetUserTotalRequestsAsync(userId);
                var totalTokens = await _aiUsageRepository.GetUserTotalTokensAsync(userId);
                var requestsToday = await _aiUsageRepository.GetUserRequestsTodayAsync(userId);
                var tokensToday = await _aiUsageRepository.GetUserTokensTodayAsync(userId);
                var lastUsed = await _aiUsageRepository.GetUserLastUsageAsync(userId);

                return new AIUsageStats
                {
                    UserId = userId,
                    TotalRequests = totalRequests,
                    TotalTokensUsed = totalTokens,
                    RequestsToday = requestsToday,
                    TokensToday = tokensToday,
                    LastUsed = lastUsed ?? DateTime.MinValue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting usage stats for user {userId}");
                return new AIUsageStats { UserId = userId };
            }
        }

        private async Task LogUsageAsync(int userId, string model, string prompt, string response, 
                                       int tokensUsed, double processingTimeMs, int? sessionId, 
                                       bool isSuccess, string? errorMessage = null)
        {
            try
            {
                var aiUsage = new AIUsage
                {
                    UserId = userId,
                    Model = model,
                    Prompt = prompt.Length > 2000 ? prompt.Substring(0, 2000) : prompt,
                    Response = response,
                    TokensUsed = tokensUsed,
                    PromptTokens = EstimateTokens(prompt),
                    CompletionTokens = EstimateTokens(response),
                    ProcessingTimeMs = processingTimeMs,
                    PromptSessionId = sessionId,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage
                };

                await _aiUsageRepository.CreateAsync(aiUsage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to log AI usage for user {userId}");
            }
        }

        private async Task<Mscc.GenerativeAI.GenerateContentResponse> GenerateContentWithRetry(
            Mscc.GenerativeAI.GenerativeModel model, 
            string message, 
            int maxRetries = 3)
        {
            var attempt = 0;
            while (attempt < maxRetries)
            {
                try
                {
                    _logger.LogInformation($"🔄 Gemini API attempt {attempt + 1}/{maxRetries}");
                    var response = await model.GenerateContent(message);
                    _logger.LogInformation($"✅ Gemini API succeeded on attempt {attempt + 1}");
                    return response;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("503") || ex.Message.Contains("Service Unavailable"))
                {
                    attempt++;
                    if (attempt >= maxRetries)
                    {
                        _logger.LogError($"❌ Gemini API failed after {maxRetries} attempts: {ex.Message}");
                        throw new Exception($"Gemini AI hiện tại không khả dụng. Vui lòng thử lại sau vài phút. (Đã thử {maxRetries} lần)");
                    }

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff: 2s, 4s, 8s
                    _logger.LogWarning($"⚠️ Gemini API attempt {attempt} failed (503 Service Unavailable). Retrying in {delay.TotalSeconds}s...");
                    await Task.Delay(delay);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
                {
                    attempt++;
                    if (attempt >= maxRetries)
                    {
                        _logger.LogError($"❌ Gemini API rate limited after {maxRetries} attempts");
                        throw new Exception("Đã vượt quá giới hạn số lần gọi API. Vui lòng thử lại sau vài phút.");
                    }

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 2)); // Longer delay for rate limits: 8s, 16s, 32s
                    _logger.LogWarning($"⚠️ Gemini API rate limited on attempt {attempt}. Retrying in {delay.TotalSeconds}s...");
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Gemini API failed with non-retryable error: {ex.Message}");
                    throw; // Re-throw non-retryable exceptions immediately
                }
            }

            throw new Exception($"Gemini AI không thể xử lý yêu cầu sau {maxRetries} lần thử.");
        }

        private int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            
            // Rough estimation: 1 token ≈ 4 characters for most languages
            // This is a simplified estimation, real token counting would require the actual tokenizer
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }
} 