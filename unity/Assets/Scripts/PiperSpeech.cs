using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using ValkyrieTools;

// Natural offline voices: Piper (github.com/rhasspy/piper, MIT) with voices from huggingface.co/rhasspy/piper-voices, downloaded on demand
public static class PiperSpeech
{
    private const string ENGINE_URL = "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip";
    private const string VOICES_URL = "https://huggingface.co/rhasspy/piper-voices/resolve/5041a7a49ae5b3a0f30e6837769eb03fdbb231bd/";
    private const float LENGTH_SCALE = 1.15f;
    private const float SENTENCE_SILENCE = 0.6f;

    // Male voices where available; Chinese and Japanese voices need a newer Piper, so those languages keep the Windows voice
    private static readonly Dictionary<string, string> VOICES = new Dictionary<string, string>
    {
        { "pl", "pl/pl_PL/bass/high/pl_PL-bass-high" },
        { "en", "en/en_GB/alan/medium/en_GB-alan-medium" },
        { "de", "de/de_DE/thorsten/medium/de_DE-thorsten-medium" },
        { "fr", "fr/fr_FR/tom/medium/fr_FR-tom-medium" },
        { "es", "es/es_ES/davefx/medium/es_ES-davefx-medium" },
        { "it", "it/it_IT/riccardo/x_low/it_IT-riccardo-x_low" },
        { "cs", "cs/cs_CZ/jirka/medium/cs_CZ-jirka-medium" },
        { "ko", "ko/ko_KR/kss/medium/ko_KR-kss-medium" },
        { "pt", "pt/pt_BR/faber/medium/pt_BR-faber-medium" },
        { "ru", "ru/ru_RU/dmitri/medium/ru_RU-dmitri-medium" },
        { "uk", "uk/uk_UA/oleksa/high/uk_UA-oleksa-high" },
    };

    private static readonly Regex OUTPUT_FILE_ID = new Regex(@"speech_(\d+)\.wav\s*$");

    private static readonly object requestLock = new object();
    private static readonly Queue<string> pendingRequests = new Queue<string>();
    private static bool synthesizing;

    private static Process process;
    private static StreamWriter input;
    private static string processLanguage;
    private static bool installing;
    private static readonly HashSet<string> failedInstalls = new HashSet<string>();

    public static bool IsDownloading()
    {
        return installing;
    }

    public static bool IsInstalled(string language)
    {
        return VOICES.ContainsKey(language) && File.Exists(EnginePath()) && File.Exists(ModelPath(language)) && File.Exists(ModelPath(language) + ".json");
    }

    // Downloads the engine and the voice for the language in the background when missing
    public static void Install(string language, MonoBehaviour host)
    {
        if (!VOICES.ContainsKey(language) || IsInstalled(language) || installing || failedInstalls.Contains(language)) return;

        installing = true;
        host.StartCoroutine(InstallRoutine(language));
    }

    // Loads the voice model ahead of the first request
    public static void WarmUp(string language)
    {
        try
        {
            lock (requestLock)
            {
                EnsureProcess(language);
            }
        }
        catch (Exception e)
        {
            ValkyrieDebug.Log("Warning: Piper text to speech could not start: " + e.Message);
            Shutdown();
        }
    }

    // Returns false when Piper could not take the request
    public static bool Speak(int id, string language, string[] sentences)
    {
        try
        {
            lock (requestLock)
            {
                EnsureProcess(language);
                string outputFile = Path.Combine(Application.temporaryCachePath, "speech_" + id + ".wav");
                foreach (string sentence in sentences)
                {
                    pendingRequests.Enqueue(BuildRequest(sentence, outputFile));
                }
                if (!synthesizing)
                {
                    SendNextRequest();
                }
            }
            return true;
        }
        catch (Exception e)
        {
            ValkyrieDebug.Log("Warning: Piper text to speech failed: " + e.Message);
            Shutdown();
            return false;
        }
    }

    public static void Cancel()
    {
        lock (requestLock)
        {
            pendingRequests.Clear();
        }
    }

    public static string BuildRequest(string speechText, string outputFile)
    {
        return "{\"text\":\"" + JsonEscape(speechText) + "\",\"output_file\":\"" + JsonEscape(outputFile) + "\"}";
    }

