using Blazeditor.Application.Models;
using Blazeditor.Application.Services;
using Microsoft.AspNetCore.Components;

namespace Blazeditor.Application.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase
    {

        protected override void OnInitialized()
        {
            if (Definition != null && !Definition.GetAreas().Any())
            {
                var home = Definition.AddArea("Little Town", "Where the character starts.");
                var dungeon = Definition.AddArea("Dungeon", "Where the monsters are.");
                var forest = Definition.AddArea("Forest", "Where the trees are.");
                forest.TileMaps[0] = new TileMap("Forest Map", "A dense forest with many trees.", 12, 12, 0);

                Definition.AddPortal(home, dungeon, 0, 0, 1, 1);
                Definition.AddPortal(dungeon, forest, 1, 1, 2, 2);
                Definition.AddPortal(forest, home, 2, 2, 0, 0);
            }
            base.OnInitialized();
        }
    }
}