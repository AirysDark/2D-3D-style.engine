
namespace GE2D3D.MapEditor.Runtime.Components
{
    /// <summary>
    /// Links a runtime entity to a map/model resource.
    /// </summary>
    public class RenderComponent : IRuntimeComponent
    {
        public int ModelId;
        public string? TexturePath;
        public bool Visible = true;
    }
}
