using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blazeditor.Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CellSize = table.Column<int>(type: "integer", nullable: false),
                    Size_Height = table.Column<int>(type: "integer", nullable: false),
                    Size_Width = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_Definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "Definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TilePalettes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CellSize = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilePalettes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TilePalettes_Definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "Definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Portals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationAreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationAreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Destination_Layer = table.Column<int>(type: "integer", nullable: false),
                    Destination_X = table.Column<int>(type: "integer", nullable: false),
                    Destination_Y = table.Column<int>(type: "integer", nullable: false),
                    Location_Layer = table.Column<int>(type: "integer", nullable: false),
                    Location_X = table.Column<int>(type: "integer", nullable: false),
                    Location_Y = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Portals_Areas_DestinationAreaId",
                        column: x => x.DestinationAreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Portals_Areas_LocationAreaId",
                        column: x => x.LocationAreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Portals_Definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "Definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TileMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Layer = table.Column<int>(type: "integer", nullable: false),
                    TilePlacements = table.Column<string>(type: "jsonb", nullable: false),
                    Size_Height = table.Column<int>(type: "integer", nullable: false),
                    Size_Width = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TileMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TileMaps_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AreaTilePalettes",
                columns: table => new
                {
                    AreasId = table.Column<Guid>(type: "uuid", nullable: false),
                    TilePalettesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaTilePalettes", x => new { x.AreasId, x.TilePalettesId });
                    table.ForeignKey(
                        name: "FK_AreaTilePalettes_Areas_AreasId",
                        column: x => x.AreasId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AreaTilePalettes_TilePalettes_TilePalettesId",
                        column: x => x.TilePalettesId,
                        principalTable: "TilePalettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TilePaletteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Image = table.Column<string>(type: "text", nullable: false),
                    FloorProperties_Impedance = table.Column<float>(type: "real", nullable: true),
                    ShimProperties_ShimType = table.Column<int>(type: "integer", nullable: true),
                    Size_Height = table.Column<int>(type: "integer", nullable: false),
                    Size_Width = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tiles_TilePalettes_TilePaletteId",
                        column: x => x.TilePaletteId,
                        principalTable: "TilePalettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_DefinitionId",
                table: "Areas",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaTilePalettes_TilePalettesId",
                table: "AreaTilePalettes",
                column: "TilePalettesId");

            migrationBuilder.CreateIndex(
                name: "IX_Portals_DefinitionId",
                table: "Portals",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Portals_DestinationAreaId",
                table: "Portals",
                column: "DestinationAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Portals_LocationAreaId",
                table: "Portals",
                column: "LocationAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_TileMaps_AreaId",
                table: "TileMaps",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_TilePalettes_DefinitionId",
                table: "TilePalettes",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_TilePaletteId",
                table: "Tiles",
                column: "TilePaletteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AreaTilePalettes");

            migrationBuilder.DropTable(
                name: "Portals");

            migrationBuilder.DropTable(
                name: "TileMaps");

            migrationBuilder.DropTable(
                name: "Tiles");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "TilePalettes");

            migrationBuilder.DropTable(
                name: "Definitions");
        }
    }
}
