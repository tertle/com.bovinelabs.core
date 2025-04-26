// <copyright file="LibraryLoader.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Class implementing a library loader for Unity.
    /// Adopted from LLMUnity:
    /// https://github.com/undreamai/LLMUnity/blob/b64c24566fb8ec17bfb426cb5e4728393af0e9b3/Runtime/LLMLib.cs
    /// Which was originally adapted from SkiaForUnity:
    /// https://github.com/ammariqais/SkiaForUnity/blob/f43322218c736d1c41f3a3df9355b90db4259a07/SkiaUnity/Assets/SkiaSharp/SkiaSharp-Bindings/SkiaSharp.HarfBuzz.Shared/HarfBuzzSharp.Shared/LibraryLoader.cs
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Platform specific")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Platform specific")]
    public static class LibraryLoader
    {
        /// <summary> Allows to retrieve a function delegate for the library. </summary>
        /// <typeparam name="T">type to cast the function.</typeparam>
        /// <param name="library">library handle.</param>
        /// <param name="name">function name.</param>
        /// <returns>function delegate.</returns>
        public static T GetSymbolDelegate<T>(IntPtr library, string name)
            where T : Delegate
        {
            var symbol = GetSymbol(library, name);
            if (symbol == IntPtr.Zero)
            {
                throw new EntryPointNotFoundException($"Unable to load symbol '{name}'.");
            }

            return Marshal.GetDelegateForFunctionPointer<T>(symbol);
        }

        /// <summary> Loads the provided library in a cross-platform manner. </summary>
        /// <param name="libraryName">library path.</param>
        /// <returns>library handle.</returns>
        public static IntPtr LoadLibrary(string libraryName)
        {
            if (string.IsNullOrEmpty(libraryName))
            {
                throw new ArgumentNullException(nameof(libraryName));
            }

            IntPtr handle;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
            {
                handle = Win32.LoadLibrary(libraryName);
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
            {
                handle = Linux.dlopen(libraryName);
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer)
            {
                handle = Mac.dlopen(libraryName);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                handle = Android.dlopen(libraryName);
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                handle = iOS.dlopen(libraryName);
            }
            else
            {
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to load library '{libraryName}'.");
            }

            return handle;
        }

        /// <summary> Retrieve a function delegate for the library in a cross-platform manner. </summary>
        /// <param name="library">library handle.</param>
        /// <param name="symbolName">function name.</param>
        /// <returns>function handle.</returns>
        public static IntPtr GetSymbol(IntPtr library, string symbolName)
        {
            if (string.IsNullOrEmpty(symbolName))
            {
                throw new ArgumentNullException(nameof(symbolName));
            }

            IntPtr handle;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
            {
                handle = Win32.GetProcAddress(library, symbolName);
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
            {
                handle = Linux.dlsym(library, symbolName);
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer)
            {
                handle = Mac.dlsym(library, symbolName);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                handle = Android.dlsym(library, symbolName);
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                handle = iOS.dlsym(library, symbolName);
            }
            else
            {
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to load symbol '{symbolName}' from library {library}.");
            }

            if (handle == IntPtr.Zero)
            {
                throw new EntryPointNotFoundException($"Unable to load symbol '{symbolName}'.");
            }

            return handle;
        }

        /// <summary> Frees up the library. </summary>
        /// <param name="library">library handle.</param>
        public static void FreeLibrary(IntPtr library)
        {
            if (library == IntPtr.Zero)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer)
            {
                Win32.FreeLibrary(library);
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer)
            {
                Linux.dlclose(library);
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer)
            {
                Mac.dlclose(library);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                Android.dlclose(library);
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                iOS.dlclose(library);
            }
            else
            {
                throw new PlatformNotSupportedException($"Current platform is unknown, unable to close library '{library}'.");
            }
        }

        private static class Mac
        {
            private const string SystemLibrary = "/usr/lib/libSystem.dylib";

            private const int RTLD_LAZY = 1;
            private const int RTLD_NOW = 2;

            public static IntPtr dlopen(string path, bool lazy = true) =>
                dlopen(path, lazy ? RTLD_LAZY : RTLD_NOW);

            [DllImport(SystemLibrary)]
            public static extern IntPtr dlopen(string path, int mode);

            [DllImport(SystemLibrary)]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport(SystemLibrary)]
            public static extern void dlclose(IntPtr handle);
        }

        private static class Linux
        {
            private const string SystemLibrary = "libdl.so";
            private const string SystemLibrary2 = "libdl.so.2"; // newer Linux distros use this

            private const int RTLD_LAZY = 1;
            private const int RTLD_NOW = 2;

            private static bool useSystemLibrary2 = true;

            public static IntPtr dlopen(string path, bool lazy = true)
            {
                try
                {
                    return dlopen2(path, lazy ? RTLD_LAZY : RTLD_NOW);
                }
                catch (DllNotFoundException)
                {
                    useSystemLibrary2 = false;
                    return dlopen1(path, lazy ? RTLD_LAZY : RTLD_NOW);
                }
            }

            public static IntPtr dlsym(IntPtr handle, string symbol)
            {
                return useSystemLibrary2 ? dlsym2(handle, symbol) : dlsym1(handle, symbol);
            }

            public static void dlclose(IntPtr handle)
            {
                if (useSystemLibrary2)
                {
                    dlclose2(handle);
                }
                else
                {
                    dlclose1(handle);
                }
            }

            [DllImport(SystemLibrary, EntryPoint = "dlopen")]
            private static extern IntPtr dlopen1(string path, int mode);

            [DllImport(SystemLibrary, EntryPoint = "dlsym")]
            private static extern IntPtr dlsym1(IntPtr handle, string symbol);

            [DllImport(SystemLibrary, EntryPoint = "dlclose")]
            private static extern void dlclose1(IntPtr handle);

            [DllImport(SystemLibrary2, EntryPoint = "dlopen")]
            private static extern IntPtr dlopen2(string path, int mode);

            [DllImport(SystemLibrary2, EntryPoint = "dlsym")]
            private static extern IntPtr dlsym2(IntPtr handle, string symbol);

            [DllImport(SystemLibrary2, EntryPoint = "dlclose")]
            private static extern void dlclose2(IntPtr handle);
        }

        private static class Win32
        {
            private const string SystemLibrary = "Kernel32.dll";

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

            [DllImport(SystemLibrary, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern void FreeLibrary(IntPtr hModule);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Conditional")]
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local", Justification = "Conditional")]
        private static class Android
        {
            public static IntPtr dlopen(string path) => dlopen(path, 1);

#if UNITY_ANDROID
            [DllImport("__Internal")]
            public static extern IntPtr dlopen(string filename, int flags);

            [DllImport("__Internal")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("__Internal")]
            public static extern int dlclose(IntPtr handle);
#else
            public static IntPtr dlopen(string filename, int flags)
            {
                return default;
            }

            public static IntPtr dlsym(IntPtr handle, string symbol)
            {
                return default;
            }

            public static int dlclose(IntPtr handle)
            {
                return 0;
            }

#endif
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Conditional")]
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local", Justification = "Conditional")]
        private static class iOS
        {
            public static IntPtr dlopen(string path) => dlopen(path, 1);

#if UNITY_IOS
            // LoadLibrary for iOS
            [DllImport("__Internal")]
            public static extern IntPtr dlopen(string filename, int flags);

            // GetSymbol for iOS
            [DllImport("__Internal")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            // FreeLibrary for iOS
            [DllImport("__Internal")]
            public static extern int dlclose(IntPtr handle);
#else
            public static IntPtr dlopen(string filename, int flags)
            {
                return default;
            }

            public static IntPtr dlsym(IntPtr handle, string symbol)
            {
                return default;
            }

            public static int dlclose(IntPtr handle)
            {
                return 0;
            }
#endif
        }
    }

}
