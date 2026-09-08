using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// Gasto de VORDIA HQ. Cubre tanto el gasto ya pagado como la cuenta por
    /// pagar: mientras Estado sea "Pendiente" cuenta como deuda, y al pagarse
    /// se carga FechaPago y pasa a "Pagado".
    /// </summary>
    [Table("CrmGastos")]
    public class CrmGasto
    {
        [Key]
        public int GastoId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(200)]
        public string Concepto { get; set; } = "";
        [Required, StringLength(40)]
        public string Categoria { get; set; } = "Operativo";
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoGs { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        [Required, StringLength(20)]
        public string Estado { get; set; } = "Pendiente";
        public int? ProveedorId { get; set; }
        [StringLength(200)]
        public string? ProveedorNombre { get; set; }
        [StringLength(40)]
        public string? MetodoPago { get; set; }
        [StringLength(60)]
        public string? NroComprobante { get; set; }
        [StringLength(500)]
        public string? Notas { get; set; }
        [StringLength(150)]
        public string? UsuarioNombre { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
