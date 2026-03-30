using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCS.Migrations
{
    /// <inheritdoc />
    public partial class MigracionProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Almacenamientos",
                columns: table => new
                {
                    Id_almacenamiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_parte = table.Column<int>(type: "int", nullable: false),
                    Parte = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Numero_parte = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cantidad_Disponible = table.Column<int>(type: "int", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha_Ingreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fecha_Vencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Suplidor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Colateral = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Archivo = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Almacenamientos", x => x.Id_almacenamiento);
                });

            migrationBuilder.CreateTable(
                name: "Fabricantes",
                columns: table => new
                {
                    Id_suplidor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre_Suplidor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Contacto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Pais = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Condiciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Colateral = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Fecha_Registro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fabricantes", x => x.Id_suplidor);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id_permiso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombrePermiso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id_permiso);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id_roles = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id_roles);
                });

            migrationBuilder.CreateTable(
                name: "Perfiles",
                columns: table => new
                {
                    Id_perfiles = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_rol = table.Column<int>(type: "int", nullable: true),
                    User = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WWID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Apellidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Departamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Superior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contrasena = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confirmacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetTokenExpiration = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    RolesId_roles = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfiles", x => x.Id_perfiles);
                    table.ForeignKey(
                        name: "FK_Perfiles_Roles_Id_rol",
                        column: x => x.Id_rol,
                        principalTable: "Roles",
                        principalColumn: "Id_roles",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Perfiles_Roles_RolesId_roles",
                        column: x => x.RolesId_roles,
                        principalTable: "Roles",
                        principalColumn: "Id_roles");
                });

            migrationBuilder.CreateTable(
                name: "RolePermisos",
                columns: table => new
                {
                    Id_role_permiso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    PermisoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermisos", x => x.Id_role_permiso);
                    table.ForeignKey(
                        name: "FK_RolePermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "Permisos",
                        principalColumn: "Id_permiso",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id_roles",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Objetos",
                columns: table => new
                {
                    Id_parte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero_parte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fabricante = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cantidad_Disponible = table.Column<int>(type: "int", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stock_Minimo = table.Column<int>(type: "int", nullable: false),
                    Stock_Maximo = table.Column<int>(type: "int", nullable: false),
                    Fecha_Ingreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fecha_Vencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuplidorId = table.Column<int>(type: "int", nullable: false),
                    Archivo = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UsuariosId_perfiles = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objetos", x => x.Id_parte);
                    table.ForeignKey(
                        name: "FK_Objetos_Fabricantes_SuplidorId",
                        column: x => x.SuplidorId,
                        principalTable: "Fabricantes",
                        principalColumn: "Id_suplidor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Objetos_Perfiles_UsuariosId_perfiles",
                        column: x => x.UsuariosId_perfiles,
                        principalTable: "Perfiles",
                        principalColumn: "Id_perfiles");
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => new { x.UsuarioId, x.RolId });
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Perfiles_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Perfiles",
                        principalColumn: "Id_perfiles",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id_roles",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Egresos",
                columns: table => new
                {
                    Id_transaccion_eg = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_parte = table.Column<int>(type: "int", nullable: false),
                    Id_perfil = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Departamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numero_serial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fecha_salida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usuario_salida = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UsuariosId_perfiles = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Egresos", x => x.Id_transaccion_eg);
                    table.ForeignKey(
                        name: "FK_Egresos_Objetos_Id_parte",
                        column: x => x.Id_parte,
                        principalTable: "Objetos",
                        principalColumn: "Id_parte",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Egresos_Perfiles_UsuariosId_perfiles",
                        column: x => x.UsuariosId_perfiles,
                        principalTable: "Perfiles",
                        principalColumn: "Id_perfiles");
                });

            migrationBuilder.CreateTable(
                name: "Ingresos",
                columns: table => new
                {
                    Id_transaccion_ing = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_parte = table.Column<int>(type: "int", nullable: false),
                    Id_perfil = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Departamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numero_serial = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fecha_ingreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usuario_ingreso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UsuariosId_perfiles = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingresos", x => x.Id_transaccion_ing);
                    table.ForeignKey(
                        name: "FK_Ingresos_Objetos_Id_parte",
                        column: x => x.Id_parte,
                        principalTable: "Objetos",
                        principalColumn: "Id_parte",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ingresos_Perfiles_UsuariosId_perfiles",
                        column: x => x.UsuariosId_perfiles,
                        principalTable: "Perfiles",
                        principalColumn: "Id_perfiles");
                });

            migrationBuilder.CreateTable(
                name: "Entradas_Salidas",
                columns: table => new
                {
                    Id_ent_sal = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_perfil = table.Column<int>(type: "int", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo_movimiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha_salida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Fecha_entrada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Hora_salida = table.Column<TimeSpan>(type: "time", nullable: true),
                    Hora_entrada = table.Column<TimeSpan>(type: "time", nullable: true),
                    IngresosId_transaccion_ing = table.Column<int>(type: "int", nullable: true),
                    EgresosId_transaccion_eg = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entradas_Salidas", x => x.Id_ent_sal);
                    table.ForeignKey(
                        name: "FK_Entradas_Salidas_Egresos_EgresosId_transaccion_eg",
                        column: x => x.EgresosId_transaccion_eg,
                        principalTable: "Egresos",
                        principalColumn: "Id_transaccion_eg");
                    table.ForeignKey(
                        name: "FK_Entradas_Salidas_Ingresos_IngresosId_transaccion_ing",
                        column: x => x.IngresosId_transaccion_ing,
                        principalTable: "Ingresos",
                        principalColumn: "Id_transaccion_ing");
                    table.ForeignKey(
                        name: "FK_Entradas_Salidas_Perfiles_Id_perfil",
                        column: x => x.Id_perfil,
                        principalTable: "Perfiles",
                        principalColumn: "Id_perfiles",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id_movimientos = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_perfi = table.Column<int>(type: "int", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo_accion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha_accion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Hora_accion = table.Column<TimeSpan>(type: "time", nullable: true),
                    ParteId_parte = table.Column<int>(type: "int", nullable: true),
                    IngresosId_transaccion_ing = table.Column<int>(type: "int", nullable: true),
                    EgresosId_transaccion_eg = table.Column<int>(type: "int", nullable: true),
                    EntradasSalidasId_ent_sal = table.Column<int>(type: "int", nullable: true),
                    RolId_roles = table.Column<int>(type: "int", nullable: true),
                    SuplidorId_suplidor = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id_movimientos);
                    table.ForeignKey(
                        name: "FK_Movimientos_Egresos_EgresosId_transaccion_eg",
                        column: x => x.EgresosId_transaccion_eg,
                        principalTable: "Egresos",
                        principalColumn: "Id_transaccion_eg",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Entradas_Salidas_EntradasSalidasId_ent_sal",
                        column: x => x.EntradasSalidasId_ent_sal,
                        principalTable: "Entradas_Salidas",
                        principalColumn: "Id_ent_sal");
                    table.ForeignKey(
                        name: "FK_Movimientos_Fabricantes_SuplidorId_suplidor",
                        column: x => x.SuplidorId_suplidor,
                        principalTable: "Fabricantes",
                        principalColumn: "Id_suplidor");
                    table.ForeignKey(
                        name: "FK_Movimientos_Ingresos_IngresosId_transaccion_ing",
                        column: x => x.IngresosId_transaccion_ing,
                        principalTable: "Ingresos",
                        principalColumn: "Id_transaccion_ing",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Objetos_ParteId_parte",
                        column: x => x.ParteId_parte,
                        principalTable: "Objetos",
                        principalColumn: "Id_parte",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Perfiles_Id_perfi",
                        column: x => x.Id_perfi,
                        principalTable: "Perfiles",
                        principalColumn: "Id_perfiles",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Roles_RolId_roles",
                        column: x => x.RolId_roles,
                        principalTable: "Roles",
                        principalColumn: "Id_roles");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Egresos_Id_parte",
                table: "Egresos",
                column: "Id_parte");

            migrationBuilder.CreateIndex(
                name: "IX_Egresos_UsuariosId_perfiles",
                table: "Egresos",
                column: "UsuariosId_perfiles");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_Salidas_EgresosId_transaccion_eg",
                table: "Entradas_Salidas",
                column: "EgresosId_transaccion_eg");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_Salidas_Id_perfil",
                table: "Entradas_Salidas",
                column: "Id_perfil");

            migrationBuilder.CreateIndex(
                name: "IX_Entradas_Salidas_IngresosId_transaccion_ing",
                table: "Entradas_Salidas",
                column: "IngresosId_transaccion_ing");

            migrationBuilder.CreateIndex(
                name: "IX_Ingresos_Id_parte",
                table: "Ingresos",
                column: "Id_parte");

            migrationBuilder.CreateIndex(
                name: "IX_Ingresos_UsuariosId_perfiles",
                table: "Ingresos",
                column: "UsuariosId_perfiles");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_EgresosId_transaccion_eg",
                table: "Movimientos",
                column: "EgresosId_transaccion_eg");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_EntradasSalidasId_ent_sal",
                table: "Movimientos",
                column: "EntradasSalidasId_ent_sal");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Id_perfi",
                table: "Movimientos",
                column: "Id_perfi");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_IngresosId_transaccion_ing",
                table: "Movimientos",
                column: "IngresosId_transaccion_ing");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_ParteId_parte",
                table: "Movimientos",
                column: "ParteId_parte");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_RolId_roles",
                table: "Movimientos",
                column: "RolId_roles");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_SuplidorId_suplidor",
                table: "Movimientos",
                column: "SuplidorId_suplidor");

            migrationBuilder.CreateIndex(
                name: "IX_Objetos_SuplidorId",
                table: "Objetos",
                column: "SuplidorId");

            migrationBuilder.CreateIndex(
                name: "IX_Objetos_UsuariosId_perfiles",
                table: "Objetos",
                column: "UsuariosId_perfiles");

            migrationBuilder.CreateIndex(
                name: "IX_Perfiles_Id_rol",
                table: "Perfiles",
                column: "Id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_Perfiles_RolesId_roles",
                table: "Perfiles",
                column: "RolesId_roles");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermisos_PermisoId",
                table: "RolePermisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermisos_RolId",
                table: "RolePermisos",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RolId",
                table: "UsuarioRoles",
                column: "RolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Almacenamientos");

            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "RolePermisos");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "Entradas_Salidas");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Egresos");

            migrationBuilder.DropTable(
                name: "Ingresos");

            migrationBuilder.DropTable(
                name: "Objetos");

            migrationBuilder.DropTable(
                name: "Fabricantes");

            migrationBuilder.DropTable(
                name: "Perfiles");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
