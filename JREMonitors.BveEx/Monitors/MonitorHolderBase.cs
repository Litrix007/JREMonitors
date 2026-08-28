using System;
using System.Drawing;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using BveEx.Extensions.ContextMenuHacker;
using BveEx.PluginHost;
using BveTypes.ClassWrappers;
using JREMonitors.BveEx.Utils;
using JREMonitors.Core.Constants;
using JREMonitors.Core.Contexts;
using JREMonitors.Core.Debugger;
using JREMonitors.Core.Lighting;
using JREMonitors.Core.Monitors;
using JREMonitors.Core.Providers;
using JREMonitors.Core.State;
using JREMonitors.Core.Utils.Render;
using SlimDX.Direct3D9;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using Monitor = JREMonitors.Core.Monitors.Monitor;
using Vector2 = System.Numerics.Vector2;
using Vector3 = SlimDX.Vector3;
using Vector4 = System.Numerics.Vector4;
using Matrix = SlimDX.Matrix;

namespace JREMonitors.BveEx.Monitors
{
    public abstract class MonitorHolderBase : IDisposable
    {
        private const int MaxTextureSize = 4096;
        private const float SafeMargin = 4;
        public const string MenuTagPrefix = "JREMonitors:ContextMenu:";
        private readonly Action<MonitorHolderBase, Vector2> _onExternalClickCallback;
        private readonly SynchronizationContext _syncContext;
        protected readonly IBveHacker BveHacker;
        protected readonly MonitorContext Context;
        protected readonly DataHub DataHub;
        protected readonly IDebugger Debugger;
        private ID3D11Texture2D _extSharedD3D11Texture;
        private Matrix4x4 _hTotal;
        private float _lastBrightness = float.NaN;
        private float _lastEAmbient = float.NaN;

        private float _lastExternalFormBrightness = -1f;
        private LightingProperties _lighting;
        private TimeSpan _pendingElapsed;
        private Scenario _scenario;
        private bool _showTextureBoundsRect;
        private bool _showUiDebugRect;
        private Needle _vehiclePanelElement;

        /// <summary>
        ///     本帧"是否需要刷新（Render/Submit/Sync）"的集中判定结果，由 ComputeFrameActions 每帧重算。
        /// </summary>
        protected FrameActions Actions;

        protected AdaptiveScreenLightingEffect AdaptiveScreenLightingEffect;
        protected bool Attached;

        protected int BufferFrameCount;
        protected float CabBorderRadius;
        protected ID2D1Bitmap CabD2D1TransformBitmap;
        protected ID3D11Texture2D CabD3D11Texture;
        protected Color4 ClearColor;
        protected Matrix4x4? ClickMatrix;
        protected bool D3D9TextureRecreated;
        protected Model DaytimeModel;
        protected ExternalDisplayForm ExternalForm;
        protected bool ExtFormNeedsSync;
        protected bool HasExternalFormShown;
        protected bool HasSyncedExternalThisFrame;

        /// <summary>
        ///     投影参数变更标记。Cab 几何/光照配置/ShowCabTextureBoundsRect 变更后置 true，
        ///     强制下一帧 RenderCabProjection 重渲（即便画面静止/frozen）。
        /// </summary>
        protected bool IsCabProjectionDirty;

        protected ID2D1Effect ProjectEffect;
        protected bool Submitted;
        protected int TexHeight;

        protected int TexWidth;

        protected MonitorHolderBase(
            DataHub dataHub,
            MonitorContext context,
            MonitorProperties properties,
            ITimeProvider timeProvider,
            bool showTextureBoundsRect,
            Action<MonitorHolderBase, Vector2> onExternalClickCallback,
            int bufferFrameCount = 1
        )
        {
            Properties = properties;
            DataHub = dataHub;
            Debugger = dataHub.GetOrNull<IDebugger>();
            BveHacker = dataHub.Get<IBveHacker>();
            Context = context;
            _showTextureBoundsRect = showTextureBoundsRect;
            ClearColor = new Color4(1, 0, 0, _showTextureBoundsRect ? 1 : 0);
            _onExternalClickCallback = onExternalClickCallback;
            _syncContext = SynchronizationContext.Current;
            BufferFrameCount = bufferFrameCount;
            _showUiDebugRect = properties.ShowDebugRect;
            var srcWidth = properties.Size.Width;
            var srcHeight = properties.Size.Height;
            TexWidth = srcWidth;
            TexHeight = srcHeight;
            var isCabEnabled = properties.Cab.Enabled;
            if (isCabEnabled)
            {
                ProjectEffect = new ID2D1Effect(context.D2D1Context.CreateEffect(EffectGuids.Transform3D));
                ProjectEffect.SetValue((int)Transform3DProperties.BorderMode, BorderMode.Soft);
                ProjectEffect.SetValue((int)Transform3DProperties.InterpolationMode,
                    Transform3DInterpolationMode.Linear);
            }

            UpdateClickMatrix(properties);

            Monitor = new Monitor(
                Properties.Id,
                DataHub,
                Context,
                Properties.ScreenFactory,
                Properties.InitialScreenId,
                () => _showUiDebugRect
            );
            MonitorGameOutput = new MonitorOutput(
                properties.Size,
                context.D2D1Context,
                context.D3D11Device,
                properties.GhostingDecayTimeSeconds
            );
            Monitor.AddOutput(MonitorGameOutput);

            if (isCabEnabled) EnsureCabResources(properties);

            var contextMenuHacker = dataHub.Get<IContextMenuHacker>();
            ContextMenuCheckbox = contextMenuHacker.AddCheckableMenuItem($"Show {properties.Id}", OnCheckedChanged,
                ContextMenuItemType.Plugins);
            ContextMenuCheckbox.Tag = MenuTagPrefix + properties.Id;
            ContextMenuCheckbox.Checked = properties.External.Show;
            if (properties.External.Show) EnsureExternalForm();
        }

