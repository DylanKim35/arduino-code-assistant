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

        public async Task<string?> SendMessage(string message)
        {
            _chatRequest.Message = message; // 추가한 부분 @김영민
            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                    {
                        // FromSystem: 프롬프트, FromUser: 사용자 질문
                        ChatMessage.FromSystem(
                            "다음 자연어 명령을 수행하는 아두이노 코드를 JSON 형식으로 작성해 주세요.\n" +
                            "형식은 다음과 같습니다.\n" +
                            "{\"code\": \"[주석이 없는 아두이노 코드]\", \"description\": \"[코드의 라인 번호별 주석]\"}\n" +
                            "\n다음 항목에 주의 해 주세요.\n" +
                            "- 아두이노 코드 작성 시 개행(줄띄움)도 신경 써 주세요.\n" +
                            "- 설명 텍스트는 '~니다'와 같은 경어체를 사용해 주세요.\n" +
                            "- 정상적이지 않은 명령이라고 판단되면 code 및 description에 오류 메시지를 출력해 주세요.\n" +
                            "- 시리얼 통신이 필요한 경우, 보드레이트는 반드시 9600으로 설정해 주세요.\n"
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
            throw new Exception(completionResult.Error?.Message);
        }
    }
}
