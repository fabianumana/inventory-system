using FluentEmail.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCS.Models;
using SCS.Services;
using SCS.ViewModels;
using System.Security.Claims;

namespace SCS.Controllers
{
    public class CuentaController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly IEmailSender _emailSender;
        private readonly BitacorasService _movimientoService;

        public CuentaController(IDbContextFactory<Service> contextFactory, IEmailSender emailSender, BitacorasService movimientoService)
        {
            _contextFactory = contextFactory;
            _emailSender = emailSender;
            _movimientoService = movimientoService;
        }

        //Metodo GET para solicitar cambio de contraseña
        [HttpGet]
        public IActionResult ContraseñaOlvidada()
        {
            return View();

        }

        //Metodo contraseña unica
        private async Task<bool> ContrasenaUnica(string password)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var passwordHasher = new PasswordHasher<Usuarios>();
            var hashedPassword = passwordHasher.HashPassword(null, password);

            var exists = await dbContext.Perfiles
                .AnyAsync(p => p.Contrasena == hashedPassword);

            return !exists;
        }

        //Metodo POST para solicitar cambio de contraseña
        [HttpPost]
        public async Task<IActionResult> ContraseñaOlvidada(ContraseñaOlvidadaVM model)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await dbContext.Perfiles.SingleOrDefaultAsync(u => u.Correo == model.Email);
            if (user == null)
            {
                return RedirectToAction("ConfirmacionContraseña");
            }

            var token = Guid.NewGuid().ToString();
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(1);
            await dbContext.SaveChangesAsync();

            var callbackUrl = Url.Action("ResetearContraseña", "Cuenta", new { token }, Request.Scheme);

            var emailBody = $@"
                <p>Por favor, recupera tu contraseña haciendo clic en el siguiente enlace:</p>
                <p><a href='{callbackUrl}' target='_self'>Cambiar Contraseña</a></p>
                <p>Si no solicitaste este cambio, ignora este mensaje.</p>
                <p><strong>Nota:</strong> Una vez que accedas al enlace, por favor modifica la contraseña y cierra la ventana!</p>";

            await _emailSender.SendEmailAsync(user.Correo, "Recupera tu contraseña", emailBody);

            return RedirectToAction("ConfirmacionContraseña", "Cuenta");
        }

        public IActionResult ConfirmacionContraseña()
        {
            return View();
        }

        //Metodo GET de resetear mi contraseña
        [HttpGet]
        public IActionResult ResetearContraseña(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token no válido.");
            }

            var model = new ResetearVM { Token = token };
            return View(model);
        }

        //Metodo POST de resetear mi contraseña
        [HttpPost]
        public async Task<IActionResult> ResetearContraseña(ResetearVM model)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await dbContext.Perfiles.SingleOrDefaultAsync(u => u.PasswordResetToken == model.Token && u.PasswordResetTokenExpiration > DateTime.UtcNow);

            if (user == null)
            {
                TempData["ErrorMessage"] = "El token de restablecimiento de contraseña es inválido o ha expirado.";
                return RedirectToAction("ErrorTokenExpirado", "Cuenta");
            }

            if (!await ContrasenaUnica(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "La contraseña ya está en uso. Por favor, elige una diferente.");
                return View(model);
            }

            var passwordHasher = new PasswordHasher<Usuarios>();

            string hashedPassword = passwordHasher.HashPassword(user, model.Password);
            user.Contrasena = hashedPassword;
            user.Confirmacion = hashedPassword;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiration = null;
            await dbContext.SaveChangesAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _movimientoService.RegistrarMovimientoAsync(
                    userId,
                    user.User,
                    "Restablecimiento de contraseña",
                    $"El usuario con correo {user.Correo} restableció su contraseña.",
                    fechaAccion,
                    horaAccion
                );
            }

            return RedirectToAction("ConfirmacionReseteo", "Cuenta");
        }

        //Metodo de confirmacion de reseteo
        public IActionResult ConfirmacionReseteo()
        {
            return View();
        }
    }
}
