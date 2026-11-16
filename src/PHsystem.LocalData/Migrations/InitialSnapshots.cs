using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PHsystem.LocalData.Migrations;

[DbContext(typeof(SnapshotDb))]
[Migration("202611150001_InitialSnapshots")]
internal sealed class InitialSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("Snapshots", table => new
        {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            CapturedAt = table.Column<long>(type: "INTEGER", nullable: false),
            Json = table.Column<string>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Snapshots", row => row.Id));
        migrationBuilder.CreateIndex("IX_Snapshots_CapturedAt", "Snapshots", "CapturedAt");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Snapshots");
    protected override void BuildTargetModel(ModelBuilder modelBuilder) => SnapshotSchema.Build(modelBuilder);
}

[DbContext(typeof(SnapshotDb))]
internal sealed class SnapshotSchema : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => Build(modelBuilder);
    internal static void Build(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.15");
        modelBuilder.Entity("PHsystem.LocalData.SnapshotRow", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("TEXT");
            entity.Property<long>("CapturedAt").HasColumnType("INTEGER");
            entity.Property<string>("Json").IsRequired().HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.HasIndex("CapturedAt");
            entity.ToTable("Snapshots");
        });
    }
}
