using Prism.Mvvm;

namespace GE2D3D.MapEditor.Modules.SceneViewer.Inspectors
{
    // Simple float editor VM ? no Gemini, no inspector framework
    public class FloatEditorViewModel : BindableBase
    {
        private float _value;

        public float Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}