namespace Gesco.API.Services
{
    /// <summary>
    /// Epoch de sesión del proceso. Cambia en cada arranque del API (o vía App:SessionEpoch),
    /// invalidando JWT emitidos antes del deploy/reinicio.
    /// </summary>
    public interface ISessionEpochService
    {
        string Epoch { get; }
        DateTime StartedAtUtc { get; }
    }

    public sealed class SessionEpochService : ISessionEpochService
    {
        public string Epoch { get; }
        public DateTime StartedAtUtc { get; }

        public SessionEpochService(IConfiguration config)
        {
            StartedAtUtc = DateTime.UtcNow;
            var configured = config["App:SessionEpoch"];
            Epoch = string.IsNullOrWhiteSpace(configured)
                ? Guid.NewGuid().ToString("N")
                : configured.Trim();
        }
    }
}
