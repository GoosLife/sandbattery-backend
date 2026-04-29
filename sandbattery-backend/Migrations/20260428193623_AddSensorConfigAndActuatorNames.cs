using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sandbattery_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorConfigAndActuatorNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "actuator_status",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensor_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    device_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sensor_index = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_config", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_config_device_id_type_sensor_index",
                table: "sensor_config",
                columns: new[] { "device_id", "type", "sensor_index" },
                unique: true);

            // Seed device DP-SB-01 if not exists
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO device (id, product_key)
                VALUES (3, 'DP-SB-01');
            ");

            // Seed sensor_config if not exists
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO sensor_config (device_id, type, sensor_index, name) VALUES
                (3, 'flow_rate',   0, 'Flow in'),
                (3, 'flow_rate',   1, 'Flow out'),
                (3, 'temperature', 0, 'Flow temp in'),
                (3, 'temperature', 1, 'Flow temp out'),
                (3, 'temperature', 2, 'Side'),
                (3, 'temperature', 3, 'Core');
            ");

            // Seed actuator_status if not exists
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO actuator_status (device_id, actuator, actuator_index, name, active, source, last_changed) VALUES
                (3, 'heater', 0, 'Small',  0, 'manual', UTC_TIMESTAMP()),
                (3, 'heater', 1, 'Medium', 0, 'manual', UTC_TIMESTAMP()),
                (3, 'heater', 2, 'Large',  0, 'manual', UTC_TIMESTAMP());
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sensor_config");

            migrationBuilder.DropColumn(
                name: "name",
                table: "actuator_status");
        }
    }
}
