using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace InventoryWarehouseApi.Infrastructure.Persistence.Configurations;
internal sealed class RefreshTokenConfiguration:IEntityTypeConfiguration<RefreshToken>{public void Configure(EntityTypeBuilder<RefreshToken>b){b.ToTable("RefreshTokens",t=>{t.HasCheckConstraint("CK_RefreshTokens_Expiry","[ExpiresAtUtc] > [CreatedAtUtc]");t.HasCheckConstraint("CK_RefreshTokens_Revocation","[RevokedAtUtc] IS NULL OR [RevokedAtUtc] >= [CreatedAtUtc]");});b.HasKey(x=>x.Id);b.Property(x=>x.TokenHash).HasMaxLength(RefreshToken.TokenHashMaxLength).IsFixedLength().IsRequired();b.HasIndex(x=>x.TokenHash).IsUnique().HasDatabaseName("UX_RefreshTokens_TokenHash");b.HasIndex(x=>new{x.UserId,x.ExpiresAtUtc});b.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);}}
