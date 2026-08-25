using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryWarehouseApi.Infrastructure.Persistence.Configurations;
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
 public void Configure(EntityTypeBuilder<User> b){b.ToTable("Users",t=>{t.HasCheckConstraint("CK_Users_Role_Supported","[Role] IN (1, 2, 3, 4)");t.HasCheckConstraint("CK_Users_Timestamps","[UpdatedAtUtc] >= [CreatedAtUtc]");});b.HasKey(x=>x.Id);b.Property(x=>x.Email).HasMaxLength(User.EmailMaxLength).IsRequired();b.Property(x=>x.NormalizedEmail).HasMaxLength(User.EmailMaxLength).IsRequired();b.Property(x=>x.DisplayName).HasMaxLength(User.DisplayNameMaxLength).IsRequired();b.Property(x=>x.PasswordHash).HasMaxLength(512).IsRequired();b.Property(x=>x.Role).HasConversion<int>();b.HasIndex(x=>x.NormalizedEmail).IsUnique().HasDatabaseName("UX_Users_NormalizedEmail");}
}
