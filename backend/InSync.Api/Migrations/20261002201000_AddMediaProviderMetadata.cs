using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
public partial class AddMediaProviderMetadata : Migration {
 protected override void Up(MigrationBuilder migrationBuilder){migrationBuilder.AddColumn<string>(name:"Provider",table:"Rooms",type:"text",nullable:true);migrationBuilder.AddColumn<string>(name:"MediaUrl",table:"Rooms",type:"text",nullable:true);}
 protected override void Down(MigrationBuilder migrationBuilder){migrationBuilder.DropColumn(name:"Provider",table:"Rooms");migrationBuilder.DropColumn(name:"MediaUrl",table:"Rooms");}
}