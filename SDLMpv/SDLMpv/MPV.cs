using System;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using SDL2;

namespace SLDMpv
{
    public class MPV
    {
        private const int MpvFormatString = 1;
        private IntPtr _libMpvDll;
        private IntPtr _mpvHandle;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        internal static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr MpvCreate();
        private MpvCreate _mpvCreate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvInitialize(IntPtr mpvHandle);
        private MpvInitialize _mpvInitialize;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvCommand(IntPtr mpvHandle, IntPtr strings);
        private MpvCommand _mpvCommand;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvTerminateDestroy(IntPtr mpvHandle);
        private MpvTerminateDestroy _mpvTerminateDestroy;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetOption(IntPtr mpvHandle, byte[] name, int format, ref long data);
        private MpvSetOption _mpvSetOption;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetOptionString(IntPtr mpvHandle, byte[] name, byte[] value);
        private MpvSetOptionString _mpvSetOptionString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvGetPropertystring(IntPtr mpvHandle, byte[] name, int format, ref IntPtr data);
        private MpvGetPropertystring _mpvGetPropertyString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetProperty(IntPtr mpvHandle, byte[] name, int format, ref byte[] data);
        private MpvSetProperty _mpvSetProperty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvRenderContextCreate(ref IntPtr context, IntPtr mpvHandler, IntPtr parameters);
        private MpvRenderContextCreate _mpvRenderContextCreate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MpvFree(IntPtr data);
        private MpvFree _mpvFree;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr OpenGlRenderContextCallback(IntPtr ctx, IntPtr name);

        public enum MpvRenderParamType
        {
            Invalid = 0,
            ApiType = 1,
            InitParams = 2,
            Fbo = 3,
            FlipY = 4,
            Depth = 5,
            IccProfile = 6,
            AmbientLight = 7,
            X11Display = 8,
            WlDisplay = 9,
            AdvancedControl = 10,
            NextFrameInfo = 11,
            BlockForTargetTime = 12,
            SkipRendering = 13,
            DrmDisplay = 14,
            DrmDrawSurfaceSize = 15,
            DrmDisplayV2 = 15
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvRenderParam
        {
            public MpvRenderParamType type;
            public IntPtr data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvOpenGlInitParams
        {
            public OpenGlRenderContextCallback get_proc_address;
            public IntPtr get_proc_address_ctx;
            public IntPtr extra_exts;
        }

        private object GetDllType(Type type, string name)
        {
            IntPtr address = GetProcAddress(_libMpvDll, name);
            if (address != IntPtr.Zero)
                return Marshal.GetDelegateForFunctionPointer(address, type);
            return null;
        }

        private void LoadMpvDynamic()
        {
            _libMpvDll = LoadLibrary("mpv-1.dll"); // The dll is included in the DEV builds by lachs0r: https://mpv.srsfckn.biz/
            _mpvCreate = (MpvCreate)GetDllType(typeof(MpvCreate), "mpv_create");
            _mpvInitialize = (MpvInitialize)GetDllType(typeof(MpvInitialize), "mpv_initialize");
            _mpvRenderContextCreate = (MpvRenderContextCreate)GetDllType(typeof(MpvRenderContextCreate), "mpv_render_context_create");
            _mpvTerminateDestroy = (MpvTerminateDestroy)GetDllType(typeof(MpvTerminateDestroy), "mpv_terminate_destroy");
            _mpvCommand = (MpvCommand)GetDllType(typeof(MpvCommand), "mpv_command");
            _mpvSetOption = (MpvSetOption)GetDllType(typeof(MpvSetOption), "mpv_set_option");
            _mpvSetOptionString = (MpvSetOptionString)GetDllType(typeof(MpvSetOptionString), "mpv_set_option_string");
            _mpvGetPropertyString = (MpvGetPropertystring)GetDllType(typeof(MpvGetPropertystring), "mpv_get_property");
            _mpvSetProperty = (MpvSetProperty)GetDllType(typeof(MpvSetProperty), "mpv_set_property");
            _mpvFree = (MpvFree)GetDllType(typeof(MpvFree), "mpv_free");
        }

        public void Pause()
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            var bytes = GetUtf8Bytes("yes");
            _mpvSetProperty(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref bytes);
        }

        public void Play()
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            var bytes = GetUtf8Bytes("no");
            _mpvSetProperty(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref bytes);
        }

        public bool IsPaused()
        {
            if (_mpvHandle == IntPtr.Zero)
                return true;

            var lpBuffer = IntPtr.Zero;
            _mpvGetPropertyString(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref lpBuffer);
            var isPaused = Marshal.PtrToStringAnsi(lpBuffer) == "yes";
            _mpvFree(lpBuffer);
            return isPaused;
        }

