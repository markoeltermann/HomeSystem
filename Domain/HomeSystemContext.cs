using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Domain;

public partial class HomeSystemContext : DbContext
{
    public HomeSystemContext(DbContextOptions<HomeSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DataType> DataTypes { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<DevicePoint> DevicePoints { get; set; }

    public virtual DbSet<EnumMember> EnumMembers { get; set; }

    public virtual DbSet<InverterSetting> InverterSettings { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum("job_status", new[] { "Running", "Completed", "Failed" });

        modelBuilder.Entity<DataType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("data_type_pkey");

            entity.ToTable("data_type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("device_pkey");

            entity.ToTable("device");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasColumnType("jsonb")
                .HasColumnName("address");
            entity.Property(e => e.IsEnabled).HasColumnName("is_enabled");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Type).HasColumnName("type");
        });

        modelBuilder.Entity<DevicePoint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("device_point_pkey");

            entity.ToTable("device_point");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.DataTypeId).HasColumnName("data_type_id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.IsFrequentReadEnabled).HasColumnName("is_frequent_read_enabled");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.DataType).WithMany(p => p.DevicePoints)
                .HasForeignKey(d => d.DataTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_data_type_id");

            entity.HasOne(d => d.Device).WithMany(p => p.DevicePoints)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_device_point");

            entity.HasOne(d => d.Unit).WithMany(p => p.DevicePoints)
                .HasForeignKey(d => d.UnitId)
                .HasConstraintName("device_point_unit_id_fkey");
        });

        modelBuilder.Entity<EnumMember>(entity =>
        {
            entity.HasKey(e => new { e.DevicePointId, e.Value }).HasName("enum_member_pkey");

            entity.ToTable("enum_member");

            entity.Property(e => e.DevicePointId).HasColumnName("device_point_id");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasColumnName("name");

            entity.HasOne(d => d.DevicePoint).WithMany(p => p.EnumMembers)
                .HasForeignKey(d => d.DevicePointId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("device_point_id");
        });

        modelBuilder.Entity<InverterSetting>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("inverter_settings");

            entity.Property(e => e.BatteryChargeCurrent).HasColumnName("battery_charge_current");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("job_pkey");

            entity.ToTable("job");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Name)
                .HasMaxLength(32)
                .HasColumnName("name");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("unit_pkey");

            entity.ToTable("unit");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
        });
        modelBuilder.HasSequence<int>("device_point_id_seq");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
