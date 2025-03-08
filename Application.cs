using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using SDL3;

namespace SDL3ImGui;

public class Application : IDisposable
{
    public readonly nint Window;
    public readonly nint Device;
    public readonly ImGuiSDL3 Platform;
    public readonly ImGuiSDL3Renderer Renderer;

    public bool IsRunning { get; private set; }
    private readonly Stopwatch timer = Stopwatch.StartNew();
    private TimeSpan time = TimeSpan.Zero;

    private nint _texture;
    private SDL.FRect _srcRect;
    private SDL.FRect _dstRect;
    private SDL.Rect _screenClipRect;

    public Application(string name, int width, int height)
    {
        if(!SDL.Init(SDL.InitFlags.Video))
            throw new Exception($"SDL_Init failed: {SDL.GetError()}");

        // Create window
        var windowFlags = SDL.WindowFlags.Resizable;
        Window = SDL.CreateWindow(name, width, height, windowFlags);
        if(Window == IntPtr.Zero)
            throw new Exception($"SDL_CreateWindow failed: {SDL.GetError()}");

        // Create Renderer
        Device = SDL.CreateRenderer(Window, null);
        if(Device == IntPtr.Zero)
            throw new Exception($"SDL_CreateRenderer failed: {SDL.GetError()}");

        // Enable VSync
        //SDL.SetRenderVSync(Device, 1);

        // Setup screen clip rect
        SetupScreenClipRect();

        // Create ImGui context
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        // Init platform and renderer
        Platform = new (Window, Device);
        Renderer = new (Device);
    }

    ~Application() => Dispose();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        IsRunning = false;
        ImGui.DestroyContext();
        Renderer.Dispose();
        SDL.DestroyWindow(Window);
        SDL.DestroyRenderer(Device);
        SDL.Quit();
    }

    public void Run()
    {
        SetupTexture();

        IsRunning = true;

        while(IsRunning)
        {
            ImGui.GetIO().DeltaTime = (float)(timer.Elapsed - time).TotalSeconds;
            time = timer.Elapsed;

            PollEvents();
            Update();
            Render();
        }
    }

    private void PollEvents()
    {
        if(ImGui.GetIO().WantTextInput && !SDL.TextInputActive(Window))
            SDL.StartTextInput(Window);
        else if(!ImGui.GetIO().WantTextInput && SDL.TextInputActive(Window))
            SDL.StopTextInput(Window);

        while(SDL.PollEvent(out var ev))
        {
            Platform.ProcessEvent(ev);

            switch((SDL.EventType)ev.Type)
            {
                case SDL.EventType.WindowCloseRequested:
                case SDL.EventType.Quit:
                    IsRunning = false;
                    break;
                case SDL.EventType.WindowResized:
                    SetupScreenClipRect();
                    break;
            }
        }
    }

    private void Update()
    {
        Platform.NewFrame();
        Renderer.NewFrame();
        ImGui.NewFrame();

        // Hello world window
        if(ImGui.Begin("Hello world"))
        {
            ImGui.Text("Hello from SDL3 & ImGui!");
            ImGui.Text($"Application running for {time.TotalSeconds:F2} seconds.");

            // Draw our texture in ImGui
            ImGui.Image(_texture, new Vector2(_srcRect.W, _srcRect.H));
        }
        ImGui.End();
        ImGui.ShowDebugLogWindow();
        ImGui.ShowDemoWindow();
        ImGui.ShowMetricsWindow();

        ImGui.EndFrame();
    }

    private void Render()
    {
        SDL.SetRenderDrawColor(Device, 25, 12, 20, 255);
        SDL.RenderClear(Device);

        // Reset the clip rect to the screen size
        SDL.SetRenderClipRect(Device, _screenClipRect);

        // Render our texture
        SDL.RenderTexture(Device, _texture, _srcRect, _dstRect);

        // Render ImGui
        ImGui.Render();
        Renderer.RenderDrawData(ImGui.GetDrawData());

        SDL.RenderPresent(Device);
    }

    private void SetupTexture()
    {
        _texture = Image.LoadTexture(Device, "pulsar4x-menu.png");
        SDL.GetTextureSize(_texture, out float w, out float h);
        _srcRect = new()
        {
            X = 0,
            Y = 0,
            W = w,
            H = h
        };

        _dstRect = new()
        {
            X = 0,
            Y = 0,
            W = w,
            H = h
        };
    }

    private void SetupScreenClipRect()
    {
        SDL.GetWindowSize(Window, out int w, out int h);
        _screenClipRect = new()
        {
            X = 0,
            Y = 0,
            W = w,
            H = h
        };
    }
}