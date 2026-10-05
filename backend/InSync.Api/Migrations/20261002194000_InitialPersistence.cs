using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

[DbContext(typeof(InSyncDbContext))]
[Migration("20261002194000_InitialPersistence")]
public partial class InitialPersistence : Migration {
 protected override void Up(MigrationBuilder migrationBuilder){
  migrationBuilder.CreateTable(name:"Rooms",columns:table=>new{
   Code=table.Column<string>(type:"character varying(6)",maxLength:6,nullable:false),
   Content=table.Column<string>(type:"text",nullable:true),
   PlaybackPosition=table.Column<double>(type:"double precision",nullable:false),
   PlaybackPlaying=table.Column<bool>(type:"boolean",nullable:false),
   PlaybackSequence=table.Column<long>(type:"bigint",nullable:false),
   PlaybackUpdatedAt=table.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:false),
   Ended=table.Column<bool>(type:"boolean",nullable:false),
   CreatedAt=table.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:false),
   LastActivityAt=table.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:false)
  },constraints:table=>table.PrimaryKey("PK_Rooms",x=>x.Code));
  migrationBuilder.CreateTable(name:"Participants",columns:table=>new{
   Id=table.Column<Guid>(type:"uuid",nullable:false),
   RoomCode=table.Column<string>(type:"character varying(6)",nullable:false),
   Name=table.Column<string>(type:"character varying(80)",maxLength:80,nullable:false),
   Ready=table.Column<bool>(type:"boolean",nullable:false),
   Online=table.Column<bool>(type:"boolean",nullable:false)
  },constraints:table=>{table.PrimaryKey("PK_Participants",x=>x.Id);table.ForeignKey(name:"FK_Participants_Rooms_RoomCode",column:x=>x.RoomCode,principalTable:"Rooms",principalColumn:"Code",onDelete:ReferentialAction.Cascade);});
  migrationBuilder.CreateIndex(name:"IX_Participants_RoomCode",table:"Participants",column:"RoomCode");
 }
 protected override void Down(MigrationBuilder migrationBuilder){migrationBuilder.DropTable(name:"Participants");migrationBuilder.DropTable(name:"Rooms");}
}