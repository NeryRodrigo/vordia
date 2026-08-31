namespace Gesco.Application.DTOs
{
    public class AuthUserResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Email { get; set; }
        public string Rol { get; set; } = "";
        public int EmpresaId { get; set; }
        public bool RequiereCambioPass { get; set; }
        public string Rubro { get; set; } = "RETAIL";
        public string? Token { get; set; }
        public List<string> PermisosIds { get; set; } = new List<string>();
        public List<int> ModulosIds { get; set; } = new List<int>();
    }
}
