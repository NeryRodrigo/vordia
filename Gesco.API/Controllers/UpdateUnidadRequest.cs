namespace Gesco.API.Controllers
{
    /// <summary>Payload mínimo para actualizar unidad (evita validación del grafo Empresa/Contratos).</summary>
    public class UpdateUnidadRequest
    {
        public int? EmpresaId { get; set; }
        public string? NombreCondominio { get; set; }
        public string? CodigoUnidad { get; set; }
        public string? Estado { get; set; }
        public string? NisAnde { get; set; }
        public string? MedidorAgua { get; set; }
        public string? Observaciones { get; set; }
    }
}
