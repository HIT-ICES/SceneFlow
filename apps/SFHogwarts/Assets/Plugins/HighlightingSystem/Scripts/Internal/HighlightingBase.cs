using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace HighlightingSystem
{
    [RequireComponent(typeof(Camera))]
    public class HighlightingBase : MonoBehaviour
    {
        #region Static Fields and Constants

        protected static readonly Color colorClear = new(0f, 0f, 0f, 0f);
        protected static readonly string renderBufferName = "HighlightingSystem";
        protected static readonly Matrix4x4 identityMatrix = Matrix4x4.identity;
        protected const CameraEvent queue = CameraEvent.BeforeImageEffectsOpaque;

        protected static RenderTargetIdentifier cameraTargetID;

        protected static Mesh quad;

        // Graphics device version identifiers
        protected const int OGL = 0;
        protected const int D3D9 = 1;
        protected const int D3D11 = 2;

        // Current graphics device version: 0 = OpenGL or unknown (default), 1 = Direct3D 9, 2 = Direct3D 11
        protected static int graphicsDeviceVersion = D3D9;

        #endregion

        #region Public Fields

        // Depth offset factor for highlighting shaders
        public float offsetFactor = 0f;

        // Depth offset units for highlighting shaders
        public float offsetUnits = 0f;

        // Highlighting buffer size downsample factor
        public int downsampleFactor
        {
            get => _downsampleFactor;
            set
            {
                if (_downsampleFactor != value)
                {
                    // Is power of two check
                    if (value != 0 && (value & (value - 1)) == 0)
                    {
                        _downsampleFactor = value;
                        isDirty = true;
                    }
                    else
                    {
                        Debug.LogWarning(
                            "HighlightingSystem : Prevented attempt to set incorrect downsample factor value.");
                    }
                }
            }
        }

        // Blur iterations
        public int iterations
        {
            get => _iterations;
            set
            {
                if (_iterations != value)
                {
                    _iterations = value;
                    isDirty = true;
                }
            }
        }

        // Blur minimal spread
        public float blurMinSpread
        {
            get => _blurMinSpread;
            set
            {
                if (_blurMinSpread != value)
                {
                    _blurMinSpread = value;
                    isDirty = true;
                }
            }
        }

        // Blur spread per iteration
        public float blurSpread
        {
            get => _blurSpread;
            set
            {
                if (_blurSpread != value)
                {
                    _blurSpread = value;
                    isDirty = true;
                }
            }
        }

        // Blurring intensity for the blur material
        public float blurIntensity
        {
            get => _blurIntensity;
            set
            {
                if (_blurIntensity != value)
                {
                    _blurIntensity = value;
                    if (Application.isPlaying) blurMaterial.SetFloat(ShaderPropertyID._Intensity, _blurIntensity);
                }
            }
        }

        #endregion

        #region Protected Fields

        protected CommandBuffer renderBuffer;

        protected bool isDirty = true;

        protected int cachedWidth = -1;
        protected int cachedHeight = -1;
        protected int cachedAA = -1;

        [FormerlySerializedAs("downsampleFactor")]
        [SerializeField]
        protected int _downsampleFactor = 4;

        [FormerlySerializedAs("iterations")]
        [SerializeField]
        protected int _iterations = 2;

        [FormerlySerializedAs("blurMinSpread")]
        [SerializeField]
        protected float _blurMinSpread = 0.65f;

        [FormerlySerializedAs("blurSpread")]
        [SerializeField]
        protected float _blurSpread = 0.25f;

        [SerializeField] protected float _blurIntensity = 0.3f;

        // RenderTargetidentifier for the highlightingBuffer RenderTexture
        protected RenderTargetIdentifier highlightingBufferID;

        // RenderTexture with highlighting buffer
        protected RenderTexture highlightingBuffer;

        // Camera reference
        protected Camera cam;

        // True if HighlightingSystem is supported on this platform
        protected bool isSupported;

        // True if framebuffer depth data is currently available (it is required for the highlighting occlusion feature)
        protected bool isDepthAvailable = true;

        // Material parameters
        protected const int BLUR = 0;
        protected const int CUT = 1;
        protected const int COMP = 2;

        protected static readonly string[] shaderPaths =
        {
            "Hidden/Highlighted/Blur",
            "Hidden/Highlighted/Cut",
            "Hidden/Highlighted/Composite"
        };

        protected static Shader[] shaders;
        protected static Material[] materials;

        // Static materials
        protected static Material cutMaterial;
        protected static Material compMaterial;

        // Dynamic materials
        protected Material blurMaterial;

        protected static bool initialized;

        #endregion

        #region MonoBehaviour

        // 
        protected virtual void OnEnable()
        {
            if (!CheckInstance()) return;

            Initialize();

            isSupported = CheckSupported();
            if (!isSupported)
            {
                enabled = false;
                Debug.LogError(
                    "HighlightingSystem : Highlighting System has been disabled due to unsupported Unity features on the current platform!");
                return;
            }

            blurMaterial = new Material(materials[BLUR]);

            // Set initial intensity in blur material
            blurMaterial.SetFloat(ShaderPropertyID._Intensity, _blurIntensity);

            renderBuffer = new CommandBuffer();
            renderBuffer.name = renderBufferName;

            cam = GetComponent<Camera>();
            UpdateHighlightingBuffer();

            // Force-rebuild renderBuffer
            isDirty = true;

            cam.AddCommandBuffer(queue, renderBuffer);
        }

        // 
        protected virtual void OnDisable()
        {
            if (renderBuffer != null)
            {
                cam.RemoveCommandBuffer(queue, renderBuffer);
                renderBuffer = null;
            }

            if (highlightingBuffer != null && highlightingBuffer.IsCreated())
            {
                highlightingBuffer.Release();
                highlightingBuffer = null;
            }
        }

        // 
        protected virtual void OnPreRender()
        {
            UpdateHighlightingBuffer();

            var aa = GetAA();

            var depthAvailable = aa == 1;

            // In case MSAA is enabled in forward/vertex lit rendeirng paths - depth buffer is not available
            if (aa > 1 && (cam.actualRenderingPath == RenderingPath.Forward ||
                           cam.actualRenderingPath == RenderingPath.VertexLit)) depthAvailable = false;

            // Check if framebuffer depth data availability has changed
            if (isDepthAvailable != depthAvailable)
            {
                isDepthAvailable = depthAvailable;
                // Update ZWrite value for all highlighting shaders correspondingly (isDepthAvailable ? ZWrite Off : ZWrite On)
                Highlighter.SetZWrite(isDepthAvailable ? 0f : 1f);
                if (isDepthAvailable)
                    Debug.LogWarning(
                        "HighlightingSystem : Framebuffer depth data is available back again and will be used to occlude highlighting. Highlighting occluders disabled.");
                else
                    Debug.LogWarning(
                        "HighlightingSystem : Framebuffer depth data is not available and can't be used to occlude highlighting. Highlighting occluders enabled.");
                isDirty = true;
            }

            // Set global depth offset properties for highlighting shaders to the values which has this HighlightingBase component
            Highlighter.SetOffsetFactor(offsetFactor);
            Highlighter.SetOffsetUnits(offsetUnits);

            isDirty |= HighlighterManager.isDirty;
            isDirty |= HighlightersChanged();

            if (isDirty)
            {
                RebuildCommandBuffer();
                isDirty = false;
            }
        }

        // 
        protected virtual void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            Graphics.Blit(src, dst, compMaterial);
        }

        #endregion

        #region Internal

        // 
        protected static void Initialize()
        {
            if (initialized) return;

            // Determine graphics device version
            var version = SystemInfo.graphicsDeviceVersion.ToLower();
            if (version.Contains("direct3d") || version.Contains("directx"))
            {
                if (version.Contains("direct3d 11") || version.Contains("directx 11"))
                    graphicsDeviceVersion = D3D11;
                else
                    graphicsDeviceVersion = D3D9;
            }
#if UNITY_EDITOR_WIN && (UNITY_ANDROID || UNITY_IOS)
			else if (version.Contains("emulated"))
			{
				graphicsDeviceVersion = D3D9;
			}
#endif
            else
            {
                graphicsDeviceVersion = OGL;
            }

            // Initialize shader property constants
            ShaderPropertyID.Initialize();

            // Initialize shaders and materials
            var l = shaderPaths.Length;
            shaders = new Shader[l];
            materials = new Material[l];
            for (var i = 0; i < l; i++)
            {
                var shader = Shader.Find(shaderPaths[i]);
                shaders[i] = shader;

                var material = new Material(shader);
                materials[i] = material;
            }

            cutMaterial = materials[CUT];
            compMaterial = materials[COMP];

            // Initialize static RenderTargetIdentifiers
            cameraTargetID = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

            // Create static quad mesh
            CreateQuad();

            initialized = true;
        }

        // 
        protected static void CreateQuad()
        {
            if (quad == null)
                quad = new Mesh();
            else
                quad.Clear();

            var y1 = 1f;
            var y2 = -1f;

            if (graphicsDeviceVersion == OGL)
            {
                y1 = -1f;
                y2 = 1f;
            }

            quad.vertices = new Vector3[]
            {
                new(-1f, y1, 0f), // Bottom-Left
                new(-1f, y2, 0f), // Upper-Left
                new(1f, y2, 0f), // Upper-Right
                new(1f, y1, 0f) // Bottom-Right
            };

            quad.uv = new Vector2[]
            {
                new(0f, 0f),
                new(0f, 1f),
                new(1f, 1f),
                new(1f, 0f)
            };

            quad.colors = new[]
            {
                colorClear,
                colorClear,
                colorClear,
                colorClear
            };

            quad.triangles = new[] { 0, 1, 2, 2, 3, 0 };
        }

        // 
        protected virtual int GetAA()
        {
            var aa = QualitySettings.antiAliasing;
            if (aa == 0) aa = 1;

            // Reset aa value to 1 in case camera is in DeferredLighting or DeferredShading Rendering Path
            if (cam.actualRenderingPath == RenderingPath.DeferredShading) aa = 1;

            return aa;
        }

        // 
        protected virtual void UpdateHighlightingBuffer()
        {
            var aa = GetAA();

            if (cam.pixelWidth == cachedWidth && cam.pixelHeight == cachedHeight && aa == cachedAA) return;

            cachedWidth = cam.pixelWidth;
            cachedHeight = cam.pixelHeight;
            cachedAA = aa;

            if (highlightingBuffer != null && highlightingBuffer.IsCreated()) highlightingBuffer.Release();

            highlightingBuffer = new RenderTexture(cachedWidth, cachedHeight, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            highlightingBuffer.antiAliasing = cachedAA;
            highlightingBuffer.filterMode = FilterMode.Point;
            highlightingBuffer.useMipMap = false;
            highlightingBuffer.wrapMode = TextureWrapMode.Clamp;
            if (!highlightingBuffer.Create())
                Debug.LogError(
                    "HighlightingSystem : UpdateHighlightingBuffer() : Failed to create highlightingBuffer RenderTexture!");

            highlightingBufferID = new RenderTargetIdentifier(highlightingBuffer);
            Shader.SetGlobalTexture(ShaderPropertyID._HighlightingBuffer, highlightingBuffer);

            var v = new Vector4((graphicsDeviceVersion == OGL ? 1f : -1f) / highlightingBuffer.width,
                1f / highlightingBuffer.height, 0f, 0f);
            Shader.SetGlobalVector(ShaderPropertyID._HighlightingBufferTexelSize, v);

            // Always set as dirty, because camera width/height/anti-aliasing has changed
            isDirty = true;
        }

        // Allow only single instance of the HighlightingBase component on a GameObject
        public virtual bool CheckInstance()
        {
            var highlightingBases = GetComponents<HighlightingBase>();
            if (highlightingBases.Length > 1 && highlightingBases[0] != this)
            {
                enabled = false;
                var className = GetType().ToString();
                Debug.LogWarning(string.Format(
                    "HighlightingSystem : Only single instance of the HighlightingRenderer component is allowed on a single Gameobject! {0} has been disabled on GameObject with name '{1}'.",
                    className, name));
                return false;
            }

            return true;
        }

        // 
        protected virtual bool CheckSupported()
        {

            // Required Render Texture Format supported?
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
            {
                Debug.LogError("HighlightingSystem : RenderTextureFormat.ARGB32 is not supported on this platform!");
                return false;
            }

            if (SystemInfo.supportsStencil < 1)
            {
                Debug.LogError("HighlightingSystem : Stencil buffer is not supported on this platform!");
                return false;
            }

            // HighlightingOpaque shader supported?
            if (!Highlighter.opaqueShader.isSupported)
            {
                Debug.LogError("HighlightingSystem : HighlightingOpaque shader is not supported on this platform!");
                return false;
            }

            // HighlightingTransparent shader supported?
            if (!Highlighter.transparentShader.isSupported)
            {
                Debug.LogError(
                    "HighlightingSystem : HighlightingTransparent shader is not supported on this platform!");
                return false;
            }

            // Required shaders supported?
            for (var i = 0; i < shaders.Length; i++)
            {
                var shader = shaders[i];
                if (!shader.isSupported)
                {
                    Debug.LogError("HighlightingSystem : Shader '" + shader.name +
                                   "' is not supported on this platform!");
                    return false;
                }
            }

            return true;
        }

        // 
        protected virtual bool HighlightersChanged()
        {
            var changed = false;

            // Check if list of highlighted objects has changed
            var e = HighlighterManager.GetEnumerator();
            while (e.MoveNext())
            {
                var highlighter = e.Current;
                changed |= highlighter.UpdateHighlighting(isDepthAvailable);
            }

            return changed;
        }

        // 
        protected virtual void RebuildCommandBuffer()
        {
            renderBuffer.Clear();

            var depthID = isDepthAvailable ? cameraTargetID : highlightingBufferID;

            // Prepare and clear render target
            renderBuffer.SetRenderTarget(highlightingBufferID, depthID);
            renderBuffer.ClearRenderTarget(!isDepthAvailable, true, colorClear);

            // Fill buffer with highlighters rendering commands
            FillBuffer(renderBuffer, 0); // Highlighters
            FillBuffer(renderBuffer, 1); // Occluders
            FillBuffer(renderBuffer, 2); // See-through Highlighters

            // Create two buffers for blurring the image
            var blur1ID = new RenderTargetIdentifier(ShaderPropertyID._HighlightingBlur1);
            var blur2ID = new RenderTargetIdentifier(ShaderPropertyID._HighlightingBlur2);

            var width = highlightingBuffer.width / _downsampleFactor;
            var height = highlightingBuffer.height / _downsampleFactor;

            renderBuffer.GetTemporaryRT(ShaderPropertyID._HighlightingBlur1, width, height, 0, FilterMode.Bilinear,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            renderBuffer.GetTemporaryRT(ShaderPropertyID._HighlightingBlur2, width, height, 0, FilterMode.Bilinear,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            renderBuffer.Blit(highlightingBufferID, blur1ID);

            // Blur the small texture
            var oddEven = true;
            for (var i = 0; i < _iterations; i++)
            {
                var off = _blurMinSpread + _blurSpread * i;
                renderBuffer.SetGlobalFloat(ShaderPropertyID._HighlightingBlurOffset, off);

                if (oddEven)
                    renderBuffer.Blit(blur1ID, blur2ID, blurMaterial);
                else
                    renderBuffer.Blit(blur2ID, blur1ID, blurMaterial);

                oddEven = !oddEven;
            }

            // Upscale blurred texture and cut stencil from it
            renderBuffer.SetGlobalTexture(ShaderPropertyID._HighlightingBlurred, oddEven ? blur1ID : blur2ID);
            renderBuffer.SetRenderTarget(highlightingBufferID, depthID);
            renderBuffer.DrawMesh(quad, identityMatrix, cutMaterial);

            // Cleanup
            renderBuffer.ReleaseTemporaryRT(ShaderPropertyID._HighlightingBlur1);
            renderBuffer.ReleaseTemporaryRT(ShaderPropertyID._HighlightingBlur2);
        }

        // 
        protected virtual void FillBuffer(CommandBuffer buffer, int renderQueue)
        {
            HashSet<Highlighter>.Enumerator e;
            e = HighlighterManager.GetEnumerator();
            while (e.MoveNext())
            {
                var highlighter = e.Current;
                highlighter.FillBuffer(renderBuffer, renderQueue);
            }
        }

        #endregion
    }
}