using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using ArduinoCodeAssistant.Models;
using OpenAI.Managers;
using OpenAI;

namespace ArduinoCodeAssistant.Services
{
    public class ChatService
    {
        private readonly IOpenAIService _openAiService;
        private readonly ChatRequest _chatRequest;

        public ChatService(ChatRequest chatRequest)
        {
            _chatRequest = chatRequest;
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = "sk-proj-XnyMkLcbZr09wOqabAlxT3BlbkFJY4sDm0IwiqI41mDzrxT1"
            });
            _openAiService = openAiService;
        }

        public async Task<string> SendMessage(string message)
        {
            _chatRequest.Message = message; // 추가한 부분 @김영민
            try
            {
                var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = new List<ChatMessage>
                    {
                        // FromSystem: 프롬프트, FromUser: 사용자 질문
                        ChatMessage.FromSystem(
                            "다음 자연어 명령을 수행하는 아두이노 코드를 JSON 형식으로 작성해 주세요.\n" +
                            "형식은 다음과 같습니다.\n" +
                            "{\"code\": \"[아두이노 코드]\", \"description\": \"[코드에 대한 설명]\"}"
                            ),
                        ChatMessage.FromUser(message)
                    },
                    Model = OpenAI.ObjectModels.Models.Gpt_4o,
                    MaxTokens = 1000
                });


                if (completionResult.Successful)
                {
                    return completionResult.Choices.First().Message.Content;
                }
                else
                {
                    return $"[Error] API Error: {completionResult.Error?.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"[Error] {ex.Message}";
            }
        }
    }
}
