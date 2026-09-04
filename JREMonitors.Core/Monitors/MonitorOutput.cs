using System;
using System.Collections.Generic;
using System.Drawing;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Lighting;
using JREMonitors.Core.Managers;
using JREMonitors.Core.Utils.Render;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace JREMonitors.Core.Monitors
{
    public class MonitorOutput : IDisposable
    {
        public const float DefaultGhostingDecayTimeSeconds = 0.1f;
        private readonly ID3D11Device _d3D11Device;

        private readonly ID2D1DeviceContext _dc;
        private readonly DisposableStack _disposableStack = new DisposableStack();

        private readonly Dictionary<Screen, ScreenBackgroundBuffer> _screenBuffers =
            new Dictionary<Screen, ScreenBackgroundBuffer>();

        private ID3D11Texture2D _directD3D11Texture;
        private float _freezeTime;
        private ID3D11Texture2D[] _stateTextures;

        public MonitorOutput(
            Size size,
            ID2D1DeviceContext dc,
            ID3D11Device d3D11Device,
            float ghostingDecayTimeSeconds = DefaultGhostingDecayTimeSeconds
        )
        {
            Size = size;
            _dc = dc;
            _d3D11Device = d3D11Device;
            GhostingDecayTimeSeconds = ghostingDecayTimeSeconds;
            PureColorBackgroundBitmap = dc.CreateBitmap(Size, IntPtr.Zero, 0, RenderHelper.BitMapProperties8Bit);
            _disposableStack.AddResource(PureColorBackgroundBitmap);
            DelayedBitmap = dc.CreateBitmap(Size, IntPtr.Zero, 0, RenderHelper.BitMapProperties8Bit);
            _disposableStack.AddResource(DelayedBitmap);
            dc.BeginDraw();
            dc.Target = PureColorBackgroundBitmap;
            dc.Clear(Colors.Black);
            dc.Target = DelayedBitmap;
            dc.Clear(null);
            if (HasGhosting)
            {
                ActualBitmap = dc.CreateBitmap(Size, IntPtr.Zero, 0, RenderHelper.BitMapProperties8Bit);
                _disposableStack.AddResource(ActualBitmap);
                CreateStateResources();
                for (var i = 0; i < 2; i++)
                {
                    dc.Target = GhostingBitmaps[i];
                    dc.Clear(Colors.Black);
                }

                dc.Target = ActualBitmap;
            }
            else
            {
                CreateDirectFinalResources();
                dc.Target = FinalBitmap;
            }

            dc.Clear(Colors.Black);
            dc.Target = null;
            dc.EndDraw();
        }

        public Size Size { get; }
        public int Width => Size.Width;
        public int Height => Size.Height;
        public ID2D1Bitmap1 MonitorRenderTargetBitmap => HasGhosting ? ActualBitmap : FinalBitmap;
        public ID2D1Bitmap1 OutputBitmap => HasGhosting ? GhostingBitmaps[CurrentStateIndex] : FinalBitmap;

        public ID3D11Texture2D OutputD3D11Texture =>
            HasGhosting ? _stateTextures[CurrentStateIndex] : _directD3D11Texture;

        private ID2D1Bitmap1 ActualBitmap { get; set; }
        public ID2D1Bitmap1 DelayedBitmap { get; }
        private ID2D1Bitmap1 FinalBitmap { get; set; }
        private ID2D1Bitmap1[] GhostingBitmaps { get; set; }
        public GhostingEffect GhostingEffect { get; private set; }
        private int CurrentStateIndex { get; set; }

        public ID2D1Bitmap1 PendingStateBitmap => GhostingBitmaps[1 - CurrentStateIndex];

        public float GhostingDecayTimeSeconds { get; private set; }
        public ID2D1Bitmap1 PureColorBackgroundBitmap { get; }

        public bool HasGhosting => GhostingDecayTimeSeconds > 0;

        public bool Frozen { get; private set; }
        public bool JustFrozen { get; private set; }

        public void Dispose()
        {
            if (_dc != null) _dc.Target = null;

            DisposeStateResources();
            DisposeDirectFinalResources();
            _disposableStack.Dispose();
            _screenBuffers.Clear();
        }

        public Color4? LastBackgroundColor { get; set; }

        public void CommitStateSwap()
        {
            CurrentStateIndex = 1 - CurrentStateIndex;
        }

        private void CreateStateResources()
        {
            _stateTextures = new ID3D11Texture2D[2];
            GhostingBitmaps = new ID2D1Bitmap1[2];
            for (var i = 0; i < 2; i++)
            {
                _stateTextures[i] =
                    _d3D11Device.CreateTexture2D(RenderHelper.CreateRenderTargetTextureDescription(Width, Height));
                using (var dxgiSurface = _stateTextures[i].QueryInterface<IDXGISurface>())
                {
                    GhostingBitmaps[i] =
                        _dc.CreateBitmapFromDxgiSurface(dxgiSurface, RenderHelper.BitMapProperties8Bit);
                }
            }

            GhostingEffect = new GhostingEffect(_dc);
            CurrentStateIndex = 0;
        }

        private void DisposeStateResources()
        {
            if (GhostingEffect != null)
            {
                GhostingEffect.SetInput(0, null, true);
                GhostingEffect.SetInput(1, null, true);
                GhostingEffect.Dispose();
                GhostingEffect = null;
            }

            if (GhostingBitmaps != null)
            {
                GhostingBitmaps[0]?.Dispose();
                GhostingBitmaps[1]?.Dispose();
                GhostingBitmaps = null;
            }

            if (_stateTextures != null)
            {
                _stateTextures[0]?.Dispose();
                _stateTextures[1]?.Dispose();
                _stateTextures = null;
            }
        }

        private void CreateDirectFinalResources()
        {
            _directD3D11Texture =
                _d3D11Device.CreateTexture2D(RenderHelper.CreateRenderTargetTextureDescription(Width, Height));
            using (var dxgiSurface = _directD3D11Texture.QueryInterface<IDXGISurface>())
            {
                FinalBitmap = _dc.CreateBitmapFromDxgiSurface(dxgiSurface, RenderHelper.BitMapProperties8Bit);
            }
        }

        private void DisposeDirectFinalResources()
        {
            if (FinalBitmap != null)
            {
                FinalBitmap.Dispose();
                FinalBitmap = null;
            }

            _directD3D11Texture?.Dispose();
            _directD3D11Texture = null;
        }

        public void UpdateFreezeState(bool updated, TimeSpan elapsed)
        {
            if (updated)
            {
                _freezeTime = 0;
                Frozen = false;
            }
            else
            {
                _freezeTime += (float)elapsed.TotalSeconds;
                if (_freezeTime >= GhostingDecayTimeSeconds)
                {
                    JustFrozen = !Frozen;
                    Frozen = true;
                }
                else
                {
                    Frozen = false;
                    JustFrozen = false;
                }
            }
        }

        public void ResetFreezeState()
        {
            _freezeTime = 0;
            Frozen = false;
            JustFrozen = false;
        }

        public void ReconfigureGhosting(float newDecayTimeSeconds)
        {
            if (Math.Abs(newDecayTimeSeconds - GhostingDecayTimeSeconds) < Epsilons.FloatEpsilon) return;
            var hasGhosting = HasGhosting;
            GhostingDecayTimeSeconds = newDecayTimeSeconds;
            var isGhosting = HasGhosting;
            _dc.Target = null;

            if (hasGhosting && !isGhosting)
            {
                DisposeStateResources();
                if (ActualBitmap != null)
                {
                    _disposableStack.RemoveResource(ActualBitmap);
                    ActualBitmap.Dispose();
                    ActualBitmap = null;
                }

                CreateDirectFinalResources();
                _dc.BeginDraw();
                _dc.Target = FinalBitmap;
                _dc.Clear(Colors.Black);
                _dc.Target = null;
                _dc.EndDraw();
            }
            else if (!hasGhosting && isGhosting)
            {
                DisposeDirectFinalResources();
                ActualBitmap = _dc.CreateBitmap(Size, IntPtr.Zero, 0, RenderHelper.BitMapProperties8Bit);
                _disposableStack.AddResource(ActualBitmap);
                CreateStateResources();
                _dc.BeginDraw();
                for (var i = 0; i < 2; i++)
                {
                    _dc.Target = GhostingBitmaps[i];
                    _dc.Clear(Colors.Black);
                }

                _dc.Target = ActualBitmap;
                _dc.Clear(Colors.Black);
                _dc.Target = null;
                _dc.EndDraw();
            }

            ResetFreezeState();
        }

        public ScreenBackgroundBuffer GetOrCreateBuffer(Screen screen, ID2D1DeviceContext dc)
        {
            if (_screenBuffers.TryGetValue(screen, out var buffer)) return buffer;
            buffer = new ScreenBackgroundBuffer(Size, dc);
            _screenBuffers.Add(screen, buffer);
            _disposableStack.AddResource(buffer);
            return buffer;
        }

        public void ResetBuffers()
        {
            foreach (var buffer in _screenBuffers.Values) buffer.HasBackgroundRendered = false;
        }

        public void ClearActiveScreen(ID2D1DeviceContext dc)
        {
            dc.Target = MonitorRenderTargetBitmap;
            dc.Clear(Colors.Black);
            dc.Target = DelayedBitmap;
            dc.Clear(null);
            dc.Target = null;
        }
    }
}