using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Scripts;
using UnityEngine;
using ValkyrieTools;

// Reads quest text aloud: natural Piper voices (PiperSpeech) or Windows voices synthesize WAV audio in the background, Unity plays it (see TextToSpeechNarrator)
public static class TextToSpeech
{
    public const string CONFIG_KEY = "readAloud";

    private const float SPEAKING_RATE = 0.85f;
    private const float VOICE_PITCH = 0.8f;
    private const string SENTENCE_PAUSE = "<break time='600ms'/>";
    private const string VOICE_LANGUAGE_TOKEN = "__VOICE_LANGUAGE__";

    private const string SETUP_COMMAND = "Add-Type -AssemblyName System.Runtime.WindowsRuntime"
        + "; $null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer,Windows.Media.SpeechSynthesis,ContentType=WindowsRuntime]"
        + "; $asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0].MakeGenericMethod([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])"
        + "; $synth = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer"
        + "; $synth.Options.SpeakingRate = __RATE__; $synth.Options.AudioPitch = __PITCH__";
    // Prefers a male voice of the language; the SSML arrives as base64 so any characters pass through the console safely
    private const string SPEAK_COMMAND = "try { $voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices | Where-Object { $_.Language.StartsWith('__LANGUAGE__') } | Sort-Object { $_.Gender -ne 'Male' } | Select-Object -First 1"
        + "; if ($voice) { $synth.Voice = $voice }"
        + "; $ssml = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('__SSML__')).Replace('" + VOICE_LANGUAGE_TOKEN + "', $synth.Voice.Language)"
        + "; $task = $asTask.Invoke($null, @($synth.SynthesizeSsmlToStreamAsync($ssml))); $task.Wait(-1) | Out-Null"
        + "; $wav = New-Object System.IO.MemoryStream; [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($task.Result).CopyTo($wav)"
        + "; [Console]::Out.WriteLine('SPEECH __ID__ ' + [System.Convert]::ToBase64String($wav.ToArray())) }"
        + " catch { [Console]::Out.WriteLine('ERROR __ID__ ' + $_) }";

    private static readonly Dictionary<string, string> LANGUAGE_CODES = new Dictionary<string, string>
    {
        { "English", "en" },
        { "German", "de" },
        { "French", "fr" },
        { "Spanish", "es" },
        { "Italian", "it" },
        { "Chinese", "zh" },
        { "Czech", "cs" },
        { "Japanese", "ja" },
        { "Korean", "ko" },
        { "Polish", "pl" },
        { "Portuguese", "pt" },
        { "Russian", "ru" },
        { "Ukrainian", "uk" },
    };

    private static readonly Regex RICH_TEXT_TAG = new Regex(@"<[^<>]+>");
    private static readonly Regex NAMED_SYMBOL = new Regex(@"\{([A-Za-z]+)\}");
    private static readonly Regex OTHER_MARKER = new Regex(@"\{[^{}]*\}");
    private static readonly Regex WHITESPACE = new Regex(@"\s+");
    private static readonly Regex SENTENCE_END = new Regex(@"(?<=[.!?…])\s+");

    private static readonly object speechLock = new object();
    private static int requestId;
    private static byte[] pendingSpeech;

    private static Process speechProcess;
    private static TextToSpeechNarrator narrator;
    private static bool failed;

    public static bool IsSupported()
    {
        return Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
    }

    public static bool IsEnabled()
    {
        return IsSupported() && !failed && Game.Get().config.data.Get("UserConfig", CONFIG_KEY) == "1";
    }

    public static void Speak(string text)
    {
        if (!IsEnabled()) return;

        Stop();
        string speechText = ToSpeechText(EventManager.InputSymbolReplace(text));
        if (speechText.Length == 0) return;

        int id;
        lock (speechLock)
        {
            id = requestId;
        }
        string language = PrepareVoice();
        if (PiperSpeech.IsInstalled(language) && PiperSpeech.Speak(id, language, speechText)) return;

        Send(BuildSpeakCommand(id, language, BuildSsml(speechText)));
    }

    // Starts downloading the natural voice for the current language if needed, Windows voices are used until it is ready
    public static string PrepareVoice()
    {
        if (narrator == null)
        {
            narrator = TextToSpeechNarrator.Create();
        }
        string language = GetLanguageCode(Game.Get().currentLang);
        PiperSpeech.Install(language, narrator);
        return language;
    }

    public static void DeliverSpeech(int id, byte[] wav)
    {
        lock (speechLock)
        {
            if (id == requestId)
            {
                pendingSpeech = wav;
            }
        }
    }

    public static void Stop()
    {
        lock (speechLock)
        {
            requestId++;
            pendingSpeech = null;
        }
        if (narrator != null)
        {
            narrator.StopSpeaking();
        }
    }

    // Returns the WAV audio of the latest request once synthesized, null otherwise
    public static byte[] TakeSpeech()
    {
        lock (speechLock)
        {
            byte[] speech = pendingSpeech;
            pendingSpeech = null;
            return speech;
        }
    }

