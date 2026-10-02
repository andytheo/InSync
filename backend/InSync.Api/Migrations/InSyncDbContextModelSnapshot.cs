using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

[DbContext(typeof(InSyncDbContext))]
partial class InSyncDbContextModelSnapshot : ModelSnapshot {
 protected override void BuildModel(ModelBuilder modelBuilder){
  modelBuilder.HasAnnotation("ProductVersion","8.0.22").HasAnnotation("Relational:MaxIdentifierLength",63);
  modelBuilder.Entity("RoomEntity",b=>{b.Property<string>("Code").HasMaxLength(6).HasColumnType("character varying(6)");b.Property<string>("Content").HasColumnType("text");b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");b.Property<bool>("Ended").HasColumnType("boolean");b.Property<DateTimeOffset>("LastActivityAt").HasColumnType("timestamp with time zone");b.Property<bool>("PlaybackPlaying").HasColumnType("boolean");b.Property<double>("PlaybackPosition").HasColumnType("double precision");b.Property<long>("PlaybackSequence").HasColumnType("bigint");b.Property<DateTimeOffset>("PlaybackUpdatedAt").HasColumnType("timestamp with time zone");b.HasKey("Code");b.ToTable("Rooms");});
  modelBuilder.Entity("ParticipantEntity",b=>{b.Property<Guid>("Id").HasColumnType("uuid");b.Property<string>("Name").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");b.Property<bool>("Online").HasColumnType("boolean");b.Property<bool>("Ready").HasColumnType("boolean");b.Property<string>("RoomCode").IsRequired().HasColumnType("character varying(6)");b.HasKey("Id");b.HasIndex("RoomCode");b.ToTable("Participants");});
  modelBuilder.Entity("ParticipantEntity",b=>{b.HasOne("RoomEntity",null).WithMany("Participants").HasForeignKey("RoomCode").OnDelete(DeleteBehavior.Cascade).IsRequired();});
 }
}