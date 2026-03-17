using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RescateAcademico.Migrations
{
    /// <inheritdoc />
    public partial class ModeladoBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alumnos",
                columns: table => new
                {
                    Matricula = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Apellidos = table.Column<string>(type: "TEXT", nullable: false),
                    Carrera = table.Column<string>(type: "TEXT", nullable: false),
                    PromedioGlobal = table.Column<decimal>(type: "TEXT", nullable: false),
                    SemestreActual = table.Column<int>(type: "INTEGER", nullable: false),
                    RiesgoAcademico = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alumnos", x => x.Matricula);
                    table.ForeignKey(
                        name: "FK_Alumnos_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Proyectos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    CupoMaximo = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstaActivo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Postulaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlumnoId = table.Column<string>(type: "TEXT", nullable: false),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postulaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Postulaciones_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Matricula",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Postulaciones_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alumnos_UserId",
                table: "Alumnos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_AlumnoId",
                table: "Postulaciones",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_ProyectoId",
                table: "Postulaciones",
                column: "ProyectoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Postulaciones");

            migrationBuilder.DropTable(
                name: "Alumnos");

            migrationBuilder.DropTable(
                name: "Proyectos");
        }
    }
}
