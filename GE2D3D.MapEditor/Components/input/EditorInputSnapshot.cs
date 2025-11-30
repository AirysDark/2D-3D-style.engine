using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Components.Input
{
    /// <summary>
    /// Device-agnostic snapshot of the editor input.
    /// Works with both WPF and MonoGame.
    /// 
    /// IMPORTANT:
    ///  - All fields are non-nullable
    ///  - Designed to support camera orbiting, looking, gizmo interaction,
    ///    camera path recording, grid snapping, etc.
    /// </summary>
    public class EditorInputSnapshot
    {
        // ------------------------------------------------------------
        // MOUSE STATE
        // ------------------------------------------------------------
        public Point MousePosition { get; set; }
        public Point MouseDelta { get; set; }

        public bool LeftButtonDown { get; set; }
        public bool MiddleButtonDown { get; set; }
        public bool RightButtonDown { get; set; }

        /// <summary>
        /// Signed wheel movement since last frame.
        /// </summary>
        public float MouseWheelDelta { get; set; }

        // ------------------------------------------------------------
        // KEYBOARD MOVEMENT (WASD + vertical controls)
        // ------------------------------------------------------------
        public bool KeyForward { get; set; }    // W
        public bool KeyBackward { get; set; }   // S
        public bool KeyLeft { get; set; }       // A
        public bool KeyRight { get; set; }      // D

        public bool KeyUp { get; set; }         // E / Space
        public bool KeyDown { get; set; }       // Q / Ctrl

        // ------------------------------------------------------------
        // CAMERA INTERACTION MODIFIERS
        // ------------------------------------------------------------

        /// <summary>
        /// Hold ALT for orbiting camera in WPF.
        /// </summary>
        public bool KeyAlt { get; set; }

        /// <summary>
        /// Hold SHIFT for slow/precision movement.
        /// </summary>
        public bool KeyShift { get; set; }

        /// <summary>
        /// Hold CTRL to temporarily disable snapping or enable alternate modes.
        /// </summary>
        public bool KeyCtrl { get; set; }

        // ------------------------------------------------------------
        // EDITOR FEATURES
        // ------------------------------------------------------------

        /// <summary>
        /// Toggle grid snapping (Ctrl or hotkey).
        /// </summary>
        public bool KeySnapToggle { get; set; }

        /// <summary>
        /// Start/stop camera path recording.
        /// </summary>
        public bool KeyRecordCameraPath { get; set; }

        /// <summary>
        /// Play the recorded path (flythrough).
        /// </summary>
        public bool KeyPlayCameraPath { get; set; }

        /// <summary>
        /// Hotkey to "Focus on Selection" (e.g. F key).
        /// </summary>
        public bool KeyFocus { get; set; }

        /// <summary>
        /// Reset camera (e.g. Home key).
        /// </summary>
        public bool KeyResetCamera { get; set; }

        // ------------------------------------------------------------
        // SELECTION INPUT
        // ------------------------------------------------------------

        /// <summary>
        /// Did the user click this frame? Helps gizmo picking.
        /// </summary>
        public bool ClickThisFrame { get; set; }

        public EditorInputSnapshot() { }
    }
}