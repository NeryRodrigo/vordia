using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>VORDIA BOUTIQUE · Vidriera pública de la boutique en el marketplace.</summary>
    [Table("BoutiquePerfiles")]
    public class BoutiquePerfil
    {
        [Key] public int PerfilId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(120)] public string NombreComercial { get; set; } = "";
        /// <summary>Dirección pública de la vidriera: /closet/{slug}.</summary>
        [Required, StringLength(80)] public string Slug { get; set; } = "";
        [StringLength(500)] public string? Descripcion { get; set; }
        [StringLength(60)] public string? Ciudad { get; set; }
        [StringLength(80)] public string? Barrio { get; set; }
        [StringLength(250)] public string? Direccion { get; set; }
        [StringLength(40)] public string? Whatsapp { get; set; }
        [StringLength(80)] public string? Instagram { get; set; }
        [StringLength(200)] public string? Horario { get; set; }
        /// <summary>Estilo principal: Fiesta | Casual | Urbano | Novias | Talles grandes | Accesorios…</summary>
        [StringLength(60)] public string? Estilo { get; set; }
        /// <summary>Boutique revisada por VORDIA (local real, RUC). Se muestra con insignia.</summary>
        public bool Verificada { get; set; }
        /// <summary>Visible en el marketplace.</summary>
        public bool Publicada { get; set; } = true;
        /// <summary>Datos de demostración: se marcan como "ejemplo" en el marketplace.</summary>
        public bool Demo { get; set; }
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Prenda o accesorio. Las variantes (talle y color) llevan el stock.</summary>
    [Table("BoutiquePrendas")]
    public class BoutiquePrenda
    {
        [Key] public int PrendaId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(150)] public string Nombre { get; set; } = "";
        /// <summary>Vestidos | Blusas | Pantalones | Faldas | Conjuntos | Abrigos | Calzados | Carteras | Accesorios</summary>
        [Required, StringLength(40)] public string Categoria { get; set; } = "Vestidos";
        /// <summary>Ocasiones separadas por coma: Fiesta, Casamiento, 15 años, Graduación, Oficina, Casual…</summary>
        [StringLength(200)] public string? Ocasiones { get; set; }
        [StringLength(1500)] public string? Descripcion { get; set; }
        [StringLength(120)] public string? Material { get; set; }
        /// <summary>Venta | Alquiler | Ambos</summary>
        [Required, StringLength(10)] public string Modalidad { get; set; } = "Venta";
        public decimal PrecioVenta { get; set; }
        /// <summary>Precio por alquiler (bloque de días mínimo).</summary>
        public decimal PrecioAlquiler { get; set; }
        public int DiasAlquiler { get; set; } = 4;
        /// <summary>Garantía que se cobra al retirar un alquiler y se devuelve al traer la prenda.</summary>
        public decimal Garantia { get; set; }
        /// <summary>Precio antes de la oferta (para mostrar el descuento).</summary>
        public decimal? PrecioAnterior { get; set; }
        /// <summary>Color principal (hex) para la ilustración cuando no hay foto.</summary>
        [StringLength(9)] public string? ColorHex { get; set; }
        public bool Publicada { get; set; } = true;
        public bool Activo { get; set; } = true;
        public int Vistas { get; set; }
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    }

    [Table("BoutiqueVariantes")]
    public class BoutiqueVariante
    {
        [Key] public int VarianteId { get; set; }
        public int EmpresaId { get; set; }
        public int PrendaId { get; set; }
        /// <summary>XS, S, M, L, XL, XXL, Único, o numérico (36, 38…).</summary>
        [Required, StringLength(10)] public string Talle { get; set; } = "Único";
        [Required, StringLength(40)] public string Color { get; set; } = "";
        [StringLength(9)] public string? ColorHex { get; set; }
        public int Stock { get; set; }
        /// <summary>Medidas reales en centímetros (busto, cintura, cadera, largo) para que la clienta compare con las suyas.</summary>
        public int? BustoCm { get; set; }
        public int? CinturaCm { get; set; }
        public int? CaderaCm { get; set; }
        public int? LargoCm { get; set; }
        [StringLength(40)] public string? CodigoBarras { get; set; }
        public bool Activo { get; set; } = true;
    }

    [Table("BoutiqueFotos")]
    public class BoutiqueFoto
    {
        [Key] public int FotoId { get; set; }
        public int EmpresaId { get; set; }
        public int PrendaId { get; set; }
        [Required, StringLength(80)] public string Archivo { get; set; } = "";
        [Required, StringLength(40)] public string ContentType { get; set; } = "image/jpeg";
        public int Orden { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Clienta de la boutique: su talle y preferencias para atenderla y avisarle novedades.</summary>
    [Table("BoutiqueClientas")]
    public class BoutiqueClienta
    {
        [Key] public int ClientaId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(150)] public string Nombre { get; set; } = "";
        [StringLength(40)] public string? Whatsapp { get; set; }
        [StringLength(150)] public string? Email { get; set; }
        [StringLength(10)] public string? Talle { get; set; }
        public DateTime? Cumpleanos { get; set; }
        [StringLength(500)] public string? Notas { get; set; }
        /// <summary>Caja | Marketplace | Manual</summary>
        [StringLength(20)] public string Origen { get; set; } = "Caja";
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Venta en caja o apartado con seña.</summary>
    [Table("BoutiqueVentas")]
    public class BoutiqueVenta
    {
        [Key] public int VentaId { get; set; }
        public int EmpresaId { get; set; }
        public int? ClientaId { get; set; }
        /// <summary>Pagada | Apartada | Anulada</summary>
        [Required, StringLength(12)] public string Estado { get; set; } = "Pagada";
        public decimal Total { get; set; }
        public decimal Pagado { get; set; }
        /// <summary>Efectivo | Transferencia | Tarjeta | QR</summary>
        [StringLength(20)] public string? MedioPago { get; set; }
        /// <summary>Tienda | WhatsApp | Instagram | Marketplace</summary>
        [StringLength(20)] public string Canal { get; set; } = "Tienda";
        public DateTime? VenceApartado { get; set; }
        [StringLength(300)] public string? Notas { get; set; }
        [StringLength(150)] public string? Vendedora { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }

    [Table("BoutiqueVentaItems")]
    public class BoutiqueVentaItem
    {
        [Key] public int ItemId { get; set; }
        public int VentaId { get; set; }
        public int VarianteId { get; set; }
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        [StringLength(220)] public string Descripcion { get; set; } = "";
    }

    /// <summary>Alquiler de una pieza por fechas (bloque mínimo de días), con garantía.</summary>
    [Table("BoutiqueAlquileres")]
    public class BoutiqueAlquiler
    {
        [Key] public int AlquilerId { get; set; }
        public int EmpresaId { get; set; }
        public int VarianteId { get; set; }
        public int? ClientaId { get; set; }
        [StringLength(150)] public string ClientaNombre { get; set; } = "";
        [StringLength(40)] public string? ClientaWhatsapp { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        /// <summary>Reservado | Retirado | Devuelto | Cancelado</summary>
        [Required, StringLength(12)] public string Estado { get; set; } = "Reservado";
        public decimal Precio { get; set; }
        public decimal Sena { get; set; }
        public decimal Garantia { get; set; }
        [StringLength(300)] public string? Notas { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Prueba agendada en la boutique (pedida desde el marketplace o cargada en la tienda).</summary>
    [Table("BoutiquePruebas")]
    public class BoutiquePrueba
    {
        [Key] public int PruebaId { get; set; }
        public int EmpresaId { get; set; }
        public int PrendaId { get; set; }
        public int? VarianteId { get; set; }
        [Required, StringLength(150)] public string Nombre { get; set; } = "";
        [Required, StringLength(40)] public string Whatsapp { get; set; } = "";
        public DateTime FechaHora { get; set; }
        /// <summary>Para alquiler: fecha del evento.</summary>
        public DateTime? FechaEvento { get; set; }
        /// <summary>Pedida | Confirmada | Vino | NoVino | Cancelada</summary>
        [Required, StringLength(12)] public string Estado { get; set; } = "Pedida";
        [StringLength(500)] public string? Mensaje { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
