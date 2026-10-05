using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

[Migration("20261005020000_ExpandSecureRoomCodes")]
public partial class ExpandSecureRoomCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "RoomCode",
            table: "Participants",
            type: "character varying(8)",
            maxLength: 8,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(6)");

        migrationBuilder.AlterColumn<string>(
            name: "Code",
            table: "Rooms",
            type: "character varying(8)",
            maxLength: 8,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(6)",
            oldMaxLength: 6);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "RoomCode",
            table: "Participants",
            type: "character varying(6)",
            maxLength: 6,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(8)");

        migrationBuilder.AlterColumn<string>(
            name: "Code",
            table: "Rooms",
            type: "character varying(6)",
            maxLength: 6,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(8)",
            oldMaxLength: 8);
    }
}
