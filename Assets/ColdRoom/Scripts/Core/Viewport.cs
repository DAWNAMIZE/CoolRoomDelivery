using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace ColdRoom.Core
{
    /// <summary>
    /// Tracks the browser CSS viewport width. 768 CSS pixels is the layout breakpoint.
    /// WebGL can't rely on Screen.width for this, because on high-DPI devices it may be
    /// the physical canvas size rather than the CSS viewport size.
    /// </summary>
    public static class Viewport
    {
        public const int CompactBreakpoint = 768;
        public const int NarrowBreakpoint = 1120;

        public static event Action<int> WidthChanged;

        public static int Width { get; private set; } = 1024;
        public static int Height { get; private set; } = 768;
        public static bool IsCompact => Width < CompactBreakpoint;
        public static bool Initialized { get; private set; }

        private static float _nextPoll;

        public static void Initialize()
        {
            Width = ReadWidth();
            Height = ReadHeight();
            Initialized = true;
        }

        public static void Poll(float unscaledTime)
        {
            if (!Initialized)
            {
                Initialize();
                WidthChanged?.Invoke(Width);
                return;
            }

            if (unscaledTime < _nextPoll)
            {
                return;
            }

            _nextPoll = unscaledTime + 0.2f;
            int width = ReadWidth();
            int height = ReadHeight();

            if (width == Width && height == Height)
            {
                return;
            }

            Width = width;
            Height = height;
            WidthChanged?.Invoke(width);
        }

        /// <summary>
        /// Panel units should map to CSS pixels. Unity's canvas may be backed by physical
        /// pixels, so scale the panel by Screen.width / CSS viewport width.
        /// </summary>
        public static float PanelScale
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                int cssWidth = ColdRoomBrowser.GetViewportWidth();
                if (cssWidth > 0)
                {
                    float ratio = Screen.width / (float)cssWidth;
                    if (ratio > 0.25f && ratio < 8f)
                    {
                        return ratio;
                    }
                }
#endif
                return 1f;
            }
        }

        private static int ReadWidth()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            int width = ColdRoomBrowser.GetViewportWidth();
            if (width > 0)
            {
                return width;
            }
#endif
            return Screen.width;
        }

        private static int ReadHeight()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            int height = ColdRoomBrowser.GetViewportHeight();
            if (height > 0)
            {
                return height;
            }
#endif
            return Screen.height;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static class ColdRoomBrowser
        {
            [DllImport("__Internal")]
            public static extern int GetViewportWidth();

            [DllImport("__Internal")]
            public static extern int GetViewportHeight();
        }
#endif
    }
}
