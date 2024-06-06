using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace ArduinoCodeAssistant.Services
{
    public class WhisperService
    {
        private readonly HttpClient _httpClient;

        public WhisperService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetTranscript(string audioPath)
        {
            string openaiApiKey = "sk-proj-XnyMkLcbZr09wOqabAlxT3BlbkFJY4sDm0IwiqI41mDzrxT1";
            string apiUrl = "https://api.openai.com/v1/audio/transcriptions";

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openaiApiKey);

                MultipartFormDataContent formData = new MultipartFormDataContent();
                formData.Add(new ByteArrayContent(await File.ReadAllBytesAsync(audioPath)), "file", "audio.mp3");
                formData.Add(new StringContent("whisper-1"), "model");

                HttpResponseMessage response = await httpClient.PostAsync(apiUrl, formData);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(jsonString);

                    return jsonResponse["text"]?.ToString() ?? "음성 인식 실패";
                }

                else
                {
                    Console.WriteLine($"Failed to transcribe audio. Status code: {response.StatusCode}");
                    return "음성 인식 실패";
                }
            }
        }
    }
}