        public void SetTime(double value)
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            DoMpvCommand("seek", value.ToString(CultureInfo.InvariantCulture), "absolute");
        }

        private static byte[] GetUtf8Bytes(string s)
        {
            return Encoding.UTF8.GetBytes(s + "\0");
        }

        public static IntPtr AllocateUtf8IntPtrArrayWithSentinel(string[] arr, out IntPtr[] byteArrayPointers)
        {
            int numberOfStrings = arr.Length + 1; // add extra element for extra null pointer last (sentinel)
            byteArrayPointers = new IntPtr[numberOfStrings];
            IntPtr rootPointer = Marshal.AllocCoTaskMem(IntPtr.Size * numberOfStrings);
            for (int index = 0; index < arr.Length; index++)
            {
                var bytes = GetUtf8Bytes(arr[index]);
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                byteArrayPointers[index] = unmanagedPointer;
            }
            Marshal.Copy(byteArrayPointers, 0, rootPointer, numberOfStrings);
            return rootPointer;
        }

        private void DoMpvCommand(params string[] args)
        {
            IntPtr[] byteArrayPointers;
            var mainPtr = AllocateUtf8IntPtrArrayWithSentinel(args, out byteArrayPointers);
            _mpvCommand(_mpvHandle, mainPtr);
            foreach (var ptr in byteArrayPointers)
            {
                Marshal.FreeHGlobal(ptr);
            }
            Marshal.FreeHGlobal(mainPtr);
        }

        public unsafe void Play(Int64 windowId)
        {
            if (_mpvHandle != IntPtr.Zero)
                _mpvTerminateDestroy(_mpvHandle);

            LoadMpvDynamic();
            if (_libMpvDll == IntPtr.Zero)
                return;

            _mpvHandle = _mpvCreate.Invoke();
            if (_mpvHandle == IntPtr.Zero)
                return;

            _mpvInitialize.Invoke(_mpvHandle);

            

            MpvOpenGlInitParams oglInitParams = new MpvOpenGlInitParams();
            oglInitParams.get_proc_address = (ctx, name) => SDL.SDL_GL_GetProcAddress(name);
            oglInitParams.get_proc_address_ctx = IntPtr.Zero;
            oglInitParams.extra_exts = IntPtr.Zero;

            var size = Marshal.SizeOf<MpvOpenGlInitParams>();
            var oglInitParamsBuf = new byte[size];

            fixed (byte* arrPtr = oglInitParamsBuf)
            {
                IntPtr oglInitParamsPtr = new IntPtr(arrPtr);
                Marshal.StructureToPtr(oglInitParams, oglInitParamsPtr, true);

                MpvRenderParam* parameters = stackalloc MpvRenderParam[3];

                parameters[0].type = MpvRenderParamType.ApiType;
                parameters[0].data = Marshal.StringToHGlobalAnsi("opengl");

                parameters[1].type = MpvRenderParamType.InitParams;
                parameters[1].data = oglInitParamsPtr;

                parameters[2].type = MpvRenderParamType.Invalid;
                parameters[2].data = IntPtr.Zero;

                var renderParamSize = Marshal.SizeOf<MpvRenderParam>();

                var paramBuf = new byte[renderParamSize * 3];
                fixed (byte* paramBufPtr = paramBuf)
                {
                    IntPtr param1Ptr = new IntPtr(paramBufPtr);
                    Marshal.StructureToPtr(parameters[0], param1Ptr, true);

                    IntPtr param2Ptr = new IntPtr(paramBufPtr + renderParamSize);
                    Marshal.StructureToPtr(parameters[1], param2Ptr, true);

                    IntPtr param3Ptr = new IntPtr(paramBufPtr + renderParamSize + renderParamSize);
                    Marshal.StructureToPtr(parameters[2], param3Ptr, true);


                    IntPtr context = new IntPtr(0);
                    _mpvRenderContextCreate(ref context, _mpvHandle, param1Ptr);
                }
            }


            //_mpvInitialize.Invoke(_mpvHandle);
            _mpvSetOptionString(_mpvHandle, GetUtf8Bytes("keep-open"), GetUtf8Bytes("always"));
            int mpvFormatInt64 = 4;
            //var windowId = pictureBox1.Handle.ToInt64();
            _mpvSetOption(_mpvHandle, GetUtf8Bytes("wid"), mpvFormatInt64, ref windowId);
            DoMpvCommand("loadfile", "drop.avi");
        }

        private void buttonPlayPause_Click(object sender, EventArgs e)
        {
            if (IsPaused())
                Play();
            else
                Pause();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Pause();
            SetTime(0);
        }

     
    }
}