        public MonitorProperties Properties { get; private set; }

        public Texture D3D9MergedTexture { get; protected set; }
        public Monitor Monitor { get; }
        public MonitorOutput MonitorGameOutput { get; private set; }
        public ToolStripMenuItem ContextMenuCheckbox { get; }

        protected bool Frozen => !MonitorGameOutput.JustFrozen && MonitorGameOutput.Frozen;

        public virtual void Dispose()
        {
            if (Context.D2D1Context != null) Context.D2D1Context.Target = null;

            if (ContextMenuCheckbox != null)
            {
                ContextMenuCheckbox.CheckedChanged -= OnCheckedChanged;
                ContextMenuCheckbox.Dispose();
            }

            DisposeExternalForm();
            DisposeD3D9Texture();
            _extSharedD3D11Texture?.Dispose();
            _extSharedD3D11Texture = null;
            if (ProjectEffect != null)
            {
                ProjectEffect.SetInput(0, null, true);
                ProjectEffect.Dispose();
                ProjectEffect = null;
            }

            if (AdaptiveScreenLightingEffect != null)
            {
                AdaptiveScreenLightingEffect.SetInput(0, null, true);
                AdaptiveScreenLightingEffect.Dispose();
                AdaptiveScreenLightingEffect = null;
            }

            DisposeCabTextures();
            DaytimeModel?.Dispose();
            DaytimeModel = null;
            DisposeVehiclePanelElement();
            Monitor.Dispose();
            MonitorGameOutput = null;
        }

        protected virtual void DisposeCabTextures()
        {
            CabD2D1TransformBitmap?.Dispose();
            CabD2D1TransformBitmap = null;
            CabD3D11Texture?.Dispose();
            CabD3D11Texture = null;
        }

        protected virtual void EnsureCabResources(MonitorProperties properties)
        {
            CabBorderRadius = properties.Cab.BorderRadius;
            _lighting = properties.Lighting;
            if (AdaptiveScreenLightingEffect == null)
                AdaptiveScreenLightingEffect = new AdaptiveScreenLightingEffect(Context.D2D1Context);
        }

        /// <summary>
        ///     销毁 cab 投影的 Lighting 资源（Cab.Enabled true→false 时调用）。
        ///     保留 ProjectEffect 实例以便再次启用时复用。COM 生命周期：先从 ProjectEffect 解绑再 Dispose。
        /// </summary>
        protected virtual void DisposeCabResources()
        {
            ProjectEffect?.SetInput(0, null, true);
            AdaptiveScreenLightingEffect?.Dispose();
            AdaptiveScreenLightingEffect = null;
            CabBorderRadius = 0;
        }

        private void EnsureExternalForm()
        {
            if (ExternalForm != null) return;
            _extSharedD3D11Texture = Context.D3D11Device.CreateTexture2D(
                RenderHelper.CreateRenderTargetTextureDescription(Properties.Size.Width, Properties.Size.Height));
            var isFullScreen = !Direct3DProvider.Instance.PresentParameters.Windowed;
            ExternalForm = new ExternalDisplayForm(Context, Properties.RecommendedSizes, Properties.Size, isFullScreen,
                BveHacker.MainFormHandle,
                Properties.ExternalTitle, Properties.External.DisplayMode);
            ExternalForm.SetSharedTexture(_extSharedD3D11Texture);
            ExternalForm.OnLeftClick += OnExternalFormLeftClick;
            ExternalForm.Hidden += OnExternalFormHidden;
            ExternalForm.Start();
            HasExternalFormShown = false;
        }

        private void OnCheckedChanged(object sender, EventArgs e)
        {
            if (ContextMenuCheckbox.Checked)
            {
                EnsureExternalForm();
                ExtFormNeedsSync = true;
                ExternalForm.Show();
            }
            else
            {
                ExternalForm?.Hide();
            }
        }

        protected void OnExternalFormLeftClick(object sender, MouseEventArgs e)
        {
            var mapped = ExternalDisplayForm.MapClickPosition(
                Properties.External.DisplayMode,
                e.X, e.Y,
                ExternalForm.ClientSize.Width, ExternalForm.ClientSize.Height,
                Properties.Size.Width, Properties.Size.Height);

            if (mapped.HasValue)
                _onExternalClickCallback?.Invoke(this, mapped.Value);
        }

