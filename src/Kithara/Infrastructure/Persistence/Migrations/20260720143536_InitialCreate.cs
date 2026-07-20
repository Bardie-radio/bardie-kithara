using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kithara.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ManagedByModuleSlug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    GuestStrunaId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "search_result_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrincipalUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModuleSlug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    QueryKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ResultsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_result_cache", x => x.Id);
                    table.ForeignKey(
                        name: "FK_search_result_cache_users_PrincipalUserId",
                        column: x => x.PrincipalUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strunas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PlaybackAccess = table.Column<int>(type: "INTEGER", nullable: false),
                    ControlAccess = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ListenToken = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    GuestCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strunas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_strunas_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tunes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModuleSlug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Artist = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    DurationSeconds = table.Column<double>(type: "REAL", nullable: true),
                    ArtworkUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    StorageKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tunes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tunes_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_auth_bindings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderSlug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExternalSubject = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_auth_bindings", x => new { x.UserId, x.ProviderSlug });
                    table.ForeignKey(
                        name: "FK_user_auth_bindings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "struna_control_grants",
                columns: table => new
                {
                    StrunaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_struna_control_grants", x => new { x.StrunaId, x.UserId });
                    table.ForeignKey(
                        name: "FK_struna_control_grants_strunas_StrunaId",
                        column: x => x.StrunaId,
                        principalTable: "strunas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_struna_control_grants_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "queue_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StrunaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TuneId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queue_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_queue_entries_strunas_StrunaId",
                        column: x => x.StrunaId,
                        principalTable: "strunas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_queue_entries_tunes_TuneId",
                        column: x => x.TuneId,
                        principalTable: "tunes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_queue_entries_StrunaId_Position",
                table: "queue_entries",
                columns: new[] { "StrunaId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_queue_entries_TuneId",
                table: "queue_entries",
                column: "TuneId");

            migrationBuilder.CreateIndex(
                name: "IX_search_result_cache_PrincipalUserId_ModuleSlug_QueryKey",
                table: "search_result_cache",
                columns: new[] { "PrincipalUserId", "ModuleSlug", "QueryKey" });

            migrationBuilder.CreateIndex(
                name: "IX_struna_control_grants_UserId",
                table: "struna_control_grants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_strunas_OwnerUserId",
                table: "strunas",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_strunas_Slug",
                table: "strunas",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tunes_CreatedByUserId",
                table: "tunes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tunes_ModuleSlug_ExternalId",
                table: "tunes",
                columns: new[] { "ModuleSlug", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Kind",
                table: "users",
                column: "Kind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "queue_entries");

            migrationBuilder.DropTable(
                name: "search_result_cache");

            migrationBuilder.DropTable(
                name: "struna_control_grants");

            migrationBuilder.DropTable(
                name: "user_auth_bindings");

            migrationBuilder.DropTable(
                name: "tunes");

            migrationBuilder.DropTable(
                name: "strunas");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
