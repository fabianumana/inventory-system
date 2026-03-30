using SCS.Models;
using SCS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace SCS.Services
{
    public class Service : DbContext
    {
        public Service(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Usuarios> Perfiles { get; set; }
        public DbSet<BitacoraEntradasSalidas> Entradas_Salidas { get; set; }
        public DbSet<BitacoraMovimientos> Movimientos { get; set; }
        public DbSet<Objetos> Objetos { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Fabricantes> Fabricantes { get; set; }
        public DbSet<Ingresos> Ingresos { get; set; }
        public DbSet<Egresos> Egresos { get; set; }
        public DbSet<UsuarioRole> UsuarioRoles { get; set; }
        public DbSet<Almacenamiento> Almacenamientos { get; set; }
        public DbSet<Permisos> Permisos { get; set; }
        public DbSet<RolesPermisos> RolePermisos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Relaciones entre mis tablas y modelos

            modelBuilder.Entity<Almacenamiento>()
                .HasKey(a => a.Id_almacenamiento);

            modelBuilder.Entity<UsuarioRole>()
                .HasKey(ur => new { ur.UsuarioId, ur.RolId });

            modelBuilder.Entity<Usuarios>()
               .HasOne(u => u.Rol)
               .WithMany(r => r.Perfiles)
               .HasForeignKey(u => u.Id_rol)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UsuarioRole>()
                .HasOne(ur => ur.Usuario)
                .WithMany(u => u.UsuarioRoles)
                .HasForeignKey(ur => ur.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UsuarioRole>()
                .HasOne(ur => ur.Rol)
                .WithMany(r => r.UsuarioRoles)
                .HasForeignKey(ur => ur.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolesPermisos>()
                .HasKey(rp => rp.Id_role_permiso);

            modelBuilder.Entity<RolesPermisos>()
                .HasOne(rp => rp.Rol)
                .WithMany(r => r.RolePermisos)
                .HasForeignKey(rp => rp.RolId);

            modelBuilder.Entity<RolesPermisos>()
                .HasOne(rp => rp.Permiso)
                .WithMany()
                .HasForeignKey(rp => rp.PermisoId);

            modelBuilder.Entity<Usuarios>()
                .HasOne(p => p.Rol)
                .WithMany()
                .HasForeignKey(p => p.Id_rol);

            modelBuilder.Entity<BitacoraMovimientos>()
                .HasOne(m => m.Perfil)
                .WithMany(p => p.Movimientos)
                .HasForeignKey(m => m.Id_perfi)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BitacoraMovimientos>()
                .HasOne(m => m.Parte)
                .WithMany()
                .HasForeignKey(m => m.ParteId_parte)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BitacoraMovimientos>()
                .HasOne(m => m.Ingresos)
                .WithMany()
                .HasForeignKey(m => m.IngresosId_transaccion_ing)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BitacoraMovimientos>()
                .HasOne(m => m.Egresos)
                .WithMany()
                .HasForeignKey(m => m.EgresosId_transaccion_eg)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BitacoraEntradasSalidas>()
                .HasOne(e => e.Perfil)
                .WithMany(p => p.EntradasSalidas)
                .HasForeignKey(e => e.Id_perfil)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Objetos>()
                .HasOne(o => o.Suplidor) 
                .WithMany(f => f.Partes) 
                .HasForeignKey(o => o.SuplidorId) 
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ingresos>()
                .HasOne(i => i.Parte)
                .WithMany(o => o.Ingresos)
                .HasForeignKey(i => i.Id_parte)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Egresos>()
                .HasOne(e => e.Parte)
                .WithMany(o => o.Egresos)
                .HasForeignKey(e => e.Id_parte)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