        private void OnExternalFormHidden(object sender, EventArgs e)
        {
            _syncContext.Post(_ =>
            {
                if (ContextMenuCheckbox != null && ContextMenuCheckbox.Checked)
                    ContextMenuCheckbox.Checked = false;
            }, null);
        }

        public bool TryProjectPanelToMonitor(Vector2 panelPos, out Vector2 finalPos)
        {
            var isCabEnabled = Properties.Cab.Enabled;
            if (!isCabEnabled || !ClickMatrix.HasValue || !Properties.Cab.Positions.Contains(panelPos))
            {
                finalPos = Vector2.Zero;
                return false;
            }

            var panelPos4D = new Vector4(panelPos.X, panelPos.Y, 0, 1);
            var finalPos4D = Vector4.Transform(panelPos4D, ClickMatrix.Value);
            if (MathHelper.Abs(finalPos4D.W) < Epsilons.FloatEpsilon)
            {
                finalPos = Vector2.Zero;
                return false;
            }

            finalPos = new Vector2(finalPos4D.X / finalPos4D.W, finalPos4D.Y / finalPos4D.W);
            return true;
        }

        public void Initialize(Scenario scenario)
        {
            if (Attached) Detach();
            DisposeVehiclePanelElement();
            _scenario = scenario;
            var isCabEnabled = Properties.Cab.Enabled;
            if (!isCabEnabled)
            {
                TexWidth = Properties.Size.Width;
                TexHeight = Properties.Size.Height;
                CreateCabD3D11Texture();
                return;
            }

            DisposeD3D9Texture();
            _vehiclePanelElement = new Needle(scenario.TimeManager);
            NeedlePhysicsHelper.InjectPhysicsSolver(_vehiclePanelElement, scenario);
            SetupCabProjectionGeometry();
        }

