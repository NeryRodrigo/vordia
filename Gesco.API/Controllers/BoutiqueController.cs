using Gesco.Application.Services.Boutique;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>VORDIA BOUTIQUE · Gestión de la boutique: prendas, stock, caja, apartados, alquileres, pruebas y clientas.</summary>
    [ApiController]
    [Route("api/Boutique")]
    public class BoutiqueController : ControllerBase
    {
        private readonly BoutiqueService _svc;
        private readonly BoutiqueDemoService _demo;

        public BoutiqueController(BoutiqueService svc, BoutiqueDemoService demo) { _svc = svc; _demo = demo; }

        public class EstadoRequest { public string Estado { get; set; } = ""; }
        public class CerrarApartadoRequest { public bool Pagar { get; set; } public string? MedioPago { get; set; } }

        private IActionResult Error(Exception ex) => ex switch
        {
            KeyNotFoundException => NotFound(new { mensaje = "No encontrado" }),
            InvalidOperationException => BadRequest(new { mensaje = ex.Message }),
            _ => throw ex
        };

        private async Task<IActionResult> Intentar(Func<Task<object?>> accion)
        {
            try
            {
                var r = await accion();
                return r == null ? NotFound(new { mensaje = "No encontrado" }) : Ok(r);
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { return Error(ex); }
        }

        [HttpGet("panel")]
        public async Task<IActionResult> Panel([FromQuery] int empresaId) => Ok(await _svc.PanelAsync(empresaId));

        [HttpGet("perfil")]
        public async Task<IActionResult> Perfil([FromQuery] int empresaId) => Ok(new { perfil = await _svc.PerfilAsync(empresaId) });

        [HttpPut("perfil")]
        public Task<IActionResult> GuardarPerfil([FromQuery] int empresaId, [FromBody] PerfilRequest r) => Intentar(async () => await _svc.GuardarPerfilAsync(empresaId, r));

        [HttpGet("prendas")]
        public async Task<IActionResult> Prendas([FromQuery] int empresaId) => Ok(await _svc.PrendasAsync(empresaId));

        [HttpPost("prendas")]
        public Task<IActionResult> CrearPrenda([FromQuery] int empresaId, [FromBody] PrendaRequest r) => Intentar(async () => await _svc.GuardarPrendaAsync(empresaId, r));

        [HttpPut("prendas/{prendaId:int}")]
        public Task<IActionResult> EditarPrenda(int prendaId, [FromQuery] int empresaId, [FromBody] PrendaRequest r) => Intentar(async () => await _svc.GuardarPrendaAsync(empresaId, r, prendaId));

        [HttpDelete("prendas/{prendaId:int}")]
        public async Task<IActionResult> EliminarPrenda(int prendaId, [FromQuery] int empresaId) => await _svc.EliminarPrendaAsync(empresaId, prendaId) ? NoContent() : NotFound();

        [HttpPost("prendas/{prendaId:int}/fotos")]
        [RequestSizeLimit(7 * 1024 * 1024)]
        [EnableRateLimiting("uploads")]
        public async Task<IActionResult> SubirFoto(int prendaId, [FromQuery] int empresaId, IFormFile archivo)
        {
            if (archivo == null) return BadRequest(new { mensaje = "Elegí una foto" });
            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            return await Intentar(async () => { var f = await _svc.SubirFotoAsync(empresaId, prendaId, ms.ToArray()); return new { f.FotoId }; });
        }

        [HttpDelete("fotos/{fotoId:int}")]
        public async Task<IActionResult> EliminarFoto(int fotoId, [FromQuery] int empresaId) => await _svc.EliminarFotoAsync(empresaId, fotoId) ? NoContent() : NotFound();

        /// <summary>Foto de una prenda propia (aunque no esté publicada).</summary>
        [HttpGet("fotos/{fotoId:int}")]
        public async Task<IActionResult> Foto(int fotoId, [FromQuery] int empresaId)
        {
            var f = await _svc.FotoPrivadaAsync(empresaId, fotoId);
            return f == null ? NotFound() : File(f.Datos, f.ContentType);
        }

        [HttpGet("clientas")]
        public async Task<IActionResult> Clientas([FromQuery] int empresaId) => Ok(await _svc.ClientasAsync(empresaId));

        [HttpPost("clientas")]
        public Task<IActionResult> CrearClienta([FromQuery] int empresaId, [FromBody] ClientaRequest r) => Intentar(async () => await _svc.GuardarClientaAsync(empresaId, r));

        [HttpPut("clientas/{clientaId:int}")]
        public Task<IActionResult> EditarClienta(int clientaId, [FromQuery] int empresaId, [FromBody] ClientaRequest r) => Intentar(async () => await _svc.GuardarClientaAsync(empresaId, r, clientaId));

        [HttpGet("ventas")]
        public async Task<IActionResult> Ventas([FromQuery] int empresaId, [FromQuery] int dias = 30) => Ok(await _svc.VentasAsync(empresaId, dias));

        [HttpPost("ventas")]
        public Task<IActionResult> Vender([FromQuery] int empresaId, [FromBody] VentaRequest r) =>
            Intentar(async () => await _svc.VenderAsync(empresaId, r, User.FindFirstValue(ClaimTypes.Name)));

        [HttpPost("ventas/{ventaId:int}/apartado")]
        public Task<IActionResult> CerrarApartado(int ventaId, [FromQuery] int empresaId, [FromBody] CerrarApartadoRequest r) =>
            Intentar(async () => await _svc.CerrarApartadoAsync(empresaId, ventaId, r.Pagar, r.MedioPago));

        [HttpGet("alquileres")]
        public async Task<IActionResult> Alquileres([FromQuery] int empresaId) => Ok(await _svc.AlquileresAsync(empresaId));

        [HttpGet("alquileres/libres")]
        public async Task<IActionResult> Libres([FromQuery] int empresaId, [FromQuery] int varianteId, [FromQuery] DateTime desde, [FromQuery] DateTime hasta) =>
            Ok(new { libres = await _svc.LibresAsync(varianteId, desde.Date, hasta.Date, empresaId: empresaId) });

        [HttpPost("alquileres")]
        public Task<IActionResult> Alquilar([FromQuery] int empresaId, [FromBody] AlquilerRequest r) => Intentar(async () => await _svc.AlquilarAsync(empresaId, r));

        [HttpPost("alquileres/{alquilerId:int}/estado")]
        public Task<IActionResult> EstadoAlquiler(int alquilerId, [FromQuery] int empresaId, [FromBody] EstadoRequest r) =>
            Intentar(async () => await _svc.EstadoAlquilerAsync(empresaId, alquilerId, r.Estado));

        [HttpGet("pruebas")]
        public async Task<IActionResult> Pruebas([FromQuery] int empresaId) => Ok(await _svc.PruebasAsync(empresaId));

        [HttpPost("pruebas/{pruebaId:int}/estado")]
        public Task<IActionResult> EstadoPrueba(int pruebaId, [FromQuery] int empresaId, [FromBody] EstadoRequest r) =>
            Intentar(async () => await _svc.EstadoPruebaAsync(empresaId, pruebaId, r.Estado));

        [HttpPost("demo")]
        public Task<IActionResult> Demo([FromQuery] int empresaId) => Intentar(async () => new { prendas = await _demo.CargarAsync(empresaId) });
    }

    /// <summary>VORDIA CLOSET · Marketplace público con el stock real de las boutiques.</summary>
    [ApiController]
    [Route("api/public/closet")]
    [AllowAnonymous]
    [EnableRateLimiting("closet")]
    public class ClosetController : ControllerBase
    {
        private readonly ClosetService _svc;
        private readonly BoutiqueService _boutique;

        public ClosetController(ClosetService svc, BoutiqueService boutique) { _svc = svc; _boutique = boutique; }

        [HttpGet("prendas")]
        public async Task<IActionResult> Catalogo([FromQuery] ClosetFiltro filtro) => Ok(await _svc.CatalogoAsync(filtro));

        [HttpGet("prendas/{prendaId:int}")]
        public async Task<IActionResult> Prenda(int prendaId, [FromQuery] DateTime? fecha)
        {
            var p = await _svc.PrendaAsync(prendaId, fecha);
            return p == null ? NotFound() : Ok(p);
        }

        [HttpGet("boutiques")]
        public async Task<IActionResult> Boutiques() => Ok(await _svc.BoutiquesAsync());

        [HttpGet("boutiques/{slug}")]
        public async Task<IActionResult> Boutique(string slug)
        {
            var b = await _svc.BoutiqueAsync(slug);
            return b == null ? NotFound() : Ok(b);
        }

        [HttpGet("fotos/{fotoId:int}")]
        [ResponseCache(Duration = 86400)]
        public async Task<IActionResult> Foto(int fotoId)
        {
            var f = await _boutique.FotoPublicaAsync(fotoId);
            return f == null ? NotFound() : File(f.Datos, f.ContentType);
        }

        [HttpPost("pruebas")]
        [EnableRateLimiting("leads")]
        public async Task<IActionResult> PedirPrueba([FromBody] PruebaPublicaRequest r)
        {
            try { return Ok(await _svc.PedirPruebaAsync(r)); }
            catch (KeyNotFoundException) { return NotFound(new { mensaje = "Esa prenda ya no está publicada" }); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }
    }
}
