using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/Crm")]
    [ApiController]
    public class CrmOperacionController : ControllerBase
    {
        private readonly ICrmOperacionService _ops;

        public CrmOperacionController(ICrmOperacionService ops)
        {
            _ops = ops;
        }

        [HttpGet("tareas")]
        public async Task<IActionResult> ListarTareas([FromQuery] int empresaId, [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarTareasAsync(empresaId, estado));
        }

        [HttpPost("tareas")]
        public async Task<IActionResult> CrearTarea([FromBody] CrmTareaUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Titulo))
                return BadRequest(new { mensaje = "Titulo es obligatorio" });
            return Ok(await _ops.CrearTareaAsync(request));
        }

        [HttpPut("tareas/{tareaId:int}")]
        public async Task<IActionResult> ActualizarTarea(int tareaId, [FromBody] CrmTareaUpsertRequest request)
        {
            var updated = await _ops.ActualizarTareaAsync(tareaId, request);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpPatch("tareas/{tareaId:int}/estado")]
        public async Task<IActionResult> CambiarEstadoTarea(int tareaId, [FromQuery] int empresaId, [FromBody] CrmEstadoSimpleRequest body)
        {
            var updated = await _ops.CambiarEstadoTareaAsync(tareaId, empresaId, body.Estado);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("tareas/{tareaId:int}")]
        public async Task<IActionResult> EliminarTarea(int tareaId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarTareaAsync(tareaId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("socios")]
        public async Task<IActionResult> ListarSocios([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarSociosAsync(empresaId));
        }

        [HttpPost("socios")]
        public async Task<IActionResult> CrearSocio([FromBody] CrmSocioUpsertRequest request)
        {
            return Ok(await _ops.UpsertSocioAsync(request));
        }

        [HttpPut("socios/{socioId:int}")]
        public async Task<IActionResult> ActualizarSocio(int socioId, [FromBody] CrmSocioUpsertRequest request)
        {
            try
            {
                return Ok(await _ops.UpsertSocioAsync(request, socioId));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("socios/{socioId:int}")]
        public async Task<IActionResult> EliminarSocio(int socioId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarSocioAsync(socioId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("flujo-caja")]
        public async Task<IActionResult> FlujoCaja([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ObtenerFlujoCajaAsync(empresaId));
        }

        [HttpGet("roadmap")]
        public async Task<IActionResult> ListarRoadmap([FromQuery] int empresaId, [FromQuery] string? tipo = null, [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarRoadmapAsync(empresaId, tipo, estado));
        }

        [HttpGet("roadmap/{roadmapId:int}")]
        public async Task<IActionResult> ObtenerRoadmap(int roadmapId, [FromQuery] int empresaId)
        {
            var item = await _ops.ObtenerRoadmapAsync(roadmapId, empresaId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("roadmap")]
        public async Task<IActionResult> CrearRoadmap([FromBody] CrmRoadmapUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Titulo))
                return BadRequest(new { mensaje = "Titulo es obligatorio" });
            return Ok(await _ops.CrearRoadmapAsync(request));
        }

        [HttpPut("roadmap/{roadmapId:int}")]
        public async Task<IActionResult> ActualizarRoadmap(int roadmapId, [FromBody] CrmRoadmapUpsertRequest request)
        {
            var updated = await _ops.ActualizarRoadmapAsync(roadmapId, request);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpPatch("roadmap/{roadmapId:int}/estado")]
        public async Task<IActionResult> CambiarEstadoRoadmap(int roadmapId, [FromQuery] int empresaId, [FromBody] CrmEstadoSimpleRequest body)
        {
            var updated = await _ops.CambiarEstadoRoadmapAsync(roadmapId, empresaId, body.Estado, body.UsuarioId, body.UsuarioNombre);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpPost("roadmap/{roadmapId:int}/notas")]
        public async Task<IActionResult> AgregarNotaRoadmap(int roadmapId, [FromBody] CrmRoadmapNotaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Titulo))
                return BadRequest(new { mensaje = "Titulo es obligatorio" });
            var updated = await _ops.AgregarNotaRoadmapAsync(roadmapId, request);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("roadmap/{roadmapId:int}")]
        public async Task<IActionResult> EliminarRoadmap(int roadmapId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarRoadmapAsync(roadmapId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Capital ---
        [HttpGet("capital")]
        public async Task<IActionResult> Capital([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ObtenerCapitalAsync(empresaId));
        }

        [HttpPost("capital")]
        public async Task<IActionResult> RegistrarCapital([FromBody] CrmCapitalUpsertRequest request)
        {
            if (request.EmpresaId <= 0 || request.MontoGs == 0)
                return BadRequest(new { mensaje = "EmpresaId y MontoGs requeridos" });
            return Ok(await _ops.RegistrarCapitalAsync(request));
        }

        [HttpDelete("capital/{capitalId:int}")]
        public async Task<IActionResult> EliminarCapital(int capitalId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarCapitalAsync(capitalId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Propuestas comerciales ---
        [HttpGet("propuestas")]
        public async Task<IActionResult> Propuestas(
            [FromQuery] int empresaId,
            [FromQuery] int? clienteId = null,
            [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarPropuestasAsync(empresaId, clienteId, estado));
        }

        [HttpPost("propuestas")]
        public async Task<IActionResult> CrearPropuesta([FromBody] CrmPropuestaUpsertRequest request)
        {
            if (request.EmpresaId <= 0 || request.ClienteId <= 0 || string.IsNullOrWhiteSpace(request.Descripcion))
                return BadRequest(new { mensaje = "EmpresaId, ClienteId y Descripcion requeridos" });
            try
            {
                return Ok(await _ops.UpsertPropuestaAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpPut("propuestas/{propuestaId:int}")]
        public async Task<IActionResult> ActualizarPropuesta(int propuestaId, [FromBody] CrmPropuestaUpsertRequest request)
        {
            if (request.EmpresaId <= 0 || request.ClienteId <= 0)
                return BadRequest(new { mensaje = "EmpresaId y ClienteId requeridos" });
            try
            {
                return Ok(await _ops.UpsertPropuestaAsync(request, propuestaId));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("propuestas/{propuestaId:int}")]
        public async Task<IActionResult> EliminarPropuesta(int propuestaId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarPropuestaAsync(propuestaId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Gastos / Cuentas por pagar ---
        [HttpGet("gastos")]
        public async Task<IActionResult> Gastos([FromQuery] int empresaId, [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarGastosAsync(empresaId, estado));
        }

        [HttpPost("gastos")]
        public async Task<IActionResult> CrearGasto([FromBody] CrmGastoUpsertRequest request)
        {
            if (request.EmpresaId <= 0 || string.IsNullOrWhiteSpace(request.Concepto) || request.MontoGs == 0)
                return BadRequest(new { mensaje = "EmpresaId, Concepto y MontoGs requeridos" });
            return Ok(await _ops.UpsertGastoAsync(request));
        }

        [HttpPut("gastos/{gastoId:int}")]
        public async Task<IActionResult> ActualizarGasto(int gastoId, [FromBody] CrmGastoUpsertRequest request)
        {
            if (request.EmpresaId <= 0 || string.IsNullOrWhiteSpace(request.Concepto))
                return BadRequest(new { mensaje = "EmpresaId y Concepto requeridos" });
            try
            {
                return Ok(await _ops.UpsertGastoAsync(request, gastoId));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpPost("gastos/{gastoId:int}/pagar")]
        public async Task<IActionResult> PagarGasto(int gastoId, [FromBody] CrmGastoPagoRequest request)
        {
            if (request.EmpresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var dto = await _ops.MarcarGastoPagadoAsync(gastoId, request);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpDelete("gastos/{gastoId:int}")]
        public async Task<IActionResult> EliminarGasto(int gastoId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarGastoAsync(gastoId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Empleados HQ ---
        [HttpGet("empleados-hq")]
        public async Task<IActionResult> Empleados([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarEmpleadosHqAsync(empresaId));
        }

        [HttpPost("empleados-hq")]
        public async Task<IActionResult> CrearEmpleado([FromBody] CrmEmpleadoHqUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return BadRequest(new { mensaje = "Nombre obligatorio" });
            return Ok(await _ops.UpsertEmpleadoHqAsync(request));
        }

        [HttpPut("empleados-hq/{empleadoId:int}")]
        public async Task<IActionResult> ActualizarEmpleado(int empleadoId, [FromBody] CrmEmpleadoHqUpsertRequest request)
        {
            try
            {
                return Ok(await _ops.UpsertEmpleadoHqAsync(request, empleadoId));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("empleados-hq/{empleadoId:int}")]
        public async Task<IActionResult> EliminarEmpleado(int empleadoId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarEmpleadoHqAsync(empleadoId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Objetivos ---
        [HttpGet("objetivos")]
        public async Task<IActionResult> Objetivos([FromQuery] int empresaId, [FromQuery] string? ambito = null, [FromQuery] int? tareaId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarObjetivosAsync(empresaId, ambito, tareaId));
        }

        [HttpPost("objetivos")]
        public async Task<IActionResult> CrearObjetivo([FromBody] CrmObjetivoUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Titulo))
                return BadRequest(new { mensaje = "Titulo obligatorio" });
            return Ok(await _ops.UpsertObjetivoAsync(request));
        }

        [HttpPut("objetivos/{objetivoId:int}")]
        public async Task<IActionResult> ActualizarObjetivo(int objetivoId, [FromBody] CrmObjetivoUpsertRequest request)
        {
            try
            {
                return Ok(await _ops.UpsertObjetivoAsync(request, objetivoId));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("objetivos/{objetivoId:int}")]
        public async Task<IActionResult> EliminarObjetivo(int objetivoId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarObjetivoAsync(objetivoId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Productos ---
        [HttpGet("productos")]
        public async Task<IActionResult> Productos([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarProductosAsync(empresaId));
        }

        [HttpPost("productos")]
        public async Task<IActionResult> CrearProducto([FromBody] CrmProductoUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.Nombre))
                return BadRequest(new { mensaje = "Codigo y Nombre obligatorios" });
            return Ok(await _ops.UpsertProductoAsync(request));
        }

        [HttpPut("productos/{productoId:int}")]
        public async Task<IActionResult> ActualizarProducto(int productoId, [FromBody] CrmProductoUpsertRequest request)
        {
            try { return Ok(await _ops.UpsertProductoAsync(request, productoId)); }
            catch (InvalidOperationException ex) { return NotFound(new { mensaje = ex.Message }); }
        }

        [HttpDelete("productos/{productoId:int}")]
        public async Task<IActionResult> EliminarProducto(int productoId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarProductoAsync(productoId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Proveedores ---
        [HttpGet("proveedores")]
        public async Task<IActionResult> Proveedores([FromQuery] int empresaId, [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarProveedoresAsync(empresaId, estado));
        }

        [HttpPost("proveedores")]
        public async Task<IActionResult> CrearProveedor([FromBody] CrmProveedorUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return BadRequest(new { mensaje = "Nombre obligatorio" });
            return Ok(await _ops.UpsertProveedorAsync(request));
        }

        [HttpPut("proveedores/{proveedorId:int}")]
        public async Task<IActionResult> ActualizarProveedor(int proveedorId, [FromBody] CrmProveedorUpsertRequest request)
        {
            try { return Ok(await _ops.UpsertProveedorAsync(request, proveedorId)); }
            catch (InvalidOperationException ex) { return NotFound(new { mensaje = ex.Message }); }
        }

        [HttpDelete("proveedores/{proveedorId:int}")]
        public async Task<IActionResult> EliminarProveedor(int proveedorId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarProveedorAsync(proveedorId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Proyectos ---
        [HttpGet("proyectos")]
        public async Task<IActionResult> Proyectos([FromQuery] int empresaId, [FromQuery] string? estado = null, [FromQuery] string? productoCodigo = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarProyectosAsync(empresaId, estado, productoCodigo));
        }

        [HttpPost("proyectos")]
        public async Task<IActionResult> CrearProyecto([FromBody] CrmProyectoUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Titulo))
                return BadRequest(new { mensaje = "Titulo obligatorio" });
            return Ok(await _ops.UpsertProyectoAsync(request));
        }

        [HttpPut("proyectos/{proyectoId:int}")]
        public async Task<IActionResult> ActualizarProyecto(int proyectoId, [FromBody] CrmProyectoUpsertRequest request)
        {
            try { return Ok(await _ops.UpsertProyectoAsync(request, proyectoId)); }
            catch (InvalidOperationException ex) { return NotFound(new { mensaje = ex.Message }); }
        }

        [HttpDelete("proyectos/{proyectoId:int}")]
        public async Task<IActionResult> EliminarProyecto(int proyectoId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarProyectoAsync(proyectoId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