        /// <summary>
        ///     根据 Properties.Cab 计算投影几何并重建纹理/模型/Effect 矩阵。
        ///     冷启动 Initialize 与热重载 ReconfigureCabProjection 共用。
        ///     前置条件：_vehiclePanelElement 与 _scenario 已就绪；调用方负责 Dispose 旧纹理资源。
        /// </summary>
        private void SetupCabProjectionGeometry()
        {
            var panel = _scenario.Vehicle.Panel;
            // 钳制Tilt防止极端视角
            const float maxTiltDegrees = 80f;
            var safeTiltX = MathHelper.Clamp(Properties.Cab.TiltX, -maxTiltDegrees, maxTiltDegrees);
            var safeTiltY = MathHelper.Clamp(Properties.Cab.TiltY, -maxTiltDegrees, maxTiltDegrees);
            var tiltX = MathHelper.ToRadians(safeTiltX);
            var tiltY = MathHelper.ToRadians(safeTiltY);
            var rotZ = MathHelper.ToRadians(Properties.Cab.Rotation);
            // R_3D = RotZ(-rotZ)·RotX(tiltX)·RotY(tiltY)，与 BVE cb.m() 的指针局部变换一致
            var rotation3D = Matrix.RotationZ(-rotZ) * Matrix.RotationX(tiltX) * Matrix.RotationY(tiltY);
            var q = new[]
            {
                Properties.Cab.Positions.TopLeft, Properties.Cab.Positions.TopRight,
                Properties.Cab.Positions.BottomRight, Properties.Cab.Positions.BottomLeft
            };
            var origin = Properties.Cab.Origin ?? (q[0] + q[1] + q[2] + q[3]) / 4f;
            // 逆向投影：从 BVE 相机经 Q 各角发射射线，用 m^-1（消去整个 R_3D 与 origin 平移）变回未旋转的 Mesh 局部系，
            // 与局部 z=0 平面求交得 L0..L3。L 位于“未旋转的轴对齐局部系”，即 c0..c3 的逆投影。
            float minLx = float.MaxValue, maxLx = float.MinValue;
            float minLy = float.MaxValue, maxLy = float.MinValue;
            var validUnprojectCount = 0;
            foreach (var p in q)
            {
                var l = ProjectionHelper.UnprojectPanelToLocal(p, rotation3D, origin, panel);
                if (l.HasValue)
                {
                    minLx = Math.Min(minLx, l.Value.X);
                    maxLx = Math.Max(maxLx, l.Value.X);
                    minLy = Math.Min(minLy, l.Value.Y);
                    maxLy = Math.Max(maxLy, l.Value.Y);
                    validUnprojectCount++;
                }
            }

            // Q 的 AABB（panel 空间）作为纹理缩放基准：meshWidth/Q_width 即 tilt 拉伸比（局部相对 panel）。
            // 关键：用 Q 而非 L 的 AABB，否则因 meshWidth≈L_AABB 会让比值≈1，抵消膨胀、欠采样。
            var qAabbMinX = Math.Min(Math.Min(q[0].X, q[1].X), Math.Min(q[2].X, q[3].X));
            var qAabbMaxX = Math.Max(Math.Max(q[0].X, q[1].X), Math.Max(q[2].X, q[3].X));
            var qAabbMinY = Math.Min(Math.Min(q[0].Y, q[1].Y), Math.Min(q[2].Y, q[3].Y));
            var qAabbMaxY = Math.Max(Math.Max(q[0].Y, q[1].Y), Math.Max(q[2].Y, q[3].Y));
            var originalWidth = Math.Max(1f, qAabbMaxX - qAabbMinX);
            var originalHeight = Math.Max(1f, qAabbMaxY - qAabbMinY);
            // 退化保护：射线与平面平行或求交失败时回退到默认尺寸
            if (validUnprojectCount < 4 || minLx >= maxLx || minLy >= maxLy)
            {
                minLx = 0;
                maxLx = originalWidth;
                minLy = -originalHeight;
                maxLy = 0;
            }

            // L 的 AABB（局部系轴对齐）加 SafeMargin 得 c0..c3 矩形；c 经旋转后在 panel 投影成 P（OBB 包裹 Q），
            // 紧贴 Q 的逆投影、消除旋转虚胖。SafeMargin 防 GPU 各向异性过滤边缘采样到空白产生黑边。
            var paddedMinX = minLx - SafeMargin;
            var paddedMaxX = maxLx + SafeMargin;
            var paddedMinY = minLy - SafeMargin;
            var paddedMaxY = maxLy + SafeMargin;
            var meshWidth = paddedMaxX - paddedMinX;
            var meshHeight = paddedMaxY - paddedMinY;
            var uiWidth = Properties.Size.Width;
            var uiHeight = Properties.Size.Height;
            // 纹理按 meshWidth/Q_width 比例放大：UI 在纹理中的 t 区域 ≈ Q/P 纹理像素，目标 Q 区域 ≈ 1:1 覆盖 uiWidth。
            // 上限 4096 防爆显存。
            var rawTexWidth = (int)Math.Ceiling(meshWidth * (uiWidth / originalWidth));
            var rawTexHeight = (int)Math.Ceiling(meshHeight * (uiHeight / originalHeight));
            TexWidth = Math.Max(uiWidth, MathHelper.Clamp(rawTexWidth, uiWidth, MaxTextureSize));
            TexHeight = Math.Max(uiHeight, MathHelper.Clamp(rawTexHeight, uiHeight, MaxTextureSize));
            CreateCabD3D11Texture();
            // c0..c3：未旋转的轴对齐局部矩形（Y 朝上）。保持矩形避免 GPU 渲染梯形时的对角线折痕。
            var c0 = new Vector3(paddedMinX, paddedMaxY, 0);
            var c1 = new Vector3(paddedMaxX, paddedMaxY, 0);
            var c2 = new Vector3(paddedMaxX, paddedMinY, 0);
            var c3 = new Vector3(paddedMinX, paddedMinY, 0);
            // 仅把 RotZ(-rotZ) 烘焙进 Mesh 顶点；RotX/RotY(tilt) 通过 Needle.Tilt 交给 BVE 原生管线。
            // 矩阵结合律保证 (c·RotZ)·RotX·RotY = c·(RotZ·RotX·RotY) = c·R_3D，与 ProjectLocalToPanel 的 m 一致。
            var rz = Matrix.RotationZ(-rotZ);
            var v0 = Vector3.TransformCoordinate(c0, rz);
            var v1 = Vector3.TransformCoordinate(c1, rz);
            var v2 = Vector3.TransformCoordinate(c2, rz);
            var v3 = Vector3.TransformCoordinate(c3, rz);
            var normal = Vector3.TransformNormal(new Vector3(0, 0, -1), rz);
            var daytimeMesh = PanelElementFactory.CreateMonitorMesh(v0, v1, v2, v3, normal);
            DaytimeModel?.Dispose();
            DaytimeModel = Model.FromMesh(daytimeMesh);
            EnsureD3D9Texture();
            // Origin=(0,0) 因 Mesh 顶点已在局部原点系；ImageLocation=origin 对应 BVE 的平移项 base.h。
            _vehiclePanelElement.Layer = Properties.Cab.Layer ?? double.MaxValue;
            _vehiclePanelElement.Tilt = new SlimDX.Vector2(tiltX, tiltY);
            _vehiclePanelElement.ImageLocation = new Point((int)Math.Round(origin.X, MidpointRounding.AwayFromZero),
                (int)Math.Round(origin.Y, MidpointRounding.AwayFromZero));
            _vehiclePanelElement.DaytimeImageModel = DaytimeModel;
            // 正向投影 c0..c3 → panel 得 p0..p3（Mesh 在 panel 上的实际落点）
            var p0 = ProjectionHelper.ProjectLocalToPanel(c0, rotation3D, origin, panel);
            var p1 = ProjectionHelper.ProjectLocalToPanel(c1, rotation3D, origin, panel);
            var p2 = ProjectionHelper.ProjectLocalToPanel(c2, rotation3D, origin, panel);
            var p3 = ProjectionHelper.ProjectLocalToPanel(c3, rotation3D, origin, panel);
            // H_total: UI 空间 → 目标四边形 Q
            _hTotal = Properties.Cab.Positions.ToHomographyMatrix(uiWidth, uiHeight);
            // H_native: 纹理空间 → Mesh 投影四边形 P(p0..p3)
            var hNative = new Quad2D(
                new Vector2(p0.X, p0.Y), new Vector2(p1.X, p1.Y),
                new Vector2(p3.X, p3.Y), new Vector2(p2.X, p2.Y)
            ).ToHomographyMatrix(TexWidth, TexHeight);
            // H_correct = H_native^-1 ∘ H_total：UI → 纹理(t 位置)，Mesh 采样后投影恰好落在 Q。
            // 因 Mesh 已通过 tilt/rotation 迎合视角，P≈Q，H_correct 形变极小 → D2D 近正交采样，消除斜采样模糊。
            // 数学等价：t_k = H_native^-1·q_k，H_correct 把 UI 钉到 texture 中的 t 位置。
            if (Matrix4x4.Invert(hNative, out var hNativeInv))
            {
                var t0 = Vector4.Transform(new Vector4(q[0].X, q[0].Y, 0f, 1f), hNativeInv);
                t0 /= t0.W;
                var t1 = Vector4.Transform(new Vector4(q[1].X, q[1].Y, 0f, 1f), hNativeInv);
                t1 /= t1.W;
                var t2 = Vector4.Transform(new Vector4(q[2].X, q[2].Y, 0f, 1f), hNativeInv);
                t2 /= t2.W;
                var t3 = Vector4.Transform(new Vector4(q[3].X, q[3].Y, 0f, 1f), hNativeInv);
                t3 /= t3.W;
                var quadTextureSpace = new Quad2D(
                    new Vector2(t0.X, t0.Y), new Vector2(t1.X, t1.Y),
                    new Vector2(t3.X, t3.Y), new Vector2(t2.X, t2.Y));
                var hCorrect = quadTextureSpace.ToHomographyMatrix(uiWidth, uiHeight);
                ProjectEffect.SetValue((int)Transform3DProperties.TransformMatrix, hCorrect);
            }
            else
            {
                ProjectEffect.SetValue((int)Transform3DProperties.TransformMatrix, _hTotal);
            }
        }

