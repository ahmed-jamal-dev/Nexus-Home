using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimescaleHypertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert device_telemetry to TimescaleDB hypertable
            // The table already exists with composite PK (device_id, timestamp)
            // which is required for hypertable partitioning
            migrationBuilder.Sql(@"
                SELECT create_hypertable(
                    'device_telemetry',
                    'timestamp',
                    chunk_time_interval => INTERVAL '1 day',
                    if_not_exists => TRUE,
                    migrate_data => TRUE
                );
            ");

            // Add compression policy for chunks older than 7 days
            migrationBuilder.Sql(@"
                ALTER TABLE device_telemetry SET (
                    timescaledb.compress,
                    timescaledb.compress_segmentby = 'device_id'
                );
            ");

            migrationBuilder.Sql(@"
                SELECT add_compression_policy('device_telemetry', INTERVAL '7 days');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove compression policy
            migrationBuilder.Sql(@"
                SELECT remove_compression_policy('device_telemetry', if_exists => TRUE);
            ");

            // Remove compression
            migrationBuilder.Sql(@"
                ALTER TABLE device_telemetry SET (timescaledb.compress = false);
            ");

            // Convert hypertable back to regular table
            // Note: This will not delete data, but will remove hypertable functionality
            migrationBuilder.Sql(@"
                SELECT drop_chunks('device_telemetry', older_than => now() - INTERVAL '100 years');
                SELECT detach_hypertable('device_telemetry', if_exists => TRUE);
            ");
        }
    }
}