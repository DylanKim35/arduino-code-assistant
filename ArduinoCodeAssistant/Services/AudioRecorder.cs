using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System;
using System.IO;

namespace ArduinoCodeAssistant.Services
{
    internal class AudioRecorder
    {
        private WaveInEvent waveIn;
        private string outputFilePath;
        private WaveFileWriter writer;
        private TaskCompletionSource<bool> recordingStoppedTcs;

        public AudioRecorder()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(44100, 1);
            outputFilePath = "";
        }

        public void StartRecording(string audioPath)
        {
            outputFilePath = Path.Combine(Path.GetTempPath(), audioPath);
            writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);
            recordingStoppedTcs = new TaskCompletionSource<bool>();

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            waveIn.StartRecording();
        }

        public async Task StopRecordingAsync()
        {
            waveIn.StopRecording();
            await recordingStoppedTcs.Task;

            // Unsubscribe to avoid potential memory leaks
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.RecordingStopped -= OnRecordingStopped;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            writer.Dispose();
            waveIn.Dispose();
            recordingStoppedTcs.TrySetResult(true); // Use TrySetResult to avoid exceptions
        }

        public string GetAudioFilePath()
        {
            return outputFilePath;
        }
    }
}
