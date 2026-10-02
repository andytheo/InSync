using Microsoft.EntityFrameworkCore;

public sealed class InSyncDbContext(DbContextOptions<InSyncDbContext> options):DbContext(options) {
 public DbSet<RoomEntity> Rooms => Set<RoomEntity>();
 public DbSet<ParticipantEntity> Participants => Set<ParticipantEntity>();
 protected override void OnModelCreating(ModelBuilder b){
  b.Entity<RoomEntity>().HasKey(x=>x.Code);
  b.Entity<RoomEntity>().Property(x=>x.Code).HasMaxLength(6);
  b.Entity<RoomEntity>().HasMany(x=>x.Participants).WithOne().HasForeignKey(x=>x.RoomCode).OnDelete(DeleteBehavior.Cascade);
  b.Entity<ParticipantEntity>().HasKey(x=>x.Id);
  b.Entity<ParticipantEntity>().Property(x=>x.Name).HasMaxLength(80);
 }
}
public sealed class RoomEntity {
 public string Code {get;set;}="";
 public string? Content {get;set;}
 public double PlaybackPosition {get;set;}
 public bool PlaybackPlaying {get;set;}
 public long PlaybackSequence {get;set;}
 public DateTimeOffset PlaybackUpdatedAt {get;set;}=DateTimeOffset.UtcNow;
 public bool Ended {get;set;}
 public DateTimeOffset CreatedAt {get;set;}=DateTimeOffset.UtcNow;
 public DateTimeOffset LastActivityAt {get;set;}=DateTimeOffset.UtcNow;
 public List<ParticipantEntity> Participants {get;set;}=[];
}
public sealed class ParticipantEntity {
 public Guid Id {get;set;}
 public required string RoomCode {get;set;}
 public required string Name {get;set;}
 public bool Ready {get;set;}
 public bool Online {get;set;}
}
