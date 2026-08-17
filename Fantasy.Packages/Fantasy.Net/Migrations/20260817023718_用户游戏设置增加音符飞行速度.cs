using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Net.Migrations
{
    /// <inheritdoc />
    public partial class 用户游戏设置增加音符飞行速度 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "音符飞行速度",
                schema: "NetShare_音游",
                table: "用户游戏设置",
                type: "real",
                nullable: false,
                defaultValue: 1f); // 存量记录按原速(1.0x)回填
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "音符飞行速度",
                schema: "NetShare_音游",
                table: "用户游戏设置");
        }
    }
}
