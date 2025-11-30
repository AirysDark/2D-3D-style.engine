using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GE2D3D.MapEditor.Components.Camera;
using GE2D3D.MapEditor.Data.Models;
using GE2D3D.MapEditor.Primitives;

namespace GE2D3D.MapEditor.Components.ModelSelector
{
    public abstract class BaseModelSelector : IGameComponent, IUpdateable
    {
        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnEnabledChanged();
                }
            }
        }

        private int _updateOrder;
        public int UpdateOrder
        {
            get => _updateOrder;
            set
            {
                if (_updateOrder != value)
                {
                    _updateOrder = value;
                    OnUpdateOrderChanged();
                }
            }
        }

        public event EventHandler<EventArgs>? EnabledChanged;
        public event EventHandler<EventArgs>? UpdateOrderChanged;

        /// <summary>
        /// Currently selected model. Can be null if nothing is selected.
        /// </summary>
        public BaseModel? SelectedModel { get; protected set; }

        /// <summary>
        /// Distance along the pick ray at which the model was hit.
        /// Used to determine "closest" hit.
        /// </summary>
        protected float SelectedModelDistance { get; set; }

        protected BaseCamera Camera { get; }

        protected CubePrimitive Cube { get; }

        /// <summary>
        /// Raised whenever the selection changes (including clearing selection).
        /// </summary>
        public event EventHandler<BaseModel?>? SelectedModelChanged;

        protected BaseModelSelector(BaseCamera camera)
        {
            Camera = camera;
            Cube = new CubePrimitive();
        }

        public abstract void Initialize();

        public virtual void Update(GameTime gameTime)
        {
            // Default: no-op
        }

        /// <summary>
        /// Default selection highlight ? draws a lime-green semi-transparent
        /// bounding cube around SelectedModel, if any.
        /// Derived classes should call base.Draw(basicEffect) if they override.
        /// </summary>
        public virtual void Draw(BasicEffect basicEffect)
        {
            if (SelectedModel is not null)
            {
                Cube.Model = SelectedModel;
                Cube.Recalc();
                Cube.Draw(basicEffect, new Color(Color.LimeGreen, 0.75f));
            }
        }

        /// <summary>
        /// Helper for derived classes to set selection and fire the event only when it changes.
        /// </summary>
        protected void SetSelection(BaseModel? model, float distance)
        {
            SelectedModelDistance = distance;

            if (!ReferenceEquals(SelectedModel, model))
            {
                SelectedModel = model;
                SelectedModelChanged?.Invoke(this, SelectedModel);
            }
        }

        protected virtual void OnUpdateOrderChanged()
            => UpdateOrderChanged?.Invoke(this, EventArgs.Empty);

        protected virtual void OnEnabledChanged()
            => EnabledChanged?.Invoke(this, EventArgs.Empty);
    }
}