        /// <summary>
        ///     热重载：Cab 投影参数变更。
        ///     仅重建纹理类资源 + DaytimeModel；复用 ProjectEffect/_vehiclePanelElement。
        ///     外屏是否显示不变。
        /// </summary>
        public virtual void ReconfigureCabProjection(MonitorProperties newProps)
        {
            var oldEnabled = Properties.Cab.Enabled;
            var newEnabled = newProps.Cab.Enabled;
            var ghostingChanged =
                Math.Abs(MonitorGameOutput.GhostingDecayTimeSeconds - newProps.GhostingDecayTimeSeconds) >
                Epsilons.FloatEpsilon;
            Properties = newProps;
            _showUiDebugRect = newProps.ShowDebugRect;
            if (ghostingChanged)
            {
                MonitorGameOutput.ReconfigureGhosting(newProps.GhostingDecayTimeSeconds);
                Monitor.ResetActiveScreen(true);
            }

            // 1. Cab.Enabled 跳变：启用<->禁用之间的资源迁移
            if (!oldEnabled && newEnabled)
            {
                // false→true：复用 true→false 保留的 ProjectEffect + cab 资源 + _vehiclePanelElement
                if (ProjectEffect == null)
                {
                    ProjectEffect = new ID2D1Effect(Context.D2D1Context.CreateEffect(EffectGuids.Transform3D));
                    ProjectEffect.SetValue((int)Transform3DProperties.BorderMode, BorderMode.Soft);
                    ProjectEffect.SetValue((int)Transform3DProperties.InterpolationMode,
                        Transform3DInterpolationMode.Linear);
                }

                EnsureCabResources(newProps);
                _vehiclePanelElement = new Needle(_scenario.TimeManager);
                NeedlePhysicsHelper.InjectPhysicsSolver(_vehiclePanelElement, _scenario);
                DisposeD3D9Texture();
                SetupCabProjectionGeometry();
                UpdateClickMatrix(newProps);
                // 运行期热重载 false→true：此时 Attached 已为 true，Attach() 会早返回，
                // 必须在此手动把新建的 _vehiclePanelElement 注册进 Panel.Elements，否则 BVE 不渲染 cab 画面。
                if (Attached && _scenario != null)
                {
                    _scenario.Vehicle.Panel.Elements.Add(_vehiclePanelElement);
                    _scenario.Vehicle.Panel.SortElements();
                }
            }
            else if (oldEnabled && !newEnabled)
            {
                // 运行期热重载 true→false：若已 Attached，先从 Panel.Elements 移除再 Dispose。
                if (Attached && _scenario != null && _vehiclePanelElement != null)
                    _scenario.Vehicle.Panel.Elements.Remove(_vehiclePanelElement);
                // true→false：销毁 cab 资源 + _vehiclePanelElement，释放 D3D9 纹理
                DisposeCabResources();
                DisposeD3D9Texture();
                DisposeCabTextures();
                DaytimeModel?.Dispose();
                DaytimeModel = null;
                DisposeVehiclePanelElement();
                TexWidth = newProps.Size.Width;
                TexHeight = newProps.Size.Height;
                CreateCabD3D11Texture();
                UpdateClickMatrix(newProps);
            }
            else if (newEnabled)
            {
                // 双方都启用：仅更新 cab 资源（border/lighting 常量）+ 重建纹理/几何
                EnsureCabResources(newProps);
                DisposeD3D9Texture();
                DisposeCabTextures();
                SetupCabProjectionGeometry();
                UpdateClickMatrix(newProps);
            }

            // 2. External.DisplayMode 变更（外屏是否显示不变）— 应用新模式
            if (ExternalForm != null && !ExternalForm.IsDisposed &&
                !Equals(ExternalForm.DisplayMode, newProps.External.DisplayMode))
            {
                ReconfigureExternalFormDisplayMode(newProps.External.DisplayMode);
            }
            else if (ExternalForm != null && !ExternalForm.IsDisposed && _extSharedD3D11Texture != null)
            {
                // 分辨率变更（DisplayMode 不变）：重建共享纹理 + 同步 renderSize 到外屏
                var texDesc = _extSharedD3D11Texture.Description;
                if (texDesc.Width != newProps.Size.Width || texDesc.Height != newProps.Size.Height)
                {
                    _extSharedD3D11Texture.Dispose();
                    _extSharedD3D11Texture = Context.D3D11Device.CreateTexture2D(
                        RenderHelper.CreateRenderTargetTextureDescription(newProps.Size.Width, newProps.Size.Height));
                    ExternalForm.SetSharedTexture(_extSharedD3D11Texture);
                    ExternalForm.SetRenderSize(newProps.Size);
                }
            }

            // 3. 标记投影脏：强制下一帧 RenderCabProjection 重渲（即便 Frozen 也生效）
            IsCabProjectionDirty = true;
        }

