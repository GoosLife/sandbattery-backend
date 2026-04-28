using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sandbattery_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSensorAndDeviceIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add auto-increment id to device, swap PK
            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "device",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.DropPrimaryKey(
                name: "PK_device",
                table: "device");

            migrationBuilder.AddPrimaryKey(
                name: "PK_device",
                table: "device",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_device_product_key",
                table: "device",
                column: "product_key",
                unique: true);

            // 2. Add device_id to all child tables (nullable temporarily via default 0)
            migrationBuilder.AddColumn<int>(
                name: "device_id",
                table: "settings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "device_id",
                table: "sensor_measurement",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "device_id",
                table: "heartbeat",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "device_id",
                table: "event",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "device_id",
                table: "alert",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "device_id",
                table: "actuator_status",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "actuator_index",
                table: "actuator_status",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 3. Data migration: populate device_id from product_key join
            migrationBuilder.Sql("UPDATE settings s JOIN device d ON s.product_key = d.product_key SET s.device_id = d.id");
            migrationBuilder.Sql("UPDATE sensor_measurement sm JOIN device d ON sm.product_key = d.product_key SET sm.device_id = d.id");
            migrationBuilder.Sql("UPDATE heartbeat h JOIN device d ON h.product_key = d.product_key SET h.device_id = d.id");
            migrationBuilder.Sql("UPDATE event e JOIN device d ON e.product_key = d.product_key SET e.device_id = d.id");
            migrationBuilder.Sql("UPDATE alert a JOIN device d ON a.product_key = d.product_key SET a.device_id = d.id");
            migrationBuilder.Sql("UPDATE actuator_status a JOIN device d ON a.product_key = d.product_key SET a.device_id = d.id");

            // 4. Drop old product_key columns and indexes from child tables
            migrationBuilder.DropIndex(
                name: "IX_settings_product_key",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "product_key",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "product_key",
                table: "sensor_measurement");

            migrationBuilder.DropColumn(
                name: "product_key",
                table: "heartbeat");

            migrationBuilder.DropColumn(
                name: "product_key",
                table: "event");

            migrationBuilder.DropColumn(
                name: "product_key",
                table: "alert");

            migrationBuilder.DropIndex(
                name: "IX_actuator_status_product_key_actuator",
                table: "actuator_status");

            migrationBuilder.DropColumn(
                name: "product_key",
                table: "actuator_status");

            // 5. Drop old flat sensor columns from sensor_measurement
            migrationBuilder.DropColumn(name: "flow_rate",      table: "sensor_measurement");
            migrationBuilder.DropColumn(name: "sand_temp",      table: "sensor_measurement");
            migrationBuilder.DropColumn(name: "water_temp_in",  table: "sensor_measurement");
            migrationBuilder.DropColumn(name: "water_temp_out", table: "sensor_measurement");

            // 6. Create new sensor reading tables
            migrationBuilder.CreateTable(
                name: "flow_rate_sensor_reading",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    measurement_id = table.Column<int>(type: "int", nullable: false),
                    sensor_index   = table.Column<int>(type: "int", nullable: false),
                    value          = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flow_rate_sensor_reading", x => x.id);
                    table.ForeignKey(
                        name: "FK_flow_rate_sensor_reading_sensor_measurement_measurement_id",
                        column: x => x.measurement_id,
                        principalTable: "sensor_measurement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "temperature_sensor_reading",
                columns: table => new
                {
                    id             = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    measurement_id = table.Column<int>(type: "int", nullable: false),
                    sensor_index   = table.Column<int>(type: "int", nullable: false),
                    label          = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value          = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temperature_sensor_reading", x => x.id);
                    table.ForeignKey(
                        name: "FK_temperature_sensor_reading_sensor_measurement_measurement_id",
                        column: x => x.measurement_id,
                        principalTable: "sensor_measurement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 7. Create new indexes
            migrationBuilder.CreateIndex(
                name: "IX_settings_device_id",
                table: "settings",
                column: "device_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_actuator_status_device_id_actuator_actuator_index",
                table: "actuator_status",
                columns: new[] { "device_id", "actuator", "actuator_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_flow_rate_sensor_reading_measurement_id",
                table: "flow_rate_sensor_reading",
                column: "measurement_id");

            migrationBuilder.CreateIndex(
                name: "IX_temperature_sensor_reading_measurement_id",
                table: "temperature_sensor_reading",
                column: "measurement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "flow_rate_sensor_reading");
            migrationBuilder.DropTable(name: "temperature_sensor_reading");

            migrationBuilder.DropIndex(name: "IX_settings_device_id",                                       table: "settings");
            migrationBuilder.DropIndex(name: "IX_actuator_status_device_id_actuator_actuator_index",        table: "actuator_status");
            migrationBuilder.DropIndex(name: "IX_device_product_key",                                        table: "device");
            migrationBuilder.DropPrimaryKey(name: "PK_device",                                               table: "device");

            migrationBuilder.DropColumn(name: "device_id",      table: "settings");
            migrationBuilder.DropColumn(name: "device_id",      table: "sensor_measurement");
            migrationBuilder.DropColumn(name: "device_id",      table: "heartbeat");
            migrationBuilder.DropColumn(name: "device_id",      table: "event");
            migrationBuilder.DropColumn(name: "device_id",      table: "alert");
            migrationBuilder.DropColumn(name: "device_id",      table: "actuator_status");
            migrationBuilder.DropColumn(name: "actuator_index", table: "actuator_status");
            migrationBuilder.DropColumn(name: "id",             table: "device");

            migrationBuilder.AddColumn<string>(name: "product_key", table: "settings",          type: "varchar(255)", nullable: false, defaultValue: "").Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "product_key", table: "sensor_measurement", type: "longtext",     nullable: false).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "product_key", table: "heartbeat",          type: "longtext",     nullable: false).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "product_key", table: "event",              type: "longtext",     nullable: false).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "product_key", table: "alert",              type: "longtext",     nullable: false).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "product_key", table: "actuator_status",    type: "varchar(255)", nullable: false, defaultValue: "").Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<float>(name: "flow_rate",      table: "sensor_measurement", type: "float", nullable: false, defaultValue: 0f);
            migrationBuilder.AddColumn<float>(name: "sand_temp",      table: "sensor_measurement", type: "float", nullable: false, defaultValue: 0f);
            migrationBuilder.AddColumn<float>(name: "water_temp_in",  table: "sensor_measurement", type: "float", nullable: false, defaultValue: 0f);
            migrationBuilder.AddColumn<float>(name: "water_temp_out", table: "sensor_measurement", type: "float", nullable: false, defaultValue: 0f);

            migrationBuilder.AddPrimaryKey(name: "PK_device", table: "device", column: "product_key");

            migrationBuilder.CreateIndex(name: "IX_settings_product_key",               table: "settings",       column: "product_key", unique: true);
            migrationBuilder.CreateIndex(name: "IX_actuator_status_product_key_actuator", table: "actuator_status", columns: new[] { "product_key", "actuator" }, unique: true);
        }
    }
}
