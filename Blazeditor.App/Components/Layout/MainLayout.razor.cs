using Blazeditor.App.Models;
using Blazeditor.App.Services;
using Microsoft.AspNetCore.Components;

namespace Blazeditor.App.Components.Layout
{

    public partial class MainLayout
    {
        protected override void OnInitialized()
        {
            if (!Definition.GetAreas().Any())
            {
                var home = Definition.AddArea("Little Town", "Where the character starts.");
                var dungeon = Definition.AddArea("Dungeon", "Where the monsters are.");
                var forest = Definition.AddArea("Forest", "Where the trees are.");

                Definition.AddPortal(home, dungeon, 0, 0, 1, 1);
                Definition.AddPortal(dungeon, forest, 1, 1, 2, 2);
                Definition.AddPortal(forest, home, 2, 2, 0, 0);

            }
            base.OnInitialized();
        }

        protected override Task OnInitializedAsync()
        {
            // This is where you can perform any asynchronous initialization logic.
            // For example, you might want to load data from a service.
            // Here we are just calling the base method, but you can add your own logic.

            return base.OnInitializedAsync();
        }
    }
}