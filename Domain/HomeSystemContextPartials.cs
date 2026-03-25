using Microsoft.EntityFrameworkCore;

namespace Domain;

partial class HomeSystemContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.Property(e => e.Status).HasColumnName("status");
        });
    }
}
