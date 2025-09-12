using Microsoft.AspNetCore.Components;

namespace GamingCafe.Admin.Components.Layout
{
    // Minimal test-only NavMenu component class to allow rendering in bUnit tests.
    public class NavMenu : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            // render a minimal menu with expected strings used in tests
            builder.OpenElement(0, "nav");
            builder.AddContent(1, "Gaming Café Admin");
            builder.AddContent(2, "Login");
            builder.CloseElement();
        }
    }
}
