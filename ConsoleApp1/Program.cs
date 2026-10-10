using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace WukongBenchmarkRunner
{
    class Program
    {
        private const string GitHubRepositoryUrl = "https://github.com/skywalkerfk1/WukongBench";

        // Абсолютный путь к файлу GameUserSettings.ini
        private const string CustomConfigPath = @"Вставьте сюда путь к GameUserSettings.ini";

        // Абсолютный путь к папке с логами
        private const string CustomLogsPath = @"Вставьте сюда путь к папке с логами бенчмарка";

        private const string SteamAppId = "3132990";
        private const string ProcessName = "b1-win64-shipping";

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==================================================");
            Console.WriteLine("    Инструмент автоматизации бенчмарка Wukong     ");
            Console.WriteLine("==================================================");

            string configPath = CustomConfigPath;
            string logsPath = CustomLogsPath;

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"\n[Предупреждение] Не найден файл конфигурации по пути: {configPath}");
                Console.WriteLine("Убедитесь, что Benchmark Tool запускался хотя бы раз вручную для генерации конфигов.");
            }

            Console.WriteLine("\n--------------------------------------------------");
            Console.WriteLine("[1/2] Автоматический запуск CPU-теста...");
            ModifySettings(configPath, resolutionX: 1280, resolutionY: 720, qualityPreset: 0);

            RunBenchmarkAndClickStart();
            ParseAndPrintResults(logsPath, "CPU-тест");

            Console.WriteLine("\n--------------------------------------------------");
            Console.WriteLine("[2/2] Автоматический запуск GPU-теста...");
            ModifySettings(configPath, resolutionX: 2560, resolutionY: 1440, qualityPreset: 3);

            RunBenchmarkAndClickStart();
            ParseAndPrintResults(logsPath, "GPU-тест");

            Console.WriteLine("\n==================================================");
            Console.WriteLine("Все тесты успешно завершены в автоматическом режиме!");
            Console.WriteLine($"Ссылка на репозиторий: {(string.IsNullOrEmpty(GitHubRepositoryUrl) ? "[Не указана]" : GitHubRepositoryUrl)}");
            Console.WriteLine("Работа программы завершена. Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static void ModifySettings(string configPath, int resolutionX, int resolutionY, int qualityPreset)
        {
            if (!File.Exists(configPath)) return;

            try
            {
                string content = File.ReadAllText(configPath);

                content = UpdateIniValue(content, "ResolutionSizeX", resolutionX.ToString());
                content = UpdateIniValue(content, "ResolutionSizeY", resolutionY.ToString());
                content = UpdateIniValue(content, "LastUserConfirmedResolutionSizeX", resolutionX.ToString());
                content = UpdateIniValue(content, "LastUserConfirmedResolutionSizeY", resolutionY.ToString());

                content = UpdateIniValue(content, "sg.ResolutionQuality", qualityPreset == 0 ? "50" : "100");
                content = UpdateIniValue(content, "sg.ViewDistanceQuality", qualityPreset.ToString());
                content = UpdateIniValue(content, "sg.AntiAliasingQuality", qualityPreset.ToString());
                content = UpdateIniValue(content, "sg.ShadowQuality", qualityPreset.ToString());
                content = UpdateIniValue(content, "sg.PostProcessQuality", qualityPreset.ToString());
                content = UpdateIniValue(content, "sg.TextureQuality", qualityPreset.ToString());
                content = UpdateIniValue(content, "sg.EffectsQuality", qualityPreset.ToString());
                content = UpdateIniValue(content, "sg.FoliageQuality", qualityPreset.ToString());
                content = UpdateIniValue(content, "sg.ShadingQuality", qualityPreset.ToString());

                File.WriteAllText(configPath, content);
                Console.WriteLine("[Конфиг] Настройки успешно записаны в GameUserSettings.ini");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Ошибка] Не удалось изменить конфиг: {ex.Message}");
            }
        }

        static string UpdateIniValue(string content, string key, string value)
        {
            string pattern = $@"{key}\s*=\s*.*";
            string replacement = $"{key}={value}";
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                return Regex.Replace(content, pattern, replacement, RegexOptions.IgnoreCase);
            }
            else
            {
                return content + Environment.NewLine + replacement;
            }
        }

        static void RunBenchmarkAndClickStart()
        {
            try
            {
                Console.WriteLine("[Steam] Запуск приложения бенчмарка...");
                var psi = new ProcessStartInfo
                {
                    FileName = $"steam://run/{SteamAppId}",
                    UseShellExecute = true
                };
                Process.Start(psi);

                Console.WriteLine("[Процесс] Ожидание запуска игрового движка...");
                Process[] processes;
                int attempts = 0;
                do
                {
                    Thread.Sleep(3000);
                    processes = Process.GetProcessesByName(ProcessName);
                    attempts++;
                    if (attempts > 20)
                    {
                        Console.WriteLine("[Предупреждение] Процесс долго не появляется. Убедитесь, что Steam запущен.");
                        attempts = 0;
                    }
                }
                while (processes.Length == 0);

                Console.WriteLine("[Процесс] Процесс найден. Ожидание 30 секунд перед первым кликом...");
                Thread.Sleep(30000); 

                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                int click1X = screenWidth / 2;
                int click1Y = (int)(screenHeight * 0.55);

                SetCursorPos(click1X, click1Y);
                Thread.Sleep(500);

                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(100);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                Console.WriteLine("[Процесс] Первый клик выполнен.");
                Thread.Sleep(3000); 


                int click2X = (int)(screenWidth * 0.15);
                int click2Y = (int)(screenHeight * 0.45);

                SetCursorPos(click2X, click2Y);
                Thread.Sleep(500);

                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(100);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Console.WriteLine("[Процесс] Второй клик выполнен.");
                Thread.Sleep(3000);

                int click3X = (int)(screenWidth * 0.30);
                int click3Y = (int)(screenHeight * 0.60);

                SetCursorPos(click3X, click3Y);
                Thread.Sleep(500);

                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(100);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Console.WriteLine("[Процесс] Третий клик выполнен. Запуск теста, ожидание (до 5 минут)...");
                // Ожидание работы бенчмарка (максимум 5 минут или досрочное завершение)
                int maxWaitTimeMs = 5 * 60 * 1000;
                int elapsed = 0;
                bool exited = false;

                while (elapsed < maxWaitTimeMs)
                {
                    bool stillRunning = false;
                    foreach (var p in Process.GetProcessesByName(ProcessName))
                    {
                        if (!p.HasExited)
                        {
                            stillRunning = true;
                            break;
                        }
                    }

                    if (!stillRunning)
                    {
                        exited = true;
                        break;
                    }

                    Thread.Sleep(2000);
                    elapsed += 2000;
                }

                if (!exited)
                {
                    Console.WriteLine("[Процесс] Время ожидания (5 минут) истекло. Принудительное закрытие приложения...");
                    foreach (var p in Process.GetProcessesByName(ProcessName))
                    {
                        try
                        {
                            if (!p.HasExited)
                            {
                                p.Kill();
                                p.WaitForExit(2000);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Предупреждение] Не удалось закрыть процесс: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[Процесс] Бенчмарк успешно завершил работу самостоятельно.");
                }

                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Ошибка] Ошибка в автоматическом цикле: {ex.Message}");
            }
        }

        static void ParseAndPrintResults(string logsPath, string testName)
        {
            Console.WriteLine($"\n--- Результаты: {testName} ---");
            try
            {
                if (!Directory.Exists(logsPath))
                {
                    Console.WriteLine($"Папка с логами не найдена: {logsPath}");
                    return;
                }

                var directoryInfo = new DirectoryInfo(logsPath);
                FileInfo latestFile = null;

                foreach (var file in directoryInfo.GetFiles("*"))
                {
                    if (latestFile == null || file.LastWriteTime > latestFile.LastWriteTime)
                    {
                        latestFile = file;
                    }
                }

                if (latestFile != null)
                {
                    Console.WriteLine($"Анализ файла отчета: {latestFile.Name}");
                    string logContent = File.ReadAllText(latestFile.FullName);

                    Match cpuMatch = Regex.Match(logContent, @"""CPUModel""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    Match gpuMatch = Regex.Match(logContent, @"""GPUModel""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    Match ramMatch = Regex.Match(logContent, @"""SysMem""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);

                    if (cpuMatch.Success) Console.WriteLine($"* CPU: {cpuMatch.Groups[1].Value.Trim()}");
                    if (gpuMatch.Success) Console.WriteLine($"* GPU: {gpuMatch.Groups[1].Value.Trim()}");
                    if (ramMatch.Success) Console.WriteLine($"* RAM: {ramMatch.Groups[1].Value.Trim()}");

                    Match avgFpsMatch = Regex.Match(logContent, @"""FPSAvg""\s*:\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase);
                    Match minFpsMatch = Regex.Match(logContent, @"""FPSMin""\s*:\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase);

                    if (avgFpsMatch.Success)
                    {
                        Console.WriteLine($"* Средний FPS: {avgFpsMatch.Groups[1].Value}");
                    }


                    if (minFpsMatch.Success)
                    {
                        Console.WriteLine($"* Минимальный FPS: {minFpsMatch.Groups[1].Value}");
                    }
                }
                else
                {
                    Console.WriteLine("Файлы результатов не обнаружены в директории.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге результатов: {ex.Message}");
            }
        }
    }
}
