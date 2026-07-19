using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cleaning.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledWorkersOnlineOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A worker dot should always mean "online right now" — same rule as the Immediate view
            // (v_online_workers_for_immediate_booking). Previously this view deliberately omitted an
            // online-status check, on the reasoning that scheduled eligibility shouldn't depend on
            // being online at query time; that's been superseded by consistency with Immediate.
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_available_workers_for_scheduled_booking;");

            migrationBuilder.Sql("""
                CREATE VIEW v_available_workers_for_scheduled_booking AS
                SELECT
                    b.id AS booking_id,
                    wp.user_id AS worker_id,
                    p.full_name,
                    wp.average_rating,
                    b.service_id,
                    b.scheduled_start_time AS start_time,
                    b.scheduled_end_time AS end_time,
                    wp.current_lat,
                    wp.current_lng
                FROM bookings b
                LEFT JOIN user_addresses ua ON ua.id = b.address_id
                JOIN worker_services ws ON ws.service_id = b.service_id AND ws.is_verified
                JOIN worker_profiles wp ON wp.user_id = ws.worker_id
                JOIN profiles p ON p.id = wp.user_id
                WHERE b.booking_type = 'scheduled'
                  AND b.status = 'awaiting_worker'
                  AND b.worker_id IS NULL
                  AND wp.verification_status = 'approved'
                  AND wp.suspended_at IS NULL
                  AND wp.online_status = 'online'
                  AND wp.user_id <> b.client_id
                  AND NOT EXISTS (
                      SELECT 1 FROM worker_hidden_bookings whb
                      WHERE whb.worker_id = wp.user_id AND whb.booking_id = b.id)
                  -- default-available model (K.7): only explicit Blocked ranges exclude the slot
                  AND NOT EXISTS (
                      SELECT 1 FROM worker_availability wa
                      WHERE wa.worker_id = wp.user_id
                        AND wa.status = 'blocked'
                        AND wa.start_time < b.scheduled_end_time
                        AND wa.end_time > b.scheduled_start_time)
                  AND NOT EXISTS (
                      SELECT 1 FROM bookings b2
                      WHERE b2.worker_id = wp.user_id
                        AND b2.status IN ('accepted', 'on_the_way', 'in_progress')
                        AND b2.scheduled_start_time < b.scheduled_end_time
                        AND b2.scheduled_end_time > b.scheduled_start_time)
                  AND (
                      ua.latitude IS NULL OR ua.longitude IS NULL
                      OR (
                          COALESCE(wp.base_latitude, wp.current_lat) IS NOT NULL
                          AND COALESCE(wp.base_longitude, wp.current_lng) IS NOT NULL
                          AND 6371 * 2 * asin(sqrt(
                                power(sin(radians((COALESCE(wp.base_latitude, wp.current_lat) - ua.latitude) / 2)), 2)
                                + cos(radians(ua.latitude))
                                  * cos(radians(COALESCE(wp.base_latitude, wp.current_lat)))
                                  * power(sin(radians((COALESCE(wp.base_longitude, wp.current_lng) - ua.longitude) / 2)), 2)
                              )) <= wp.service_radius_km
                      )
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_available_workers_for_scheduled_booking;");

            migrationBuilder.Sql("""
                CREATE VIEW v_available_workers_for_scheduled_booking AS
                SELECT
                    b.id AS booking_id,
                    wp.user_id AS worker_id,
                    p.full_name,
                    wp.average_rating,
                    b.service_id,
                    b.scheduled_start_time AS start_time,
                    b.scheduled_end_time AS end_time,
                    wp.current_lat,
                    wp.current_lng
                FROM bookings b
                LEFT JOIN user_addresses ua ON ua.id = b.address_id
                JOIN worker_services ws ON ws.service_id = b.service_id AND ws.is_verified
                JOIN worker_profiles wp ON wp.user_id = ws.worker_id
                JOIN profiles p ON p.id = wp.user_id
                WHERE b.booking_type = 'scheduled'
                  AND b.status = 'awaiting_worker'
                  AND b.worker_id IS NULL
                  AND wp.verification_status = 'approved'
                  AND wp.suspended_at IS NULL
                  AND wp.user_id <> b.client_id
                  AND NOT EXISTS (
                      SELECT 1 FROM worker_hidden_bookings whb
                      WHERE whb.worker_id = wp.user_id AND whb.booking_id = b.id)
                  AND NOT EXISTS (
                      SELECT 1 FROM worker_availability wa
                      WHERE wa.worker_id = wp.user_id
                        AND wa.status = 'blocked'
                        AND wa.start_time < b.scheduled_end_time
                        AND wa.end_time > b.scheduled_start_time)
                  AND NOT EXISTS (
                      SELECT 1 FROM bookings b2
                      WHERE b2.worker_id = wp.user_id
                        AND b2.status IN ('accepted', 'on_the_way', 'in_progress')
                        AND b2.scheduled_start_time < b.scheduled_end_time
                        AND b2.scheduled_end_time > b.scheduled_start_time)
                  AND (
                      ua.latitude IS NULL OR ua.longitude IS NULL
                      OR (
                          COALESCE(wp.base_latitude, wp.current_lat) IS NOT NULL
                          AND COALESCE(wp.base_longitude, wp.current_lng) IS NOT NULL
                          AND 6371 * 2 * asin(sqrt(
                                power(sin(radians((COALESCE(wp.base_latitude, wp.current_lat) - ua.latitude) / 2)), 2)
                                + cos(radians(ua.latitude))
                                  * cos(radians(COALESCE(wp.base_latitude, wp.current_lat)))
                                  * power(sin(radians((COALESCE(wp.base_longitude, wp.current_lng) - ua.longitude) / 2)), 2)
                              )) <= wp.service_radius_km
                      )
                  );
                """);
        }
    }
}