        /// <summary>
        ///     热重载：物理分辨率变更。移除并 Dispose 旧 MonitorOutput → 添加新分辨率 output → 走投影重建。
        /// </summary>
        public virtual void ReconfigureResolution(MonitorProperties newProps)
        {
            var oldOutput = MonitorGameOutput;
            Monitor.RemoveOutput(oldOutput);
            oldOutput.Dispose();
            MonitorGameOutput = new MonitorOutput(newProps.Size, Context.D2D1Context, Context.D3D11Device,
                newProps.GhostingDecayTimeSeconds);
            Monitor.AddOutput(MonitorGameOutput);
            Monitor.ResetActiveScreen(true);
            ReconfigureCabProjection(newProps);
        }

        private void UpdateClickMatrix(MonitorProperties props)
        {
            if (!props.Cab.Enabled)
            {
                ClickMatrix = null;
                return;
            }

            if (Matrix4x4.Invert(props.Cab.Positions.ToHomographyMatrix(props.Size.Width, props.Size.Height),
                    out var clickMatrix))
                ClickMatrix = clickMatrix;
            else
                ClickMatrix = null;
        }

        public void SetShowTextureBoundsRect(bool value)
        {
            _showTextureBoundsRect = value;
            ClearColor = new Color4(1, 0, 0, value ? 1 : 0);
            IsCabProjectionDirty = true;
        }

        public void SetShowUiDebugRect(bool value)
        {
            _showUiDebugRect = value;
            Monitor.ResetActiveScreen(true);
        }

        /// <summary>
        ///     热重载 BufferFrameCount：基类默认 no-op，由 D3D9/D3D9Ex 子类 override 重建 ring/shared buffer。
        ///     子类实现需确保重建后 Sync 状态机处于干净初值。
        /// </summary>
        public virtual void ReconfigureBufferFrameCount(int newBufferFrameCount)
        {
            BufferFrameCount = newBufferFrameCount;
        }

        /// <summary>
        ///     原地更新 ExternalForm 的 DisplayMode，无需销毁重建。
        ///     SetDisplayMode 内部通过 _needsResize 触发 STA 线程的 SwapChain 重建 + D2D 资源迁移。
        /// </summary>
        private void ReconfigureExternalFormDisplayMode(ScreenDisplayMode newMode)
        {
            ExternalForm.SetDisplayMode(newMode);
            ExtFormNeedsSync = true;
        }

        /// <summary>
        ///     生产背压：true = 本 holder 本帧跳过内容生产（Monitor.Draw 与 cab 投影渲染/提交/外屏同步）。
        ///     D3D9Ex 子类用写槽 fence（IsNextWriteSlotPending）实现；基类恒不阻塞。
        /// </summary>
        protected virtual bool IsProductionBlocked()
        {
            return false;
        }

