using UnityEngine;

namespace AuxiliaryComponents
{
    public class SafeAreaHelper : MonoBehaviour
    {

        [SerializeField]
        [Range(0f, 1f)]
        // ReSharper disable once NotAccessedField.Local
        private float _iosTopIgnoredPart;

        [SerializeField]
        [Range(0f, 1f)]
        // ReSharper disable once NotAccessedField.Local
        private float _androidTopIgnoredPart;

        [Space]
        
        [SerializeField]
        [Range(0f, 1f)]
        // ReSharper disable once NotAccessedField.Local
        private float _iosBottomIgnoredPart;

        [SerializeField]
        [Range(0f, 1f)]
        // ReSharper disable once NotAccessedField.Local
        private float _androidBottomIgnoredPart;

        private RectTransform _rt;

        private float _topIgnoredPart;
        private float _bottomIgnored;

        /// <summary>
        /// Sets the relative ignored top padding part.
        /// </summary>
        /// <param name="ignoredPart">Ignored part in range [0; 1]</param>
        public void SetTopIgnored(float ignoredPart)
        {
            SetTopIgnoredInternal(ignoredPart);
            UpdateRectTransform();
        }

        /// <summary>
        /// Sets the relative ignored bottom padding part.
        /// </summary>
        /// <param name="ignoredPart">Ignored part in range [0; 1]</param>
        public void SetBottomIgnored(float ignoredPart)
        {
            SetBottomIgnoredInternal(ignoredPart);
            UpdateRectTransform();
        }

        private void SetTopIgnoredInternal(float ignoredPart)
        {
            _topIgnoredPart = Mathf.Clamp01(ignoredPart);
        }

        private void SetBottomIgnoredInternal(float ignoredPart)
        {
            _bottomIgnored = Mathf.Clamp01(ignoredPart);
        }
        
        private void Awake()
        {
#if DEV_CONSOLE
            CreatedSafeAreasStack.Add(this);
#endif
            _rt = (RectTransform)transform;
#if UNITY_ANDROID
            SetTopIgnoredInternal(_androidTopIgnoredPart);
            SetBottomIgnoredInternal(_androidBottomIgnoredPart);
#else
            SetTopIgnoredInternal(_iosTopIgnoredPart);
            SetBottomIgnoredInternal(_iosBottomIgnoredPart);
#endif
            UpdateRectTransform();
        }

        private void UpdateRectTransform()
        {
            _rt.anchorMax = GetMaxAnchor();
            _rt.anchorMin = GetMinAnchor();
        }
        
        /// <summary>
        /// Gets min safe area max anchor. Considers ignored padding setting;
        /// </summary>
        /// <returns></returns>
        private Vector2 GetMaxAnchor()
        {
            var maxAnchor = _rt.anchorMax;
            var safeMaxY = GetSafeMaxY();
            var topPaddingDelta = (Screen.height - safeMaxY) * _topIgnoredPart;
            maxAnchor.y = (safeMaxY + topPaddingDelta) / Screen.height;
            return maxAnchor;
        }

        /// <summary>
        /// Gets min safe area min anchor. Considers ignored padding setting;
        /// </summary>
        /// <returns></returns>
        private Vector2 GetMinAnchor()
        {
            var minAnchor = _rt.anchorMin;
            var safeMinY = GetSafeMinY();
            var bottomPaddingDelta = safeMinY * _bottomIgnored;
            minAnchor.y = (safeMinY - bottomPaddingDelta)  / Screen.height;
            return minAnchor;
        }

        private float GetSafeMaxY()
        {
#if DEV_CONSOLE
            if (_topDebugPadding.HasValue)
            {
                return Screen.height - _topDebugPadding.Value;
            }
#endif
            return Screen.safeArea.yMax;
        }

        private float GetSafeMinY()
        {
#if DEV_CONSOLE
            if (_bottomDebugPadding.HasValue)
            {
                return _bottomDebugPadding.Value;
            }
#endif
            return Screen.safeArea.yMin;
        }

#if DEV_CONSOLE
        
        private static readonly List<SafeAreaHelper> CreatedSafeAreasStack = new();
        