    public static string JsonEscape(string text)
    {
        var escaped = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c == '"' || c == '\\')
            {
                escaped.Append('\\').Append(c);
            }
            else if (c < ' ')
            {
                escaped.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            }
            else
            {
                escaped.Append(c);
            }
        }
        return escaped.ToString();
    }

    private static string Root()
    {
        return Path.Combine(Game.DefaultAppData(), "TextToSpeech");
    }

    private static string EnginePath()
    {
        return Path.Combine(Root(), "piper", "piper.exe");
    }

    private static string ModelPath(string language)
    {
        return Path.Combine(Root(), "voices", Path.GetFileName(VOICES[language]) + ".onnx");
    }

    private static IEnumerator InstallRoutine(string language)
    {
        bool ok = true;
        Directory.CreateDirectory(Path.Combine(Root(), "voices"));

        if (!File.Exists(EnginePath()))
        {
            string archive = Path.Combine(Root(), "piper.zip");
            yield return Download(ENGINE_URL, archive, result => ok = result);
            if (ok)
            {
                var extract = new Thread(() => ZipManager.Extract(Root(), archive, ZipManager.Extract_mode.ZIPMANAGER_EXTRACT_FULL));
                extract.Start();
                while (extract.IsAlive)
                {
                    yield return null;
                }
                File.Delete(archive);
                ok = File.Exists(EnginePath());
            }
        }

        string model = ModelPath(language);
        if (ok && !File.Exists(model + ".json"))
        {
            yield return Download(VOICES_URL + VOICES[language] + ".onnx.json", model + ".json", result => ok = result);
        }
        if (ok && !File.Exists(model))
        {
            yield return Download(VOICES_URL + VOICES[language] + ".onnx", model, result => ok = result);
        }

        if (!ok)
        {
            failedInstalls.Add(language);
        }
        installing = false;
    }

    private static IEnumerator Download(string url, string path, Action<bool> done)
    {
        string partPath = path + ".part";
        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, new DownloadHandlerFile(partPath) { removeFileOnAbort = true }, null))
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                ValkyrieDebug.Log("Warning: Unable to download text to speech file: " + url + " Error: " + request.error);
                done(false);
                yield break;
            }
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }
        File.Move(partPath, path);
        done(true);
    }

    private static void EnsureProcess(string language)
    {
        if (process == null || process.HasExited || processLanguage != language)
        {
            StartProcess(language);
        }
    }

    // Sentences go to Piper one at a time so they play as soon as ready and a cancelled text delays the next one by one sentence at most
    private static void SendNextRequest()
    {
        synthesizing = pendingRequests.Count > 0;
        if (synthesizing)
        {
            input.WriteLine(pendingRequests.Dequeue());
        }
    }

    private static void StartProcess(string language)
    {
        Shutdown();
        string arguments = "--model \"" + ModelPath(language) + "\" --json-input --quiet"
            + " --length_scale " + LENGTH_SCALE.ToString(CultureInfo.InvariantCulture)
            + " --sentence_silence " + SENTENCE_SILENCE.ToString(CultureInfo.InvariantCulture);
        var startInfo = new ProcessStartInfo(EnginePath(), arguments)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        process = Process.Start(startInfo);
        process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) { OnOutput(e.Data); };
        process.BeginOutputReadLine();
        input = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)) { AutoFlush = true };
        processLanguage = language;
        Application.quitting -= Shutdown;
        Application.quitting += Shutdown;
    }

    // Piper prints the path of each finished WAV file
    private static void OnOutput(string line)
    {
        if (line == null) return;

        Match match = OUTPUT_FILE_ID.Match(line);
        if (!match.Success) return;

        string path = line.Trim();
        try
        {
            byte[] wav = File.ReadAllBytes(path);
            File.Delete(path);
            TextToSpeech.DeliverSpeech(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), wav);
        }
        catch (Exception e)
        {
            ValkyrieDebug.Log("Warning: Unable to read synthesized speech: " + path + " Error: " + e.Message);
        }

        try
        {
            lock (requestLock)
            {
                if (input != null)
                {
                    SendNextRequest();
                }
            }
        }
        catch (Exception e)
        {
            ValkyrieDebug.Log("Warning: Piper text to speech failed: " + e.Message);
        }
    }

    private static void Shutdown()
    {
        lock (requestLock)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception e)
            {
                ValkyrieDebug.Log("Warning: Could not stop Piper process: " + e.Message);
            }
            process = null;
            input = null;
            processLanguage = null;
            pendingRequests.Clear();
            synthesizing = false;
        }
    }
}