        public void DrawMonitorContent(TimeSpan elapsed)
        {
            Submitted = false;
            if (IsProductionBlocked())
            {
                _pendingElapsed += elapsed;
                Debugger?.AddLineLasting($"{Monitor.Id} draw blocked");
                return;
            }

            if (Properties.Cab.Enabled) EnsureD3D9Texture();
            var effectiveElapsed = elapsed + _pendingElapsed;
            _pendingElapsed = TimeSpan.Zero;
            Monitor.Draw(effectiveElapsed, false);
        }

        public void RenderCabProjectionFrame(bool contentIsStatic)
        {
            Submitted = false;
            Actions = ComputeFrameActions(contentIsStatic);
            if (Actions.ShouldRender) RenderCabProjection();
            if (Actions.ShouldSubmit) IsCabProjectionDirty = false;
            Debugger?.AddLine($"{Monitor.Id} renderCab:{Actions.ShouldRender}");
        }

        /// <summary>
        ///     本帧刷新判定：把原 Frozen/EffectiveFrozen/LightingReRenderedWhileFrozen/IsCabProjectionDirty/IsContentStaticThisFrame
        ///     五标志的分散守卫收拢于此，返回 ShouldRender/ShouldSubmit/ShouldSync 供
        ///     RenderCabProjectionFrame/Submit/Sync 读。
        ///     lightingChanged 由 UpdateLighting 内部比对。
        /// </summary>
        private FrameActions ComputeFrameActions(bool contentIsStatic)
        {
            // Frozen 排除 JustFrozen 边沿帧（边沿帧需渲一次捕获冻结瞬间画面）
            var frozen = Frozen;
            var effectiveFrozen = frozen || contentIsStatic;
            var lightingChanged = UpdateLighting();
            // D3D11 写槽背压：积压时停产（不渲/不提交/不同步外屏）。
            // IsCabProjectionDirty 仅在 ShouldSubmit 时清除，背压帧保留、恢复后自动重渲。
            var blocked = IsProductionBlocked();
            var hasReason = !blocked && (!effectiveFrozen || IsCabProjectionDirty || lightingChanged);
            return new FrameActions
            {
                ShouldRender = hasReason && Properties.Cab.Enabled,
                ShouldSubmit = hasReason,
                ShouldSync = hasReason,
                ShouldSyncExternal = !blocked && !effectiveFrozen
            };
        }

        protected void RenderCabProjection()
        {
            if (ProjectEffect == null || CabD2D1TransformBitmap == null) return;
            RenderCabProjectionWithLighting(MonitorGameOutput.OutputBitmap, CabD2D1TransformBitmap);
        }

        protected void RenderCabProjectionWithLighting(ID2D1Bitmap rawInput, ID2D1Bitmap outputTarget)
        {
            if (outputTarget == null || rawInput == null) return;
            var brightness = Monitor.Brightness;
            var useShader = _lighting.Enabled || brightness < 1f || CabBorderRadius > 0;
            if (useShader)
            {
                UpdateLightingConstants(_lighting.Enabled, brightness);
                AdaptiveScreenLightingEffect.SetInput(0, rawInput, true);
                ProjectEffect.SetInputEffect(0, AdaptiveScreenLightingEffect);
            }
            else
            {
                ProjectEffect.SetInput(0, rawInput, true);
            }

            Context.D2D1Context.Target = outputTarget;
            Context.D2D1Context.Clear(ClearColor);
            Context.D2D1Context.DrawImage(ProjectEffect);
        }

        private void UpdateLightingConstants(bool lightingEnabled, float brightness)
        {
            var brightExtent = _lighting.BrightExtent ?? new Vector2(0, 1e6f);
            var shadowExtent = _lighting.ShadowExtent ?? new Vector2(0, 1e6f - 1e3f);
            var halfW = Properties.Size.Width / 2f;
            var halfH = Properties.Size.Height / 2f;
            var ambientScale = _lighting.AmbientMax > 0f ? _lighting.AmbientMax : 1f;
            var ambientFactor = MathHelper.Clamp(RetrieveAmbient() / ambientScale, 0f, 1f);
            var dimGamma = _lighting.NightDimmingResponse +
                           (1f - _lighting.NightDimmingResponse) * ambientFactor;
            var perceptualB = (float)Math.Pow(MathHelper.Clamp(brightness, 0f, 1f), dimGamma);
            var srgbAdapt = _lighting.NightAdaptationGain +
                            (1f - _lighting.NightAdaptationGain) * ambientFactor;
            var linearBrightness = (float)Math.Pow(perceptualB * srgbAdapt, 2.2);
            var leakScale = linearBrightness / _lighting.PanelContrastRatio;
            var leakColor = _lighting.LeakColor;
            var compressThreshold = _lighting.CompressThresholdNight +
                                    (1f - _lighting.CompressThresholdNight) * ambientFactor;
            AdaptiveScreenLightingEffect.UpdateConstants(new AdaptiveScreenLightingConstants
            {
                HTotal = _hTotal,
                BrightExtent = brightExtent,
                ShadowExtent = shadowExtent,
                CornerRect = new Vector4(halfW, halfH, halfW, halfH),
                CornerRadius = CabBorderRadius,
                Mode = lightingEnabled ? 0f : 1f,
                AmbientFactor = ambientFactor,
                CompressThreshold = compressThreshold,
                LinearBrightness = linearBrightness,
                GlassReflectanceLight = _lighting.GlassReflectanceLight,
                GlassReflectanceDark = _lighting.GlassReflectanceDark,
                Brightness = brightness,
                Leakage = new Color3(leakColor.R * leakScale, leakColor.G * leakScale,
                    leakColor.B * leakScale),
                GlareColor = _lighting.GlareColor
            });
        }

