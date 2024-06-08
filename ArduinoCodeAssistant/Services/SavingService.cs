using ArduinoCodeAssistant.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace ArduinoCodeAssistant.Services
{
    public class SavingService
    {
        private const string SaveFilePath = "advancedTextStates.json";
        public Dictionary<Guid, TextState> TextStatesDictionary { get; } = [];
        public ObservableCollection<TextState> TextStatesCollection { get; } = [];

        LoggingService _loggingService;
        public SavingService(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public void AddEmptyTemplate()
        {
            var newState = new TextState();
            TextStatesDictionary.Add(newState.Id, newState);
            TextStatesCollection.Add(newState);
        }

        public async Task SaveTextStatesAsync(Guid id, string generatedTag, string boardStatus, string requestingMessage, string generatedCode, string generatedDescription)
        {
            if (string.IsNullOrWhiteSpace(generatedTag) || !TextStatesDictionary.ContainsKey(id))
            {
                return;
            }

            var state = TextStatesDictionary[id];
            bool needsUpdate = false;

            if (state.Id != id)
            {
                state.Id = id;
                needsUpdate = true;
            }
            if (state.GeneratedTag != generatedTag)
            {
                state.GeneratedTag = generatedTag;
                needsUpdate = true;
            }
            if (state.BoardStatus != boardStatus)
            {
                state.BoardStatus = boardStatus;
                needsUpdate = true;
            }
            if (state.RequestingMessage != requestingMessage)
            {
                state.RequestingMessage = requestingMessage;
                needsUpdate = true;
            }
            if (state.GeneratedCode != generatedCode)
            {
                state.GeneratedCode = generatedCode;
                needsUpdate = true;
            }
            if (state.GeneratedDescription != generatedDescription)
            {
                state.GeneratedDescription = generatedDescription;
                needsUpdate = true;
            }

            if (!needsUpdate) return;

            var json = JsonConvert.SerializeObject(TextStatesDictionary, Formatting.Indented);

            try
            {
                await File.WriteAllTextAsync(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                _loggingService.Log("파일 쓰기 오류: ", LoggingService.LogLevel.Error, ex);
            }
        }

        public async Task RemoveTextStatesAsync(Guid id)
        {
            if(TextStatesDictionary.ContainsKey(id))
            {
                var textState = TextStatesDictionary[id];
                TextStatesDictionary.Remove(id);
                TextStatesCollection.Remove(textState);

                var json = JsonConvert.SerializeObject(TextStatesDictionary, Formatting.Indented);

                try
                {
                    await File.WriteAllTextAsync(SaveFilePath, json);
                }
                catch (Exception ex)
                {
                    _loggingService.Log("파일 쓰기 오류: ", LoggingService.LogLevel.Error, ex);
                }
            }
        }

        //public async Task LoadTextStatesAsync()
        //{
        //    TextStatesDictionary.Clear();
        //    TextStatesCollection.Clear();

        //    if (File.Exists(SaveFilePath))
        //    {
        //        var json = await File.ReadAllTextAsync(SaveFilePath);
        //        var states = JsonConvert.DeserializeObject<Dictionary<Guid, TextState>>(json);
        //        if (states != null)
        //        {
        //            foreach (var kvp in states)
        //            {
        //                TextStatesDictionary[kvp.Key] = kvp.Value;
        //                TextStatesCollection.Add(kvp.Value);
        //            }
        //        }
        //    }

        //    if(TextStatesDictionary.Count == 0 && TextStatesCollection.Count == 0)
        //    {
        //        var newState = new TextState();
        //        TextStatesDictionary.Add(newState.Id, newState);
        //        TextStatesCollection.Add(newState);
        //    }    
        //}

        public void InitializeTextStates()
        {
            TextStatesDictionary.Clear();
            TextStatesCollection.Clear();

            if (File.Exists(SaveFilePath))
            {
                var json = File.ReadAllText(SaveFilePath);
                var states = JsonConvert.DeserializeObject<Dictionary<Guid, TextState>>(json);
                if (states != null)
                {
                    foreach (var kvp in states)
                    {
                        TextStatesDictionary[kvp.Key] = kvp.Value;
                        TextStatesCollection.Add(kvp.Value);
                    }
                }
            }

            if (TextStatesDictionary.Count == 0 && TextStatesCollection.Count == 0)
            {
                AddEmptyTemplate();
            }
        }
    }
}
