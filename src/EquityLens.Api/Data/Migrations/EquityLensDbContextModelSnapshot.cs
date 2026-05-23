using EquityLens.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace EquityLens.Api.Data.Migrations;

[DbContext(typeof(EquityLensDbContext))]
partial class EquityLensDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

        modelBuilder.Entity("EquityLens.Api.Models.ApiRequestLog", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            b.Property<string>("EndpointName")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("ErrorMessage")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("Provider")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<DateTime>("RequestedAt")
                .HasColumnType("TEXT");

            b.Property<int>("StatusCode")
                .HasColumnType("INTEGER");

            b.Property<bool>("Success")
                .HasColumnType("INTEGER");

            b.Property<string>("Ticker")
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.ToTable("ApiRequestLogs");
        });

        modelBuilder.Entity("EquityLens.Api.Models.ResearchSnapshot", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("TEXT");

            b.Property<string>("DashboardJson")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<decimal>("OneYearReturn")
                .HasColumnType("TEXT");

            b.Property<int>("RiskScore")
                .HasColumnType("INTEGER");

            b.Property<string>("Summary")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("Ticker")
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.ToTable("ResearchSnapshots");
        });

        modelBuilder.Entity("EquityLens.Api.Models.WatchlistItem", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            b.Property<DateTime>("AddedAt")
                .HasColumnType("TEXT");

            b.Property<int?>("LastKnownRiskScore")
                .HasColumnType("INTEGER");

            b.Property<DateTime>("LastViewedAt")
                .HasColumnType("TEXT");

            b.Property<string>("Notes")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("Ticker")
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("Ticker")
                .IsUnique();

            b.ToTable("WatchlistItems");
        });
#pragma warning restore 612, 618
    }
}
