using Microsoft.EntityFrameworkCore.Migrations;

namespace WebSiteBanHang.Migrations
{
    public partial class FixOrderStatusMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET Status = 0 
                WHERE Status IN (0, 1) -- Map Pending, Processing to Pending (0)
            ");

            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET Status = 1 
                WHERE Status = 2 -- Map Shipped to Processing (1)
            ");

            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET Status = 2 
                WHERE Status = 3 -- Map Delivered to Completed (2)
            ");

            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET Status = 3 
                WHERE Status IN (4, 5, 6) -- Map Cancelled, Returned, Refunded to Cancelled (3)
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down method is not provided in the original file or the code block
            // If you need to revert the changes, you might want to implement a down method
        }
    }
} 