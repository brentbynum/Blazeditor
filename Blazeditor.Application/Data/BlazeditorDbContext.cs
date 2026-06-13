using System.Text.Json;
using Blazeditor.Application.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TilePlacement = Blazeditor.Application.Models.TilePlacement;

namespace Blazeditor.Application.Data;

public class BlazeditorDbContext(DbContextOptions<BlazeditorDbContext> options) : DbContext(options)
{
    public DbSet<Definition> Definitions => Set<Definition>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<TileMap> TileMaps => Set<TileMap>();
    public DbSet<TilePalette> TilePalettes => Set<TilePalette>();
    public DbSet<Tile> Tiles => Set<Tile>();
    public DbSet<Portal> Portals => Set<Portal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Complex (value-object) types: mapped as columns on the owning table ---
        modelBuilder.Entity<Area>().ComplexProperty(a => a.Size);
        modelBuilder.Entity<TileMap>().ComplexProperty(t => t.Size);
        modelBuilder.Entity<Tile>().ComplexProperty(t => t.Size);
        modelBuilder.Entity<Tile>().ComplexProperty(t => t.FloorProperties, b => b.IsRequired(false));
        modelBuilder.Entity<Tile>().ComplexProperty(t => t.ShimProperties, b => b.IsRequired(false));
        modelBuilder.Entity<Portal>().ComplexProperty(p => p.Destination);
        modelBuilder.Entity<Portal>().ComplexProperty(p => p.Location);

        // --- Definition owns Areas / TilePalettes / Portals (cascade delete) ---
        modelBuilder.Entity<Area>()
            .HasOne(a => a.Definition).WithMany(d => d.Areas)
            .HasForeignKey(a => a.DefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TilePalette>()
            .HasOne(p => p.Definition).WithMany(d => d.TilePalettes)
            .HasForeignKey(p => p.DefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Portal>()
            .HasOne(p => p.Definition).WithMany(d => d.Portals)
            .HasForeignKey(p => p.DefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Area owns TileMaps (cascade delete) ---
        modelBuilder.Entity<TileMap>()
            .HasOne(t => t.Area).WithMany(a => a.TileMaps)
            .HasForeignKey(t => t.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- TilePalette owns Tiles (cascade delete) ---
        modelBuilder.Entity<Tile>()
            .HasOne(t => t.TilePalette).WithMany(p => p.Tiles)
            .HasForeignKey(t => t.TilePaletteId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Area <-> TilePalette many-to-many ---
        modelBuilder.Entity<Area>()
            .HasMany(a => a.TilePalettes)
            .WithMany(p => p.Areas)
            .UsingEntity(j => j.ToTable("AreaTilePalettes"));

        // --- Portal -> Area references: don't cascade-delete portals when an area is removed ---
        modelBuilder.Entity<Portal>()
            .HasOne<Area>().WithMany()
            .HasForeignKey(p => p.DestinationAreaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Portal>()
            .HasOne<Area>().WithMany()
            .HasForeignKey(p => p.LocationAreaId)
            .OnDelete(DeleteBehavior.SetNull);

        // --- TileMap.TilePlacements as jsonb (no FK enforcement on TileId/PaletteId inside) ---
        var placementsConverter = new ValueConverter<TilePlacement?[], string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<TilePlacement?[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<TilePlacement?>());

        var placementsComparer = new ValueComparer<TilePlacement?[]>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
            v => JsonSerializer.Deserialize<TilePlacement?[]>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);

        modelBuilder.Entity<TileMap>()
            .Property(t => t.TilePlacements)
            .HasColumnType("jsonb")
            .HasConversion(placementsConverter)
            .Metadata.SetValueComparer(placementsComparer);
    }
}
