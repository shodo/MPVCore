using System;
using SDL2;
using SLDMpv;

namespace SDLMpv
{
    class Program
    {
        static void Main(string[] args)
        {
            SDL.SDL_SetHint(SDL.SDL_HINT_NO_SIGNAL_HANDLERS, "no");
            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1"); //Needed if launched from VisualStudio
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            var window = IntPtr.Zero;
            window = SDL.SDL_CreateWindow(".NET Core SDL2-CS Tutorial",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                1028,
                800,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE 
                | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL 
                | SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN
            );

            IntPtr glcontext = SDL.SDL_GL_CreateContext(window);

            SDL.SDL_GL_MakeCurrent(window, glcontext);

            var id = SDL.SDL_GetWindowID(window);
            MPV mpv = new MPV();
            mpv.Play(id);

            SDL.SDL_Event e;
            bool quit = false;
            while (!quit)
            {
                while (SDL.SDL_PollEvent(out e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            quit = true;
                            break;
                    }
                }
            }

            SDL.SDL_DestroyWindow(window);

            SDL.SDL_Quit();
        }
    }
}
