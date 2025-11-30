using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GE2D3D.MapEditor.Components.Camera;
using GE2D3D.MapEditor.Data.Models;
using GE2D3D.MapEditor.Renders;

namespace GE2D3D.MapEditor.Components.ModelSelector
{
    /// <summary>
    /// Default model selector:
    /// - Ray picks against BaseModelListRenderer.TotalModels.
    /// - Supports external selection (e.g. from WPF tree) via SelectByEntityId.
    /// - When LockSelectionFromExternal is true, Update() won't override the selection.
    /// </summary>
    public class ModelSelectorDefault : BaseModelSelector
    {
        /// <summary>
        /// If true, Update() won't immediately overwrite a programmatic selection.
        /// This lets WPF or tools call SelectByEntityId and keep that selection
        /// until the user explicitly clears it or you clear it when the viewport is clicked.
        /// </summary>
        public bool LockSelectionFromExternal { get; set; }

        public ModelSelectorDefault(BaseCamera camera)
            : base(camera)
        {
        }

        public override void Initialize()
        {
            // Hook input or services here if needed.
        }

        public override void Update(GameTime gameTime)
        {
            // If selection is "locked" by external code (e.g. WPF tree),
            // skip ray-based picking for this frame.
            if (LockSelectionFromExternal)
                return;

            SetSelection(null, float.MaxValue);

            var models = BaseModelListRenderer.TotalModels;
            if (models == null || models.Count == 0)
                return;

            var ray = Camera.GetMouseRay();

            foreach (var model in models.Where(m => m?.Entity?.Visible == true))
            {
                var hit = model!.BoundingBox.Intersects(ray);
                if (hit.HasValue && hit.Value < SelectedModelDistance)
                {
                    // Use the helper so events fire correctly
                    SetSelection(model, hit.Value);
                }
            }
        }

        /// <summary>
        /// Programmatic selection from editor/WPF code.
        /// Example: tree view node click -> SelectByEntityId(entity.Id).
        /// </summary>
        public void SelectByEntityId(int entityId, bool lockSelection = true)
        {
            var models = BaseModelListRenderer.TotalModels;
            if (models == null || models.Count == 0)
                return;

            var match = models.FirstOrDefault(m => m?.Entity != null && m.Entity.ID == entityId);
            if (match != null)
            {
                SetSelection(match, 0f);
                LockSelectionFromExternal = lockSelection;
            }
        }

        /// <summary>
        /// Clears the external selection lock so ray picking can take over again.
        /// Call this when the user clicks into the viewport, for example.
        /// </summary>
        public void ClearExternalSelectionLock()
        {
            LockSelectionFromExternal = false;
        }

        /// <summary>
        /// Draw selection highlight and (later) gizmos.
        /// Currently calls base.Draw to keep the bounding box outline.
        /// </summary>
        public override void Draw(BasicEffect basicEffect)
        {
            // Keep the original lime-green bounding cube highlight
            base.Draw(basicEffect);

            // TODO: add move/rotate/scale gizmos for SelectedModel here if desired.
        }
    }
}