        private float RetrieveAmbient()
        {
            var currentLocation = _scenario.VehicleLocation.Location;
            var cabIlluminance = (float)_scenario.Map.CabIlluminanceObjects.GetValueAt(currentLocation);
            return 1.0f - cabIlluminance;
        }

        private bool UpdateLighting()
        {
            var ambient = RetrieveAmbient();
            var brightness = Monitor.Brightness;
            var changed = Math.Abs(ambient - _lastEAmbient) > Epsilons.FloatEpsilon ||
                          Math.Abs(brightness - _lastBrightness) > Epsilons.FloatEpsilon;
            _lastEAmbient = ambient;
            _lastBrightness = brightness;
            return changed;
        }

        public void Attach()
        {
            if (_scenario == null || Attached) return;
            Attached = true;

            if (_vehiclePanelElement != null && Properties.Cab.Enabled)
            {
                _scenario.Vehicle.Panel.Elements.Add(_vehiclePanelElement);
                _scenario.Vehicle.Panel.SortElements();
            }
        }

        public virtual void Detach()
        {
            if (!Attached) return;
            Attached = false;
            ExternalForm?.Hide();
            if (_scenario != null && _vehiclePanelElement != null && Properties.Cab.Enabled)
                _scenario.Vehicle.Panel.Elements.Remove(_vehiclePanelElement);
            Monitor.Reset();
        }

        public virtual void DisposeD3D9Texture()
        {
            D3D9MergedTexture?.Dispose();
            D3D9MergedTexture = null;
        }

        public void DisposeVehiclePanelElement()
        {
            if (_vehiclePanelElement == null) return;
            _vehiclePanelElement.Dispose();
            _vehiclePanelElement.DaytimeImageModel = null;
            _vehiclePanelElement = null;
        }

        private void DisposeExternalForm()
        {
            if (ExternalForm == null) return;
            var form = ExternalForm;
            ExternalForm = null;
            form.OnLeftClick -= OnExternalFormLeftClick;
            form.Hidden -= OnExternalFormHidden;
            if (!form.IsDisposed)
                try
                {
                    form.Dispose();
                }
                catch (Exception)
                {
                    // ignored
                }
        }

        protected abstract void CreateCabD3D11Texture();
        public abstract void EnsureD3D9Texture();
        public abstract void Submit();
        public abstract void Sync();

        protected void SyncExternalCopy(ID3D11Texture2D sourceTexture)
        {
            if (ExternalForm != null && !ExternalForm.IsDisposed)
            {
                if (!HasExternalFormShown && ExternalForm.IsHwndReady) HasExternalFormShown = true;
                if (HasExternalFormShown && sourceTexture != null && _extSharedD3D11Texture != null &&
                    (ExternalForm.IsVisible || !ExternalForm.HasShownOnce))
                {
                    Context.D3D11Context.CopyResource(_extSharedD3D11Texture, sourceTexture);
                    ExtFormNeedsSync = false;
                    HasSyncedExternalThisFrame = true;
                }
            }
        }

        public void RenderExternal()
        {
            if (ExternalForm == null || ExternalForm.IsDisposed || !HasExternalFormShown)
                return;
            if (ExternalForm.HasShownOnce && !ExternalForm.IsVisible)
                return;
            if (ExternalForm.IsInSizingLoop)
                return;

            var brightness = Monitor.Brightness;
            var brightnessChanged = Math.Abs(brightness - _lastExternalFormBrightness) > Epsilons.FloatEpsilon;
            _lastExternalFormBrightness = brightness;
            ExternalForm.Brightness = brightness;
            if (!Submitted && !ExtFormNeedsSync && !brightnessChanged && !ExternalForm.NeedsResize &&
                ExternalForm.HasShownOnce && !HasSyncedExternalThisFrame)
                return;
            HasSyncedExternalThisFrame = false;
            ExternalForm.NotifyFrameReady();
        }

        protected struct FrameActions
        {
            /// <summary>
            ///     重投投影（cab 启用且有理由：内容变/光照变/配置脏）
            /// </summary>
            public bool ShouldRender;

            /// <summary>
            ///     提交新纹理到 buffer（有理由即提交；Submit 另加消费者守卫）
            /// </summary>
            public bool ShouldSubmit;

            /// <summary>
            ///     同步 cab 纹理到 D3D9（有理由即同步）
            /// </summary>
            public bool ShouldSync;

            /// <summary>
            ///     同步外屏原始纹理（仅内容真变了=非冻结且本帧画过；外屏不关心光照/cab 脏，只看原始内容）
            /// </summary>
            public bool ShouldSyncExternal;
        }
    }
}