        private static float? _bottomDebugPadding;
        private static float? _topDebugPadding;
        private static bool _debugGizmosEnabled;
        public static float DebugAlpha { get; set; } = 0.5f;
        
        /// <summary>
        /// Texture to display safe area.
        /// </summary>
        private RenderTexture _safeTexture;
        
        /// <summary>
        /// Texture to display safe area paddings.
        /// </summary>
        private RenderTexture _notSafeTexture;
        
        /// <summary>
        /// Texture to display ignored safe area paddings.
        /// </summary>
        private RenderTexture _ignoredTexture;
        
        public static void SwitchDebugGizmos()
        {
            _debugGizmosEnabled = !_debugGizmosEnabled;
        }
        
        public static void SetDebugPaddings(float? top, float? bottom)
        {
            _topDebugPadding = top;
            _bottomDebugPadding = bottom;
        }

        private void OnGUI()
        {
            if (!_debugGizmosEnabled)
            {
                DisposeTextures();
                return;
            }

            if (CreatedSafeAreasStack[^1] != this)
            {
                return;
            }

            if (!_safeTexture)
            {
                _safeTexture = GetRenderTexture(new Color(1, 1, 1, DebugAlpha));
            }
            if (!_notSafeTexture)
            {
                _notSafeTexture = GetRenderTexture(new Color(1, 0, 0, DebugAlpha));
            }
            if (!_ignoredTexture)
            {
                _ignoredTexture = GetRenderTexture(new Color(0, 0, 1, DebugAlpha));
            }
            
            var min = GetMinAnchor();
            var max = GetMaxAnchor();
            
            
            // Center
            var safeSize = new Vector2(Screen.width, GetSafeMaxY() - GetSafeMinY());
            var safePos = new Vector2(0f, Screen.height - GetSafeMaxY());
            GUI.DrawTexture(new Rect(safePos, safeSize), _safeTexture);

            
            // Top padding
            var topIgnoredSize = new Vector2(Screen.width, Screen.height * max.y - GetSafeMaxY());
            var topIgnoredPos = new Vector2(0f, safePos.y - topIgnoredSize.y);
            GUI.DrawTexture(new Rect(topIgnoredPos, topIgnoredSize), _ignoredTexture);
            
            var topSize = new Vector2(Screen.width, Screen.height - max.y * Screen.height);
            var topPos = new Vector2(0f, 0f);
            GUI.DrawTexture(new Rect(topPos, topSize), _notSafeTexture);

            
            // Bottom padding
            var bottomIgnoredSize = new Vector2(Screen.width, GetSafeMinY() - Screen.height * min.y);
            var bottomIgnoredPos = new Vector2(0f, safePos.y + safeSize.y);
            GUI.DrawTexture(new Rect(bottomIgnoredPos, bottomIgnoredSize), _ignoredTexture);
            
            var bottomSize = new Vector2(Screen.width, min.y * Screen.height);
            var bottomPos = new Vector2(0f, Screen.height - min.y * Screen.height);
            GUI.DrawTexture(new Rect(bottomPos, bottomSize), _notSafeTexture);
        }

        private RenderTexture GetRenderTexture(Color c)
        {
            var tex = RenderTexture.GetTemporary(1, 1);
            using CommandBuffer cmd = new CommandBuffer();
            cmd.SetRenderTarget(tex);
            cmd.ClearRenderTarget(true, true, c);
            Graphics.ExecuteCommandBuffer(cmd);
            return tex;
        }

        private void DisposeTextures()
        {
            if (_safeTexture)
            {
                RenderTexture.ReleaseTemporary(_safeTexture);
                _safeTexture = null;
            }

            if (_notSafeTexture)
            {
                RenderTexture.ReleaseTemporary(_notSafeTexture);
                _notSafeTexture = null;
            }
            
            if (_ignoredTexture)
            {
                RenderTexture.ReleaseTemporary(_ignoredTexture);
                _ignoredTexture = null;
            }
        }

        private void OnDestroy()
        {
            DisposeTextures();
            CreatedSafeAreasStack.Remove(this);
        }
#endif
    }
}