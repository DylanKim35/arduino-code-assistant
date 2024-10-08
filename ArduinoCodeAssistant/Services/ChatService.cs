﻿using System;
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

        public async Task<string?> SendMessage(string message, string boardStatus)
        {
            _chatRequest.Message = message; // 추가한 부분 @김영민
            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem(
                            "다음 자연어 명령을 수행하는 아두이노 코드를 JSON 형식으로 작성해 주세요.\n" +
                            "JSON 형식은 다음과 같습니다:\n" +
                            "{\n" +
                            "    \"code\": \"[주석이 없는 아두이노 코드 문자열]\",\n" +
                            "    \"description\": \"[생성한 아두이노 코드에 해당하는 주석 문자열, 주석 형식은 '(실행 순서를 나타내는 숫자). (내용)']\",\n" +
                            "    \"tag\": \"[코드의 기능을 요약하는 20자 미만의 한글 제목 문자열]\"\n" +
                            "}\n" +
                            "\n현재 사용자의 아두이노 보드 상태는 다음과 같습니다. 필요하다면 응답 생성 시 참고하고, 불필요하다고 판단되면 무시하세요.:\n" +
                            $"- 아두이노 보드 상태: {boardStatus}\n" +
                            "\n다음 항목에 주의해 주세요:\n" +
                            "- code 작성 시 개행(줄띄움)도 신경 써 주세요.\n" +
                            "- description 작성 시 개행(줄띄움)도 신경 써 주세요.\n" +
                            "- description은 '~니다'와 같은 경어체를 사용해 주세요.\n" +
                            "- description 및 tag에는 특수 문자(예: /, +, -, \\)를 포함하지 마세요.\n" +
                            "- 들여쓰기는 space 대신 tab을 사용해 주세요.\n" +
                            "- code는 Allman 스타일로 작성해 주세요.\n" +
                            "- code는 최대한 효율적이고 간략한 알고리즘으로 작성해 주세요.\n" +
                            "- 정상적이지 않은 명령이라고 판단되면 code에는 아무 텍스트도 작성하지 말고, description에만 오류 메시지를 작성해 주세요.\n" +
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
