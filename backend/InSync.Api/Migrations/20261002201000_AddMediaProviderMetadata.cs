using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
#nullable disable
[DbContext(typeof(InSyncDbContext))]
[Migration("20261002201000_AddMediaProviderMetadata")]
public partial class AddMediaProviderMetadata : Migration {
 protected override void Up(MigrationBuilder migrationBuilder){migrationBuilder.AddColumn<string>(name:"Provider",table:"Rooms",type:"text",nullable:true);migrationBuilder.AddColumn<string>(name:"MediaUrl",table:"Rooms",type:"text",nullable:true);}
 protected override void Down(MigrationBuilder migrationBuilder){migrationBuilder.DropColumn(name:"Provider",table:"Rooms");migrationBuilder.DropColumn(name:"MediaUrl",table:"Rooms");}
}