    public static string ToSpeechText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        string output = text.Replace("\\n", " ");
        output = RICH_TEXT_TAG.Replace(output, "");
        output = NAMED_SYMBOL.Replace(output, "$1");
        output = OTHER_MARKER.Replace(output, "");
        return WHITESPACE.Replace(output, " ").Trim();
    }

    public static string GetLanguageCode(string language)
    {
        if (language != null && LANGUAGE_CODES.TryGetValue(language, out string code))
        {
            return code;
        }
        return LANGUAGE_CODES[ValkyrieConstants.DefaultLanguage];
    }

    public static string BuildSsml(string speechText)
    {
        var body = new StringBuilder();
        foreach (string sentence in SENTENCE_END.Split(speechText))
        {
            if (body.Length > 0) body.Append(SENTENCE_PAUSE);
            body.Append(SecurityElement.Escape(sentence));
        }
        return "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='" + VOICE_LANGUAGE_TOKEN + "'>" + body + "</speak>";
    }

    public static string BuildSpeakCommand(int id, string languageCode, string ssml)
    {
        return SPEAK_COMMAND
            .Replace("__ID__", id.ToString(CultureInfo.InvariantCulture))
            .Replace("__LANGUAGE__", languageCode)
            .Replace("__SSML__", Convert.ToBase64String(Encoding.UTF8.GetBytes(ssml)));
    }

    public static bool TryReadWav(byte[] wav, out float[] samples, out int channels, out int sampleRate)
    {
        samples = null;
        channels = 0;
        sampleRate = 0;
        if (wav == null || wav.Length < 12 || Encoding.ASCII.GetString(wav, 0, 4) != "RIFF" || Encoding.ASCII.GetString(wav, 8, 4) != "WAVE") return false;

        int bitsPerSample = 0;
        int position = 12;
        while (position + 8 <= wav.Length)
        {
            string chunkId = Encoding.ASCII.GetString(wav, position, 4);
            int chunkSize = BitConverter.ToInt32(wav, position + 4);
            int chunkStart = position + 8;
            if (chunkSize < 0) return false;

            if (chunkId == "fmt " && chunkSize >= 16 && chunkStart + 16 <= wav.Length)
            {
                bool isPcm = BitConverter.ToInt16(wav, chunkStart) == 1;
                if (!isPcm) return false;
                channels = BitConverter.ToInt16(wav, chunkStart + 2);
                sampleRate = BitConverter.ToInt32(wav, chunkStart + 4);
                bitsPerSample = BitConverter.ToInt16(wav, chunkStart + 14);
            }
            else if (chunkId == "data")
            {
                if (bitsPerSample != 16 || channels <= 0) return false;
                int sampleCount = Math.Min(chunkSize, wav.Length - chunkStart) / 2;
                samples = new float[sampleCount - sampleCount % channels];
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = BitConverter.ToInt16(wav, chunkStart + i * 2) / 32768f;
                }
                return true;
            }
            position = chunkStart + chunkSize + chunkSize % 2;
        }
        return false;
    }

    private static void OnSpeechOutput(string line)
    {
        if (line == null) return;

        string[] parts = line.Split(new[] { ' ' }, 3);
        if (parts.Length < 3 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) return;

        if (parts[0] == "ERROR")
        {
            ValkyrieDebug.Log("Warning: Text to speech could not synthesize text: " + parts[2]);
            return;
        }
        if (parts[0] != "SPEECH") return;

        DeliverSpeech(id, Convert.FromBase64String(parts[2]));
    }

    private static void Send(string command)
    {
        try
        {
            if (speechProcess == null || speechProcess.HasExited)
            {
                StartSpeechProcess();
            }
            speechProcess.StandardInput.WriteLine(command);
        }
        catch (Exception e)
        {
            ValkyrieDebug.Log("Warning: Text to speech failed, disabling it: " + e.Message);
            failed = true;
            Shutdown();
        }
    }

    private static void StartSpeechProcess()
    {
        var startInfo = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command -")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        speechProcess = Process.Start(startInfo);
        speechProcess.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) { OnSpeechOutput(e.Data); };
        speechProcess.BeginOutputReadLine();
        speechProcess.StandardInput.AutoFlush = true;
        speechProcess.StandardInput.WriteLine(SETUP_COMMAND
            .Replace("__RATE__", SPEAKING_RATE.ToString(CultureInfo.InvariantCulture))
            .Replace("__PITCH__", VOICE_PITCH.ToString(CultureInfo.InvariantCulture)));
        Application.quitting -= Shutdown;
        Application.quitting += Shutdown;
    }

    private static void Shutdown()
    {
        try
        {
            if (speechProcess != null && !speechProcess.HasExited)
            {
                speechProcess.Kill();
            }
        }
        catch (Exception e)
        {
            ValkyrieDebug.Log("Warning: Could not stop text to speech process: " + e.Message);
        }
        speechProcess = null;
    }
}
