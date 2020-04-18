﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Chroma.Natives.Syscalls;

namespace Chroma.Natives.Boot
{
    internal class NativeLibraryRegistry
    {
        private readonly List<string> _lookupPaths;
        private readonly Dictionary<string, NativeLibrary> _libRegistry;

        public NativeLibraryRegistry(List<string> lookupPaths)
        {
            _lookupPaths = new List<string>(lookupPaths);
            _libRegistry = new Dictionary<string, NativeLibrary>();
        }

        public NativeLibrary Register(string fileName)
        {
            var lookupStack = new Stack<string>(_lookupPaths);

            while (lookupStack.Count != 0)
            {
                var lookupDirectory = lookupStack.Pop();
                var libPath = Path.Combine(lookupDirectory, fileName);

                if (!File.Exists(libPath))
                    continue;

                var handle = RegisterPlatformSpecific(libPath, out NativeLibrary.SymbolLookupDelegate symbolLookup);
                var nativeInfo = new NativeLibrary(libPath, handle, symbolLookup);

                _libRegistry.Add(fileName, nativeInfo);
                return nativeInfo;
            }

            throw new NativeLoaderException($"Failed to find '{fileName}' at the provided lookup paths!");
        }

        public NativeLibrary TryRegister(params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                try
                {
                    return Register(fileName);
                }
                catch (NativeLoaderException)
                {
                    continue;
                }
            }

            throw new NativeLoaderException("Failed to find any provided file name variat at the provided lookup paths!");
        }

        public NativeLibrary Retrieve(string fileName)
        {
            if (!_libRegistry.ContainsKey(fileName))
                throw new NativeLoaderException($"Library file '{fileName}' was never registered.");

            return _libRegistry[fileName];
        }

        public NativeLibrary TryRetrieve(params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                try
                {
                    return Retrieve(fileName);
                }
                catch (NativeLoaderException)
                {
                    continue;
                }
            }
            
            throw new NativeLoaderException("None of the provided file names were ever registered.");
        }

        private IntPtr RegisterPlatformSpecific(string absoluteFilePath, out NativeLibrary.SymbolLookupDelegate symbolLookup)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var handle = Posix.dlopen(absoluteFilePath, Posix.RTLD_NOW);

                if (handle == IntPtr.Zero)
                    throw new NativeLoaderException($"Failed to load '{absoluteFilePath}'. dlerror: {Marshal.PtrToStringAnsi(Posix.dlerror())}");

                symbolLookup = Posix.dlsym;
                return handle;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var dllDirectory = Path.GetDirectoryName(absoluteFilePath);
                var fileName = Path.GetFileName(absoluteFilePath);

                Windows.SetDllDirectory(dllDirectory);
                var handle = Windows.LoadLibrary(fileName);

                if (handle == IntPtr.Zero)
                    throw new NativeLoaderException($"Failed to load '{absoluteFilePath}'. LoadLibrary: {Windows.GetLastError():X8}");

                symbolLookup = Windows.GetProcAddress;
                return handle;
            }
            else
            {
                throw new NativeLoaderException($"Platform '{Environment.OSVersion.Platform}' is not supported.");
            }
        }
    }
}