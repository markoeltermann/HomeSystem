using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    partial class HomeSystemContext
    {
        static HomeSystemContext()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<JobStatus>("job_status", new EnumNameTranslator());
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Job>(entity =>
            {
                entity.Property(e => e.Status).HasColumnName("status");
            });
        }

        private class EnumNameTranslator : INpgsqlNameTranslator
        {
            public string TranslateMemberName(string clrName)
            {
                return clrName;
            }

            public string TranslateTypeName(string clrName)
            {
                return clrName;
            }
        }
    }
}
