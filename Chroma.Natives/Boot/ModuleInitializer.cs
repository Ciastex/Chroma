﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Chroma.Natives.Boot.Config;
using Chroma.Natives.Boot.PlatformSpecific;
using Chroma.Natives.SDL;
using Chroma.Natives.Syscalls;

namespace Chroma.Natives.Boot
{
    internal static class ModuleInitializer
    {
        internal static IPlatform Platform { get; private set; }
        internal static BootConfig BootConfig { get; private set; }

        [ModuleInitializer]
        public static void Initialize()
        {
            if (!Environment.Is64BitOperatingSystem)
                throw new PlatformNotSupportedException("Chroma supports 64-bit systems only.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                SetupConsoleMode();

            ReadBootConfig();

            try
            {
                Console.WriteLine("Please wait. I'm trying to boot...");

                if (BootConfig.SkipChecksumVerification)
                    Console.WriteLine("Checksum verification disabled. Living on the edge, huh?");
                
                LoadNatives();
            }
            catch (NativeExtractorException nee)
            {
                Console.WriteLine($"{nee.Message}. Inner exception: {nee.InnerException}");
                Console.WriteLine("Press any key to terminate...");
                Console.ReadKey();
                
                Environment.Exit(1);
            }
            InitializeSdlSystems();
        }

        private static void SetupConsoleMode()
        {
            var stdHandle = Windows.GetStdHandle(Windows.STD_OUTPUT_HANDLE);

            Windows.GetConsoleMode(stdHandle, out var consoleMode);
            consoleMode |= Windows.ENABLE_PROCESSED_OUTPUT;
            consoleMode |= Windows.ENABLE_VIRTUAL_TERMINAL_PROCESSING;

            Windows.SetConsoleMode(stdHandle, consoleMode);
        }

        private static void ReadBootConfig()
        {
            var bootConfigPath = Path.Combine(
                AppContext.BaseDirectory,
                "boot.json"
            );

            try
            {
                using var sr = new StreamReader(bootConfigPath);
                BootConfig = JsonSerializer.Deserialize<BootConfig>(sr.ReadToEnd());
            }
            catch (Exception e)
            {
                Console.WriteLine($"No boot.json or it was invalid ({e.Message}) defaults created.");

                BootConfig = new BootConfig();

                using var sw = new StreamWriter(bootConfigPath);
                sw.WriteLine(
                    JsonSerializer.Serialize(BootConfig, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        IgnoreNullValues = true,
                        IgnoreReadOnlyProperties = false
                    })
                );
            }
        }

        private static void LoadNatives()
        {
            var libraryFileNames = NativeLibraryExtractor.ExtractAll()
                .Select(Path.GetFileName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Platform = new WindowsPlatform();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Platform = new LinuxPlatform();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("Warning! Your platform support is currently *untested*.\n" +
                                  "Things can (and probably will) explode!");
                
                Platform = new MacPlatform();
            }

            foreach (var libraryFileName in libraryFileNames)
            {
                Console.WriteLine($"Now loading: {libraryFileName}");
                Platform.Register(libraryFileName);
            }
        }

        private static void InitializeSdlSystems()
        {
            Console.WriteLine("---");
            
            Console.WriteLine("Initializing SDL2 core...");
            SDL2.SDL_Init(BootConfig.SdlModules.SdlInitFlags);
            
            if (BootConfig.EnableSdlGpuDebugging)
            {
                Console.WriteLine("Enabling SDL_gpu debugging...");
                SDL_gpu.GPU_SetDebugLevel(SDL_gpu.GPU_DebugLevelEnum.GPU_DEBUG_LEVEL_MAX);
            }
        }
    }
}