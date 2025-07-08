using Blazeditor.Application.Components.Pages;
using Blazeditor.Application.Models;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace Blazeditor.Application.Services
{
    public class DefinitionManager
    {
        public DefinitionManager()
        {
            Console.WriteLine("Creating DefinitionManager.");
        }
        private Definition _definition = new Definition();
        public Area? SelectedArea { get; set; }

        public void AddPortal(Area destinationArea, Area locationArea, int destX, int destY, int locX, int locY)
        {
            if (destinationArea == null) throw new ArgumentNullException(nameof(destinationArea));
            if (locationArea == null) throw new ArgumentNullException(nameof(locationArea));
            var portal = new Portal(destinationArea, locationArea, destX, destY, locX, locY);
            AddPortal(portal);
        }

        public void AddPortal(Portal portal)
        {
            if (portal == null) throw new ArgumentNullException(nameof(portal));
            _definition.Portals.Add(portal);
        }
        public void RemovePortal(int index)
        {
            if (index < 0 || index >= _definition.Portals.Count) throw new ArgumentOutOfRangeException(nameof(index));
            _definition.Portals.RemoveAt(index);
        }
        public IEnumerable<Portal> GetPortals()
        {
            return _definition.Portals;
        }

        public Area AddArea(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be null or empty.", nameof(description));
            var area = new Area(name, description);
            return AddArea(area);
        }

        public Area AddArea(Area area)
        {
            if (area == null) throw new ArgumentNullException(nameof(area));
            if (SelectedArea == null)
            {
                SelectedArea = area;
            }
            _definition.Areas[area.Id] = area;
            return area;
        }
        public void RemoveArea(int id)
        {
            if (_definition.Areas.ContainsKey(id))
            {
                _definition.Areas.Remove(id);
                if (SelectedArea?.Id == id)
                {
                    SelectedArea = _definition.Areas.Values.FirstOrDefault();
                }
            }
        }
        public IEnumerable<Area> GetAreas()
        {
            return _definition.Areas.Values;
        }


        public List<string?> GetTileImageFilenames()
        {
            var imageDirectory = Path.Combine("wwwroot", "tilesets");
            if (!Directory.Exists(imageDirectory)) return new List<string?>();
            return Directory.GetFiles(imageDirectory, "*.png")
                .Select(Path.GetFileName)
                .Cast<string?>()
                .ToList();
        }

        public List<Tile> AddTilePaletteToArea(Area area, string filename)
        {
            if (area == null) throw new ArgumentNullException(nameof(area));
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("Filename cannot be null or empty.", nameof(filename));
            var tiles = ExtractTilesFromImage(filename);
            foreach (var tile in tiles)
            {
                area.TilePalette.Add(tile);
            }
            return tiles;
        }

        public List<Tile> ExtractTilesFromImage(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("Filename cannot be null or empty.", nameof(filename));
            var baseName = Path.GetFileNameWithoutExtension(filename);
            var imagePath = Path.Combine("wwwroot", "tilesets", baseName + ".png");
            var jsonPath = Path.Combine("wwwroot", "tilesets", baseName + ".json");

            if (!File.Exists(imagePath)) throw new FileNotFoundException($"Image file not found: {imagePath}");
            if (!File.Exists(jsonPath)) throw new FileNotFoundException($"JSON file not found: {jsonPath}");

            var json = File.ReadAllText(jsonPath);
            var doc = JsonDocument.Parse(json);
            var tiles = doc.RootElement.GetProperty("tiles");
            var cellSizeVal = doc.RootElement.GetProperty("cellSize").GetInt32();
            var cellSize = new Blazeditor.Application.Models.Size
            {
                Width = cellSizeVal,
                Height = cellSizeVal
            };

            var result = new List<Tile>();
            using (var image = Image.Load<Rgba32>(imagePath))
            {
                foreach (var tileElem in tiles.EnumerateArray())
                {
                    string name = tileElem.GetProperty("name").GetString() ?? "tile";
                    string description = tileElem.GetProperty("description").GetString() ?? string.Empty;
                    int w = tileElem.GetProperty("w").GetInt32();
                    int h = tileElem.GetProperty("h").GetInt32();
                    int x = tileElem.GetProperty("x").GetInt32();
                    int y = tileElem.GetProperty("y").GetInt32();

                    var rect = new SixLabors.ImageSharp.Rectangle(
                        x * cellSize.Width,
                        y * cellSize.Height,
                        w * cellSize.Width,
                        h * cellSize.Height
                    );

                    using (var tileImg = image.Clone(ctx => ctx.Crop(rect)))
                    using (var ms = new MemoryStream())
                    {
                        tileImg.Save(ms, new PngEncoder());
                        var base64 = Convert.ToBase64String(ms.ToArray());
                        var base64Url = $"data:image/png;base64,{base64}";
                        result.Add(new Tile(name, description, "default", base64Url, new Blazeditor.Application.Models.Size(w, h)));
                    }
                }
            }
            return result;
        }
    }
}
