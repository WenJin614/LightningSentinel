namespace LightningSentinel.Data.Entities
{
    public class ProbeResultEntity
    {
        public int Id { get; set; } // Database Primary Key
        public string PubKey { get; set; } = default!;
        public bool IsAlive { get; set; }
        public int LatencyMs { get; set; }
        public DateTime CheckedAt { get; set; }

        // Internal metadata the Prober doesn't need to know about
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
